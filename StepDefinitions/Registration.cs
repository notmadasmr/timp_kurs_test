using Microsoft.Playwright;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using Xunit;
using PlaywrightTests.Support;

namespace PlaywrightTests.StepDefinitions
{
	[Binding]
	[Scope(Tag = "registration")]
	public class RegistrationSteps : Base
	{
		public RegistrationSteps(ScenarioContext scenarioContext)
			: base(scenarioContext)
		{
		}

		[Given(@"пользователь находится на странице регистрации")]
		public async Task GivenUserOnRegistrationPage()
		{
			string scenarioTitle = ScenarioContext.ScenarioInfo.Title;

			if (scenarioTitle.Contains("Успешная регистрация"))
			{
				await DeleteExistingUser();
			}
			else if (scenarioTitle.Contains("использованной ранее"))
			{
				await EnsureUserExists();
			}

			await Page.GotoAsync($"{BaseUrl.Trim()}/Account/Register");
		}

		[When(@"вводит электронную почту ""(.*)""")]
		public async Task WhenEnterEmail(string email)
		{
			await Page.FillAsync("#Email", email);
		}

		[When(@"вводит пароль ""(.*)""")]
		public async Task WhenEnterPassword(string password)
		{
			await Page.FillAsync("#Password", password);
		}

		[When(@"повторяет пароль ""(.*)""")]
		public async Task WhenRepeatPassword(string password)
		{
			await Page.FillAsync("#ConfirmPassword", password);
		}

		[When(@"нажимает кнопку ""(.*)""")]
		public async Task WhenClickButton(string buttonText)
		{
			await Page.ClickAsync($"button:has-text('{buttonText}')");
		}

		[Then(@"происходит переход на главную страницу")]
		public async Task ThenRedirectToHomePage()
		{
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync($"{BaseUrl.Trim()}/");
		}

		[Then(@"отображается сообщение ""(.*)""")]
		public async Task ThenDisplayMessage(string message)
		{
			var successMessage = Page.GetByText(message, new() { Exact = false }).First;
			await Microsoft.Playwright.Assertions.Expect(successMessage).ToBeVisibleAsync();
		}

		[Then(@"появляется сообщение рядом с полем ""(.*)"" ""(.*)""")]
		public async Task ThenDisplayFieldMessage(string fieldName, string message)
		{
			var dataValMsgFor = fieldName.ToLower() switch
			{
				"электронная почта" => "Email",
				"пароль" => "Password",
				"повторите пароль" => "ConfirmPassword",
				_ => fieldName
			};

			var fieldValidationMessage = Page.Locator($"span[data-valmsg-for='{dataValMsgFor}'].text-danger:not(.field-validation-valid)");

			if (await fieldValidationMessage.IsVisibleAsync(new() { Timeout = 1000 }))
			{
				await Microsoft.Playwright.Assertions.Expect(fieldValidationMessage).ToContainTextAsync(message);
				return;
			}

			var validationSummary = Page.Locator(".validation-summary-valid ul li, .validation-summary-errors ul li");
			var targetMessage = validationSummary.Filter(new() { HasText = message });

			await Microsoft.Playwright.Assertions.Expect(targetMessage.First).ToBeVisibleAsync();
		}

		[Then(@"регистрация не проходит")]
		public async Task ThenRegistrationFails()
		{
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync($"{BaseUrl.Trim()}/Account/Register");
		}

		[When(@"оставляет все поля пустыми")]
		public Task WhenLeaveFieldsEmpty()
		{
			return Task.CompletedTask;
		}
		private async Task DeleteExistingUser()
		{
			var freshPage = await CreateFreshPage();
			try
			{
				await freshPage.GotoAsync($"{BaseUrl.Trim()}/Account/Login");
				await freshPage.FillAsync("#Email", TestUserEmail.Trim());
				await freshPage.FillAsync("#Password", TestUserPassword.Trim());
				await freshPage.ClickAsync("button:has-text('Войти')");

				await freshPage.WaitForURLAsync(new Regex(".*(/Profile|/Account/Manage|/Home|/).*"), new() { Timeout = 5000 });

				if (!freshPage.Url.Contains("/Account/Login"))
				{
					if (!freshPage.Url.Contains("/Profile"))
					{
						await freshPage.GotoAsync($"{BaseUrl.Trim()}/Profile");
					}

					var deleteButton = freshPage.Locator("button:has-text('Удалить аккаунт'), a:has-text('Удалить аккаунт')").First;
					if (await deleteButton.IsVisibleAsync(new() { Timeout = 2000 }))
					{
						await deleteButton.ClickAsync();
						await freshPage.WaitForTimeoutAsync(1000);
					}
				}
			}
			catch {}
			finally
			{
				if (!freshPage.IsClosed) await freshPage.CloseAsync();
			}
		}
		private async Task EnsureUserExists()
		{
			var freshPage = await CreateFreshPage();
			try
			{
				await freshPage.GotoAsync($"{BaseUrl.Trim()}/Account/Login");
				await freshPage.FillAsync("#Email", TestUserEmail.Trim());
				await freshPage.FillAsync("#Password", TestUserPassword.Trim());
				await freshPage.ClickAsync("button:has-text('Войти')");
				await freshPage.WaitForTimeoutAsync(1000);

				if (freshPage.Url.Contains("/Account/Login"))
				{
					await freshPage.GotoAsync($"{BaseUrl.Trim()}/Account/Register");
					await freshPage.FillAsync("#Email", TestUserEmail.Trim());
					await freshPage.FillAsync("#Password", TestUserPassword.Trim());
					await freshPage.FillAsync("#ConfirmPassword", TestUserPassword.Trim());
					await freshPage.ClickAsync("button:has-text('Создать аккаунт')");
					await freshPage.WaitForURLAsync(new Regex(".*(/|/Profile|/Home).*"), new() { Timeout = 5000 });
				}
			}
			catch { }
			finally
			{
				if (!freshPage.IsClosed) await freshPage.CloseAsync();
			}
		}
	}
}