using Microsoft.Playwright;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using Xunit;
using PlaywrightTests.Support;

namespace PlaywrightTests.StepDefinitions
{
	[Binding]
	public class Search : Base
	{
		public Search(ScenarioContext scenarioContext)
			: base(scenarioContext)
		{
		}

		[Given(@"пользователь находится на странице поиска")]
		public async Task GivenUserOnSearchPage()
		{
			await Page.GotoAsync($"{BaseUrl}/");
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync($"{BaseUrl}/");
		}

		[When(@"вводит в поисковую строку ""(.*)""")]
		public async Task WhenEnterSearchQuery(string query)
		{
			await Page.GetByPlaceholder("Введите название фильма").FillAsync(query);
		}

		[Then(@"в списке выводится фильм ""(.*)""")]
		public async Task ThenMovieDisplayed(string movieName)
		{
			var movieElement = Page.Locator($"a.list-group-item:has-text('{movieName}')");
			await Microsoft.Playwright.Assertions.Expect(movieElement).ToBeVisibleAsync();
		}

		[Then(@"выводится сообщение ""(.*)""")]
		public async Task ThenDisplayMessage(string message)
		{
			var messageElement = Page.Locator($".alert:has-text('{message}'), .search-result-message:has-text('{message}')");
			await Microsoft.Playwright.Assertions.Expect(messageElement).ToBeVisibleAsync();
		}

		[Then(@"выводится предупреждение ""(.*)""")]
		public async Task ThenDisplayAlert(string message)
		{
			var input = Page.GetByPlaceholder("Введите название фильма");
			var validationMessage = await input.EvaluateAsync<string>("el => el.validationMessage");

			Assert.Contains(message, validationMessage);
		}

		[When(@"оставляет строку поиска пустой")]
		public Task WhenLeaveSearchEmpty()
		{
			return Task.CompletedTask;
		}
	}
}