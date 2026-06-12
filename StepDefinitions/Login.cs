using Microsoft.Playwright;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using Xunit;
using PlaywrightTests.Support;

namespace PlaywrightTests.StepDefinitions
{
	[Binding]
	public class Login : Base
	{
		public Login(ScenarioContext scenarioContext)
			: base(scenarioContext)
		{
		}

		[Given("пользователь находится на странице авторизации")]
		public async Task GivenUserOnLoginPage()
		{
			await Page.GotoAsync($"{BaseUrl}/Account/Login");
		}

		[When(@"вводит существующую почту ""(.*)""")]
		public async Task WhenEnterExistingEmail(string email)
		{
			await Page.FillAsync("#Email", email);
		}

		[When(@"вводит корректный пароль ""(.*)""")]
		public async Task WhenEnterCorrectPassword(string password)
		{
			await Page.FillAsync("#Password", password);
		}

		[When(@"вводит неверный пароль ""(.*)""")]
		public async Task WhenEnterWrongPassword(string password)
		{
			await Page.FillAsync("#Password", password);
		}

		[When(@"оставляет поля формы авторизации пустыми")]
		public async Task WhenLeaveLoginFormFieldsEmpty()
		{
			await Page.FillAsync("#Email", "");
			await Page.FillAsync("#Password", "");
		}

		[Then(@"выполняется вход в систему")]
		public async Task ThenLoginSuccessful()
		{
			await Microsoft.Playwright.Assertions.Expect(Page).Not.ToHaveURLAsync($"{BaseUrl}/Account/Login");
		}

		[Then(@"вход не выполняется")]
		public async Task ThenLoginFails()
		{
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync($"{BaseUrl}/Account/Login");
		}

		[Then(@"отображается главная страница")]
		public async Task ThenHomePageDisplayed()
		{
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync($"{BaseUrl}/");
		}

		[Then(@"появляется сообщение ""(.*)""")]
		public async Task ThenMessageDisplayed(string message)
		{
			var msg = Page.GetByText(message, new() { Exact = false }).First;
			await Microsoft.Playwright.Assertions.Expect(msg).ToBeVisibleAsync();
		}

		[Then(@"появляется сообщение рядом с полем ""(.*)"": ""(.*)""")]
		public async Task ThenFieldValidationMessageDisplayed(string fieldName, string message)
		{
			var msg = Page.GetByText(message, new() { Exact = false }).First;
			await Microsoft.Playwright.Assertions.Expect(msg).ToBeVisibleAsync();
		}
	}
}