using Microsoft.Playwright;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;

namespace PlaywrightTests.Support
{
	[Binding]
	public class Base
	{
		protected ScenarioContext ScenarioContext { get; }
		protected IPage Page => ScenarioContext.Get<IPage>("Page");
		protected IBrowserContext Context => ScenarioContext.Get<IBrowserContext>("Context");

		protected const string BaseUrl = "http://localhost";
		protected static string TestUserEmail = "alisa@test.com";
		protected static string TestUserPassword = "qwerty123!@#";
		protected const string AdminEmail = "admin@example.com";
		protected const string AdminPassword = "Admin123!";

		public Base(ScenarioContext scenarioContext)
		{
			ScenarioContext = scenarioContext;
		}

		protected async Task LoginAsUser(string email = null, string password = null)
		{
			email ??= TestUserEmail;
			password ??= TestUserPassword;

			await Page.GotoAsync($"{BaseUrl}/Account/Login",
				new() { WaitUntil = WaitUntilState.DOMContentLoaded });
			await Page.FillAsync("#Email", email);
			await Page.FillAsync("#Password", password);
			await Page.ClickAsync("button:has-text('Войти')");

			await Page.WaitForURLAsync(
				new Regex(".*(/|/Profile|/Home).*"),
				new() { Timeout = 10000, WaitUntil = WaitUntilState.DOMContentLoaded }
			).ContinueWith(t => { });
		}

		protected async Task LoginAsAdmin()
		{
			await Page.GotoAsync($"{BaseUrl}/Account/Login",
				new() { WaitUntil = WaitUntilState.DOMContentLoaded });
			await Page.FillAsync("#Email", AdminEmail);
			await Page.FillAsync("#Password", AdminPassword);
			await Page.ClickAsync("button:has-text('Войти')");
			await Page.WaitForURLAsync(
				new Regex(".*(/|/Profile|/Home).*"),
				new() { Timeout = 10000, WaitUntil = WaitUntilState.DOMContentLoaded }
			).ContinueWith(t => { });
		}

		protected async Task<IPage> CreateFreshPage()
		{
			var browser = ScenarioContext.Get<IBrowser>("Browser");
			var freshContext = await browser.NewContextAsync();
			var freshPage = await freshContext.NewPageAsync();
			ScenarioContext.Add("FreshContext_" + Guid.NewGuid(), freshContext);
			return freshPage;
		}

		public static void UpdateCredentials(string newEmail = null, string newPassword = null)
		{
			if (newEmail != null) TestUserEmail = newEmail;
			if (newPassword != null) TestUserPassword = newPassword;
		}

		public static void ResetCredentials()
		{
			TestUserEmail = "alisa@test.com";
			TestUserPassword = "qwerty123!@#";
		}
		public static async Task EnsureAccountExists()
		{
			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(
				new() { Headless = true });
			var freshContext = await browser.NewContextAsync();
			var freshPage = await freshContext.NewPageAsync();

			try
			{
				await freshPage.GotoAsync($"{BaseUrl}/Account/Login",
					new() { WaitUntil = WaitUntilState.DOMContentLoaded });
				await freshPage.FillAsync("#Email", TestUserEmail);
				await freshPage.FillAsync("#Password", TestUserPassword);
				await freshPage.ClickAsync("button:has-text('Войти')");

				try
				{
					await freshPage.WaitForURLAsync(
						new Regex("^(?!.*Account/Login).*$"),
						new() { Timeout = 5000, WaitUntil = WaitUntilState.DOMContentLoaded });
				}
				catch (TimeoutException)
				{
					await freshPage.CloseAsync();
					await RecreateTestAccount(browser);
					return;
				}
			}
			finally
			{
				if (!freshPage.IsClosed) await freshPage.CloseAsync();
			}
		}
		public static async Task DeleteAndRecreateAccount()
		{
			var uniqueEmail = $"alisa_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com";
			TestUserEmail = uniqueEmail;

			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(
				new() { Headless = true });
			var freshContext = await browser.NewContextAsync();
			var freshPage = await freshContext.NewPageAsync();

			try
			{
				await RecreateTestAccount(browser);
			}
			catch
			{
				try
				{
					await RecreateTestAccount(browser);
				}
				catch { }
			}
			finally
			{
				if (!freshPage.IsClosed) await freshPage.CloseAsync();
			}
		}

		private static async Task RecreateTestAccount(IBrowser browser)
		{
			var freshContext = await browser.NewContextAsync();
			var freshPage = await freshContext.NewPageAsync();
			try
			{
				await freshPage.GotoAsync($"{BaseUrl}/Account/Register",
					new() { WaitUntil = WaitUntilState.DOMContentLoaded });

				await freshPage.FillAsync("#Email", TestUserEmail);
				await freshPage.FillAsync("#Password", TestUserPassword);
				await freshPage.FillAsync("#ConfirmPassword", TestUserPassword);
				await freshPage.ClickAsync("button:has-text('Зарегистрироваться'), button[type='submit']");

				await freshPage.WaitForURLAsync(
					new Regex("^(?!.*Account/Register).*$"),
					new() { Timeout = 10000, WaitUntil = WaitUntilState.DOMContentLoaded });
			}
			finally
			{
				if (!freshPage.IsClosed) await freshPage.CloseAsync();
			}
		}
	}
}