using Microsoft.Playwright;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using Xunit;
using PlaywrightTests.Support;

namespace PlaywrightTests.StepDefinitions
{
	[Binding]
	public class Navigation : Base
	{
		public Navigation(ScenarioContext scenarioContext)
			: base(scenarioContext)
		{
		}

		[Given("пользователь авторизован")]
		public async Task GivenUserAuthorized()
		{
			await LoginAsUser();
		}

		[Then("происходит выход из аккаунта")]
		public async Task ThenLogoutSuccessful()
		{
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync($"{BaseUrl}/");
		}

		[Then(@"появляется сообщение: ""(.*)""")]
		public async Task ThenDisplayMessage(string message)
		{
			var msg = Page.GetByText(message, new() { Exact = false }).First;
			await Microsoft.Playwright.Assertions.Expect(msg).ToBeVisibleAsync();
		}

		[Given("пользователь находится на любой странице сайта")]
		public async Task GivenUserOnAnyPage()
		{
			await LoginAsUser();
			await Page.GotoAsync($"{BaseUrl}/Profile");
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync($"{BaseUrl}/Profile");
		}

		[Scope(Feature = "Навигация и выход из системы")]
		[Then("происходит переход на главную страницу")]
		public async Task ThenRedirectToHomePage()
		{
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync($"{BaseUrl}/");
		}
	}
}