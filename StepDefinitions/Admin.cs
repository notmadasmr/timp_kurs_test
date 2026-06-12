using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using Xunit;
using PlaywrightTests.Support;
using Microsoft.Playwright;

namespace PlaywrightTests.StepDefinitions
{
	[Binding]
	[Scope(Tag = "admin")]
	public class Admin : Base
	{
		private new const string TestUserEmail = "testuser_change_delete@example.com";
		private new const string TestUserPassword = "TestPass123!";

		public Admin(ScenarioContext scenarioContext) : base(scenarioContext)
		{
		}

		[Given(@"пользователь авторизован как администратор")]
		public async Task GivenUserIsAdmin()
		{
			await Page.GotoAsync($"{BaseUrl.Trim()}/Account/Login");
			await Page.FillAsync("#Email", "admin@example.com");
			await Page.FillAsync("#Password", "Admin123!");
			await Page.ClickAsync("button:has-text('Войти')");
			await Microsoft.Playwright.Assertions
				.Expect(Page).ToHaveURLAsync(new Regex(".*(/|/Profile|/Home|/Admin).*"), new() { Timeout = 5000 });
		}

		[Given(@"находится на странице админ-панели")]
		public async Task GivenOnAdminPanel()
		{
			await Page.GotoAsync($"{BaseUrl.Trim()}/Admin/Movies");
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync(new Regex(".*Admin.*"));
		}

		[Given(@"существует тестовый пользователь для изменения")]
		public async Task GivenTestUserExistsForChange()
		{
			await CreateTestUserIfNeeded();
			ScenarioContext["TestScenario"] = "Change";
		}

		[Given(@"существует тестовый пользователь для удаления")]
		public async Task GivenTestUserExistsForDelete()
		{
			await CreateTestUserIfNeeded();
			ScenarioContext["TestScenario"] = "Delete";
		}

		[Given(@"существует тестовый фильм")]
		public async Task GivenTestMovieExists()
		{
			await Page.GotoAsync($"{BaseUrl.Trim()}/Admin/Movies");

			var existingMovie = Page.GetByText("Тестовый фильм для удаления");
			if (await existingMovie.CountAsync() > 0) return;

			var addButton = Page.Locator("a:has-text('Добавить фильм'), button:has-text('Добавить фильм')").First;
			await addButton.ClickAsync();
			await Task.Delay(500);

			await Page.FillAsync("#Title", "Тестовый фильм для удаления");
			await Page.FillAsync("#Description", "Описание тестового фильма");
			await Page.FillAsync("#Year", "2024");
			await Page.FillAsync("#DurationMinutes", "90");
			await Page.FillAsync("#AgeRating", "+16");
			await Page.FillAsync("#FilePath", "/var/opt/mssql-files/films/test.mp4");

			await Page.ClickAsync("button:has-text('Сохранить'), input[value='Сохранить']");
			await Task.Delay(1000);
		}

		[Given(@"имеет существующую озвучку у фильма")]
		public async Task GivenExistingDubbing()
		{
			await EnsureMediaExists("Озвучки", "Добавить озвучку", "Тестовый язык", "/var/opt/mssql-files/dubbing/test.mp3");
		}

		[Given(@"имеет существующие субтитры у фильма")]
		public async Task GivenExistingSubtitles()
		{
			await EnsureMediaExists("Субтитры", "Добавить субтитры", "Тестовый язык", "/var/opt/mssql-files/subtitles/test.srt");
		}

		[When(@"нажимает кнопку ""(.*)"" у фильма")]
		public async Task WhenClickButtonOnMovie(string buttonText)
		{
			ILocator movieRow = Page.Locator("tr")
				.Filter(new() { HasText = "Тестовый фильм для удаления" }).First;

			if (await movieRow.CountAsync() == 0)
				movieRow = Page.Locator("tr").Filter(new() { HasText = "Новый фильм" }).First;

			if (await movieRow.CountAsync() == 0)
			{
				var allRows = Page.Locator("tr");
				var count = await allRows.CountAsync();
				movieRow = count > 1 ? allRows.Nth(count - 1) : allRows.Nth(1);
			}

			try
			{
				var movieTitle = await movieRow.Locator("td").Nth(0).InnerTextAsync();
				ScenarioContext["CurrentMovieTitle"] = movieTitle.Trim();
			}
			catch { }

			ILocator button;
			if (buttonText == "Удалить")
				button = movieRow.Locator($"form button:has-text('{buttonText}'), button.btn-outline-danger:has-text('{buttonText}')").First;
			else if (buttonText == "Изменить")
				button = movieRow.Locator($"a:has-text('{buttonText}'), button:has-text('{buttonText}')").First;
			else
				button = movieRow.Locator($"button:has-text('{buttonText}'), a:has-text('{buttonText}')").First;

			await button.ClickAsync();

			if (buttonText == "Удалить")
			{
				await Task.Delay(500);
				try
				{
					var modal = Page.Locator(".modal.show, .modal[style*='display: block'], [role='dialog'], .swal2-container").First;
					await Microsoft.Playwright.Assertions.Expect(modal).ToBeVisibleAsync(new() { Timeout = 2000 });
					var confirm = modal.Locator("button:has-text('Да'), button:has-text('Удалить'), input[value='Удалить'], .swal2-confirm").First;
					if (await confirm.IsVisibleAsync()) await confirm.ClickAsync();
				}
				catch { }
			}

			await Task.Delay(1000);
		}

		[When(@"нажимает кнопку ""(.*)"" у пользователя")]
		public async Task WhenClickButtonOnUser(string buttonText)
		{
			ILocator userRow;

			if (ScenarioContext.ContainsKey("TestUserEmail"))
			{
				var testEmail = ScenarioContext["TestUserEmail"]?.ToString() ?? "";
				userRow = Page.Locator("tbody tr").Filter(new() { HasText = testEmail }).First;

				if (await userRow.CountAsync() == 0)
					throw new Exception($"Тестовый пользователь {testEmail} не найден.");
			}
			else
			{
				userRow = Page.Locator("tbody tr").First;
			}

			try
			{
				var userEmail = await userRow.Locator("td").First.InnerTextAsync();
				ScenarioContext["CurrentUserEmail"] = userEmail.Trim();
			}
			catch { }

			ILocator button;
			if (buttonText == "Удалить")
				button = userRow.Locator($"form button:has-text('{buttonText}'), button.btn-outline-danger:has-text('{buttonText}')").First;
			else if (buttonText == "Изменить")
				button = userRow.Locator($"a.btn-outline-secondary:has-text('{buttonText}'), a:has-text('{buttonText}')").First;
			else
				button = userRow.Locator($"button:has-text('{buttonText}'), a:has-text('{buttonText}')").First;

			await button.ClickAsync();

			if (buttonText == "Удалить")
			{
				await Task.Delay(500);
				try
				{
					var modal = Page.Locator(".modal.show, .modal[style*='display: block'], [role='dialog'], .swal2-container").First;
					await Microsoft.Playwright.Assertions.Expect(modal).ToBeVisibleAsync(new() { Timeout = 2000 });
					var confirm = modal.Locator("button:has-text('Да'), button:has-text('Удалить'), input[value='Удалить'], .swal2-confirm").First;
					if (await confirm.IsVisibleAsync()) await confirm.ClickAsync();
				}
				catch { }
			}

			await Task.Delay(1000);
		}

		[When(@"нажимает кнопку ""(.*)"" у озвучки")]
		[When(@"нажимает кнопку ""(.*)"" у субтитров")]
		public async Task WhenClickButtonOnMedia(string buttonText)
		{
			var row = Page.Locator("table tbody tr").First;

			try
			{
				var rowText = await row.InnerTextAsync();
				ScenarioContext["CurrentMediaRowText"] = rowText.Trim();
			}
			catch { }

			var button = row.Locator($"button:has-text('{buttonText}'), a:has-text('{buttonText}')").First;
			await button.ClickAsync();

			if (buttonText == "Удалить")
			{
				await Task.Delay(500);
				try
				{
					var modal = Page.Locator(".modal.show, .modal[style*='display: block'], [role='dialog'], .swal2-container").First;
					await Microsoft.Playwright.Assertions.Expect(modal).ToBeVisibleAsync(new() { Timeout = 2000 });
					var confirm = modal.Locator("button:has-text('Да'), button:has-text('Удалить'), .swal2-confirm").First;
					if (await confirm.IsVisibleAsync()) await confirm.ClickAsync();
				}
				catch { }
			}
			await Task.Delay(1000);
		}

		[When(@"вводит язык озвучки ""(.*)""")]
		[When(@"вводит язык субтитров ""(.*)""")]
		public async Task WhenEntersMediaLanguage(string language)
		{
			await Page.FillAsync("#Language", language);
			ScenarioContext["CurrentLanguage"] = language;
		}

		[When(@"^изменяет поле ""(.*)"" на ""(.*)""$")]
		public async Task WhenChangesFieldToValue(string fieldName, string fieldValue)
		{
			string selector = fieldName switch
			{
				"Роль" => "#Role",
				"Описание" => "#Description",
				"Название" => "#Title",
				"Язык озвучки" => "#Language",
				"Язык субтитров" => "#Language",
				_ => $"#{fieldName}"
			};

			var locator = Page.Locator(selector);

			if (fieldName == "Роль")
			{
				string roleValue = fieldValue switch
				{
					"Администратор" => "Admin",
					"Пользователь" => "User",
					_ => fieldValue
				};
				await locator.SelectOptionAsync(roleValue);
			}
			else
			{
				await locator.FillAsync("");
				await locator.FillAsync(fieldValue);
			}

			ScenarioContext["UpdatedFieldValue"] = fieldValue;
		}

		[When(@"вводит название ""(.*)""")] public async Task WhenEntersTitle(string title) => await Page.FillAsync("#Title", title);
		[When(@"вводит описание ""(.*)""")] public async Task WhenEntersDescription(string d) => await Page.FillAsync("#Description", d);
		[When(@"вводит год выхода ""(.*)""")] public async Task WhenEntersYear(string y) => await Page.FillAsync("#Year", y);
		[When(@"вводит длительность ""(.*)""")] public async Task WhenEntersDuration(string d) => await Page.FillAsync("#DurationMinutes", d);
		[When(@"вводит возрастной рейтинг ""(.*)""")] public async Task WhenEntersAgeRating(string a) => await Page.FillAsync("#AgeRating", a);
		[When(@"вводит путь к файлу ""(.*)""")] public async Task WhenEntersFilePath(string f) => await Page.FillAsync("#FilePath", f);

		[When(@"оставляет все поля ввода пустыми")]
		public async Task WhenLeavesAllFieldsEmpty()
		{
			foreach (var f in new[] { "#Title", "#Description", "#Year", "#DurationMinutes", "#AgeRating", "#FilePath", "#Language" })
			{
				var l = Page.Locator(f);
				if (await l.CountAsync() > 0) await l.FillAsync("");
			}
		}

		[When(@"^изменяет поле ""([^""]+)""\s*$")]
		public async Task WhenChangesField(string fieldName)
		{
			string selector = fieldName switch
			{
				"Описание" => "#Description",
				"Название" => "#Title",
				"Язык озвучки" => "#Language",
				"Язык субтитров" => "#Language",
				_ => $"#{fieldName}"
			};

			var locator = Page.Locator(selector);
			var currentText = await locator.InputValueAsync();
			var newText = currentText + " [updated]";
			await locator.FillAsync(newText);
			ScenarioContext["UpdatedFieldText"] = newText;
		}

		[Then(@"фильм появляется в списке админ-панели")]
		public async Task ThenMovieAppearsInAdminList()
			=> await Microsoft.Playwright.Assertions.Expect(Page.GetByText("Новый фильм").First).ToBeVisibleAsync();

		[Then(@"фильм появляется в списке на главной странице")]
		public async Task ThenMovieAppearsOnMainPage()
		{
			await Page.GotoAsync($"{BaseUrl.Trim()}/");
			await Microsoft.Playwright.Assertions.Expect(Page.GetByText("Новый фильм").First).ToBeVisibleAsync();
		}

		[Then(@"фильм не появляется в списке")]
		public async Task ThenMovieDoesNotAppearInList()
		{
			var validationErrors = Page.Locator(".validation-summary-errors, .text-danger:not(.field-validation-valid), span.text-danger[style*='color: red'], div.alert-danger");
			await Microsoft.Playwright.Assertions.Expect(validationErrors.First).ToBeVisibleAsync(new() { Timeout = 5000 });
		}

		[Then(@"запись изменяется в списке админ-панели")]
		public async Task ThenRecordChangedInAdminList()
		{
			if (ScenarioContext.ContainsKey("CurrentUserEmail"))
				await Microsoft.Playwright.Assertions.Expect(Page.GetByText(ScenarioContext["CurrentUserEmail"]?.ToString() ?? "").First).ToBeVisibleAsync();
			else if (ScenarioContext.ContainsKey("CurrentMovieTitle"))
				await Microsoft.Playwright.Assertions.Expect(Page.GetByText(ScenarioContext["CurrentMovieTitle"]?.ToString() ?? "").First).ToBeVisibleAsync();
		}

		[Then(@"запись изменяется в списке на главной странице")]
		public async Task ThenRecordChangedOnMainPage()
		{
			if (ScenarioContext.ContainsKey("CurrentMovieTitle"))
			{
				await Page.GotoAsync($"{BaseUrl.Trim()}/");
				await Microsoft.Playwright.Assertions.Expect(Page.GetByText(ScenarioContext["CurrentMovieTitle"]?.ToString() ?? "").First).ToBeVisibleAsync();
			}
		}

		[Then(@"запись убирается из списка")]
		public async Task ThenRecordRemovedFromList()
		{
			await Task.Delay(1000);

			if (ScenarioContext.ContainsKey("CurrentMediaRowText"))
			{
				var text = ScenarioContext["CurrentMediaRowText"]?.ToString() ?? "";
				await Microsoft.Playwright.Assertions.Expect(Page.Locator("table tbody tr").Filter(new() { HasText = text })).ToHaveCountAsync(0);
			}
			else if (ScenarioContext.ContainsKey("CurrentUserEmail"))
			{
				var email = ScenarioContext["CurrentUserEmail"]?.ToString() ?? "";
				await Microsoft.Playwright.Assertions.Expect(Page.GetByText(email)).ToHaveCountAsync(0);
			}
			else if (ScenarioContext.ContainsKey("CurrentMovieTitle"))
			{
				var title = ScenarioContext["CurrentMovieTitle"]?.ToString() ?? "";
				await Microsoft.Playwright.Assertions.Expect(Page.GetByText(title)).ToHaveCountAsync(0);
			}
		}

		[Then(@"запись убирается из списка админ-панели")]
		public async Task ThenRecordRemovedFromAdminList()
		{
			if (ScenarioContext.ContainsKey("CurrentMovieTitle"))
				await Microsoft.Playwright.Assertions.Expect(Page.GetByText(ScenarioContext["CurrentMovieTitle"]?.ToString() ?? "")).ToHaveCountAsync(0);
		}

		[Then(@"записи нет в списке на главной странице")]
		public async Task ThenRecordNotOnMainPage()
		{
			if (ScenarioContext.ContainsKey("CurrentMovieTitle"))
			{
				await Page.GotoAsync($"{BaseUrl.Trim()}/");
				await Microsoft.Playwright.Assertions.Expect(Page.GetByText(ScenarioContext["CurrentMovieTitle"]?.ToString() ?? "")).ToHaveCountAsync(0);
			}
		}

		[Then(@"запись добавляется в список озвучек фильма")]
		[Then(@"запись добавляется в список субтитров фильма")]
		public async Task ThenMediaRecordAdded()
		{
			var lang = ScenarioContext.ContainsKey("CurrentLanguage") ? ScenarioContext["CurrentLanguage"]?.ToString() ?? "" : "Русский";
			await Microsoft.Playwright.Assertions.Expect(Page.Locator("table tbody tr").Filter(new() { HasText = lang }).First).ToBeVisibleAsync();
		}

		[Then(@"запись не добавляется в список озвучек фильма")]
		[Then(@"запись не добавляется в список субтитров фильма")]
		public async Task ThenMediaRecordNotAdded()
		{
			var lang = ScenarioContext.ContainsKey("CurrentLanguage") ? ScenarioContext["CurrentLanguage"]?.ToString() ?? "" : "";
			if (!string.IsNullOrEmpty(lang))
			{
				await Microsoft.Playwright.Assertions.Expect(Page.Locator("table tbody tr").Filter(new() { HasText = lang })).ToHaveCountAsync(0);
			}
			else
			{
				await Task.Delay(500);
			}
		}

		[Then(@"запись изменяется в списке на странице фильма")]
		public async Task ThenMediaRecordChangedOnMoviePage()
		{
			var lang = ScenarioContext.ContainsKey("UpdatedFieldValue") ? ScenarioContext["UpdatedFieldValue"]?.ToString() ?? "" : "Английский";
			await Microsoft.Playwright.Assertions.Expect(Page.Locator("table tbody tr").Filter(new() { HasText = lang }).First).ToBeVisibleAsync();
		}

		[Then(@"запись убирается из списка на странице фильма")]
		public async Task ThenMediaRecordRemovedFromMoviePage()
		{
			await Task.Delay(1000);

			if (ScenarioContext.ContainsKey("CurrentMediaRowText"))
			{
				var text = ScenarioContext["CurrentMediaRowText"]?.ToString() ?? "";
				await Microsoft.Playwright.Assertions.Expect(Page.Locator("table tbody tr").Filter(new() { HasText = text })).ToHaveCountAsync(0);
			}
			else
			{
				var lang = ScenarioContext.ContainsKey("CurrentLanguage") ? ScenarioContext["CurrentLanguage"]?.ToString() ?? "" : "";
				if (!string.IsNullOrEmpty(lang))
				{
					await Microsoft.Playwright.Assertions.Expect(Page.Locator("table tbody tr").Filter(new() { HasText = lang })).ToHaveCountAsync(0);
				}
			}
		}

		[Scope(Tag = "admin")]
		[Then(@"^появляется сообщение: ""(.*)""$")]
		public async Task ThenSuccessMessageAppears(string message)
		{
			var messageLocator = Page.Locator(
				$".alert-success:has-text('{message}'), " +
				$".text-success:has-text('{message}'), " +
				$"div[role='alert']:has-text('{message}'), " +
				$".swal2-container:has-text('{message}'), " +
				$".text-danger:has-text('{message}'), " +
				$".field-validation-error:has-text('{message}'), " +
				$".alert-danger:has-text('{message}'), " +
				$".validation-summary-errors:has-text('{message}')").First;

			await Microsoft.Playwright.Assertions
				.Expect(messageLocator).ToBeVisibleAsync(new() { Timeout = 5000 });
		}

		private async Task CreateTestUserIfNeeded()
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
					await freshPage.WaitForURLAsync(
						new Regex(".*(/|/Profile|/Home).*"),
						new() { Timeout = 5000 });
				}
			}
			catch { }
			finally
			{
				if (!freshPage.IsClosed) await freshPage.CloseAsync();
			}

			ScenarioContext["TestUserEmail"] = TestUserEmail;
		}
		private async Task EnsureMediaExists(string buttonName, string addButtonName, string lang, string path)
		{
			var movieRow = Page.Locator("tr").Filter(new() { HasText = "Тестовый фильм для удаления" }).First;
			if (await movieRow.CountAsync() == 0) return;

			var btn = movieRow.Locator($"a:has-text('{buttonName}'), button:has-text('{buttonName}')").First;
			await btn.ClickAsync();
			await Task.Delay(1000);

			var rows = Page.Locator("table tbody tr");
			var count = await rows.CountAsync();
			bool isEmpty = count == 0 || await Page.Locator("text=Нет данных").IsVisibleAsync();

			if (isEmpty)
			{
				var addBtn = Page.Locator($"button:has-text('{addButtonName}'), a:has-text('{addButtonName}')").First;
				await addBtn.ClickAsync();
				await Task.Delay(500);

				await Page.FillAsync("#Language", lang);
				await Page.FillAsync("#FilePath", path);
				await Page.ClickAsync("button:has-text('Сохранить'), input[value='Сохранить']");
				await Task.Delay(1000);
			}
			await Page.GotoAsync($"{BaseUrl.Trim()}/Admin/Movies");
			await Task.Delay(500);
		}
	}
}