using Microsoft.Playwright;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using Xunit;
using PlaywrightTests.Support;

namespace PlaywrightTests.StepDefinitions
{
	[Binding]
	public class AccountSettings : Base
	{
		public AccountSettings(ScenarioContext scenarioContext) : base(scenarioContext) { }

		[Given(@"пользователь авторизован без подписки")]
		public async Task GivenUserAuthorizedWithoutSubscription()
		{
			ResetCredentials();
			await DeleteAndRecreateAccount();
			await Context.ClearCookiesAsync();
			await LoginAsUser();

			if (!Page.Url.Contains("/Account/Login"))
			{
				await Page.GotoAsync($"{BaseUrl}",
					new() { WaitUntil = WaitUntilState.DOMContentLoaded });
			}
		}

		[Given(@"пользователь авторизован и находится в личном кабинете")]
		public async Task GivenUserInAccountPage()
		{
			ResetCredentials();

			await DeleteAndRecreateAccount();

			await Context.ClearCookiesAsync();
			await LoginAsUser();

			if (Page.Url.Contains("/Account/Login"))
			{
				await Page.ScreenshotAsync(new() { Path = "login_failure_debug.png", FullPage = true });
				throw new Exception($"Не удалось выполнить вход! URL: {Page.Url}. " +
								   $"Проверьте скриншот и логи выше.");
			}

			await Page.GotoAsync($"{BaseUrl}/Profile",
				new() { WaitUntil = WaitUntilState.DOMContentLoaded });

			await Microsoft.Playwright.Assertions.Expect(Page)
				.ToHaveURLAsync($"{BaseUrl}/Profile");
		}

		[Given(@"пользователь не авторизован")]
		public async Task GivenUserIsNotAuthorized()
		{
			await Context.ClearCookiesAsync();

			await Page.GotoAsync($"{BaseUrl}",
				new() { WaitUntil = WaitUntilState.DOMContentLoaded });
		}

		[When(@"открывает страницу фильма")]
		public async Task WhenOpenMoviePage()
		{
			await Page.GotoAsync($"{BaseUrl}/");
			await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
			var firstMovie = Page.Locator("a.list-group-item").First;
			await firstMovie.ClickAsync();
		}

		[When(@"нажимает кнопку ""(.*)""")]
		public async Task WhenClickButton(string buttonText)
		{
			var button = Page.Locator($"button:has-text('{buttonText}')")
				.Or(Page.Locator($"a:has-text('{buttonText}')"))
				.Or(Page.Locator($"input[value='{buttonText}']"));
			await button.ClickAsync();
			await Task.Delay(300);
		}

		[When(@"вводит новую электронную почту ""(.*)""")]
		public async Task WhenEnterNewEmail(string email) =>
			await Page.FillAsync("#Email", email);

		[When(@"вводит электронную почту ""(.*)""")]
		public async Task WhenEnterEmail(string email) =>
			await Page.FillAsync("#Email", email);

		[When(@"вводит строку ""(.*)""")]
		public async Task WhenEnterString(string value) =>
			await Page.FillAsync("#Email", value);

		[When(@"делает поле ""(.*)"" пустым")]
		public async Task WhenClearField(string fieldName)
		{
			var field = Page.Locator("#Email");
			await field.ClickAsync();
			await field.PressAsync("Control+a");
			await field.PressAsync("Delete");
		}

		[When(@"вводит текущий пароль ""(.*)""")]
		public async Task WhenEnterCurrentPassword(string password) =>
			await Page.FillAsync("#CurrentPassword", password);

		[When(@"вводит новый пароль ""(.*)""")]
		public async Task WhenEnterNewPassword(string password) =>
			await Page.FillAsync("#NewPassword", password);

		[When(@"вводит некорректный текущий пароль ""(.*)""")]
		public async Task WhenEnterWrongCurrentPassword(string password) =>
			await Page.FillAsync("#CurrentPassword", password);

		[Then(@"появляется сообщение о неудаче: ""(.*)""")]
		public async Task ThenDisplayFailureMessageWithColon(string message)
		{
			var msg = Page.GetByText(message, new() { Exact = false }).First;
			await Microsoft.Playwright.Assertions.Expect(msg).ToBeVisibleAsync();
		}

		[Then(@"появляется сообщение об успехе: ""(.*)""")]
		public async Task ThenDisplaySuccessMessageWithColon(string message)
		{
			var msg = Page.GetByText(message, new() { Exact = false }).First;
			await Microsoft.Playwright.Assertions.Expect(msg).ToBeVisibleAsync();
		}

		[Then(@"пользователь перенаправляется на страницу подписки")]
		public async Task ThenRedirectedToSubscriptionPage()
		{
			await Microsoft.Playwright.Assertions.Expect(Page)
				.ToHaveURLAsync(new Regex(".*[Ss]ubscription.*"));
		}

		[Then(@"пользователь не может совершить вход по старым данным")]
		public async Task ThenCannotLoginWithOldCredentials()
		{
			var freshPage = await CreateFreshPage();
			await freshPage.GotoAsync($"{BaseUrl}/Account/Login");

			await freshPage.FillAsync("#Email", TestUserEmail);
			await freshPage.FillAsync("#Password", TestUserPassword);
			await freshPage.ClickAsync("button:has-text('Войти')");
			await freshPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			bool stayedOnLogin = freshPage.Url.Contains("/Account/Login");
			bool hasLoginError = false;
			try
			{
				var error = freshPage.Locator(
					".validation-summary-errors, .text-danger, .alert-danger").First;
				hasLoginError = await error.IsVisibleAsync(new() { Timeout = 2000 });
			}
			catch { }

			Assert.True(stayedOnLogin || hasLoginError,
				$"Вход по старым данным не должен был сработать. URL: {freshPage.Url}");
			await freshPage.CloseAsync();

			UpdateCredentials(newEmail: "alisa@gmail.com");
			await DeleteAndRecreateAccount();
		}

		[Then(@"пользователь успешно выполняет вход по новым данным")]
		public async Task ThenCanLoginWithNewCredentials()
		{
			var freshPage = await CreateFreshPage();
			await freshPage.GotoAsync($"{BaseUrl}/Account/Login");
			await freshPage.FillAsync("#Email", TestUserEmail);
			await freshPage.FillAsync("#Password", TestUserPassword);
			await freshPage.ClickAsync("button:has-text('Войти')");
			await Microsoft.Playwright.Assertions.Expect(freshPage)
				.ToHaveURLAsync($"{BaseUrl}/");
			ResetCredentials();
			await DeleteAndRecreateAccount();
			await freshPage.CloseAsync();
		}

		[Then(@"пользователь может произвести вход по новым данным")]
		public async Task ThenCanLoginWithNewPassword()
		{
			var freshPage = await CreateFreshPage();
			await freshPage.GotoAsync($"{BaseUrl}/Account/Login");
			await freshPage.FillAsync("#Email", TestUserEmail);
			await freshPage.FillAsync("#Password", "newpassword456");
			await freshPage.ClickAsync("button:has-text('Войти')");
			await Microsoft.Playwright.Assertions.Expect(freshPage)
				.ToHaveURLAsync($"{BaseUrl}/");
			await freshPage.CloseAsync();

			UpdateCredentials(newPassword: "qwerty123!@#");

			await DeleteAndRecreateAccount();
		}

		[Then(@"изменение не происходит")]
		public async Task ThenNoChangesMade()
		{
			await Microsoft.Playwright.Assertions.Expect(Page)
				.ToHaveURLAsync($"{BaseUrl}/Profile/Update");
		}

		[Then(@"изменение не происходит второе")]
		public async Task ThenNoChangesMadeSecond()
		{
			await Microsoft.Playwright.Assertions.Expect(Page)
				.ToHaveURLAsync($"{BaseUrl}/Profile");
		}

		[Then(@"пользователь выходит из аккаунта")]
		public async Task ThenUserLoggedOut()
		{
			await Microsoft.Playwright.Assertions.Expect(Page)
				.ToHaveURLAsync($"{BaseUrl}/");
		}

		[Then(@"пользователь больше не может зайти в свой аккаунт")]
		public async Task ThenCannotLoginAnymore()
		{
			var freshPage = await CreateFreshPage();
			await freshPage.GotoAsync($"{BaseUrl}/Account/Login");
			await freshPage.FillAsync("#Email", TestUserEmail);
			await freshPage.FillAsync("#Password", TestUserPassword);
			await freshPage.ClickAsync("button:has-text('Войти')");
			await freshPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			Assert.Contains("/Account/Login", freshPage.Url);

			var errorLocator = freshPage.Locator(
				".validation-summary-errors, .text-danger, .alert-danger").First;
			await Microsoft.Playwright.Assertions.Expect(errorLocator)
				.ToBeVisibleAsync(new() { Timeout = 5000 });

			await freshPage.CloseAsync();
		}

		[Then(@"пользователь перенаправляется на страницу авторизации")]
		public async Task ThenRedirectedToLoginPage()
		{
			await Microsoft.Playwright.Assertions.Expect(Page)
				.ToHaveURLAsync(new Regex(".*[Aa]ccount/[Ll]ogin.*"));
		}
	}
}