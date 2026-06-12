using Microsoft.Playwright;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using Xunit;
using PlaywrightTests.Support;
using System.Threading.Tasks;

namespace PlaywrightTests.StepDefinitions
{
	[Binding]
	public class Subscription : Base
	{
		public Subscription(ScenarioContext scenarioContext) : base(scenarioContext)
		{
		}

		[Given(@"пользователь авторизован и не имеет действующей подписки")]
		public async Task GivenUserAuthorizedWithoutSubscription()
		{
			await DeleteAndRecreateAccount();
			await LoginAsUser();
			await Page.GotoAsync($"{BaseUrl}/Profile");
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}

		[Given(@"пользователь авторизован и имеет права администратора")]
		public async Task GivenUserIsAdmin()
		{
			await LoginAsAdmin();
			await Page.GotoAsync($"{BaseUrl}/Profile");
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}

		[When(@"переходит на страницу оформления подписки")]
		public async Task WhenGoToSubscriptionPage()
		{
			var button = Page.Locator("button:has-text('Оформить подписку')")
				.Or(Page.Locator("a:has-text('Оформить подписку')"))
				.Or(Page.Locator("text='Оформить подписку'"));

			var count = await button.CountAsync();
			if (count > 0)
			{
				await button.First.ClickAsync();
				await Task.Delay(500);
			}

			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}

		[When(@"вводит номер карты ""(.*)""")]
		public async Task WhenEnterCardNumber(string cardNumber) => await Page.FillAsync("#CardNumber", cardNumber);

		[When(@"вводит имя владельца ""(.*)""")]
		public async Task WhenEnterCardHolder(string cardHolder) => await Page.FillAsync("#CardHolder", cardHolder);

		[When(@"вводит месяц ""(.*)""")]
		public async Task WhenEnterExpMonth(string month) => await Page.FillAsync("#ExpMonth", month);

		[When(@"вводит год ""(.*)""")]
		public async Task WhenEnterExpYear(string year) => await Page.FillAsync("#ExpYear", year);

		[When(@"вводит CVV ""(.*)""")]
		public async Task WhenEnterCvv(string cvv) => await Page.FillAsync("#Cvv", cvv);

		[Then(@"в личном кабинете изменяется статус подписки")]
		public async Task ThenSubscriptionStatusChanged()
		{
			await Page.GotoAsync($"{BaseUrl}/Profile");
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var pageContent = await Page.ContentAsync();
			Assert.True(pageContent.Contains("Active") || pageContent.Contains("Активна"),
				 "Статус подписки должен быть Active/Активна");
		}

		[Then(@"статус подписки не изменяется")]
		public async Task ThenSubscriptionStatusNotChanged()
		{
			await Page.GotoAsync($"{BaseUrl}/Profile");
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(1000);

			var pageContent = await Page.ContentAsync();
			bool hasActiveStatus = pageContent.Contains("Active") || pageContent.Contains("Активна");
			Assert.False(hasActiveStatus, "Статус подписки не должен быть Active/Активна");
		}

		[Then(@"пользователь может просматривать фильмы")]
		public async Task ThenUserCanWatchMovies()
		{
			await Page.GotoAsync($"{BaseUrl}/");
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var firstMovie = Page.Locator("a.list-group-item").First;
			await firstMovie.ClickAsync();
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var watchButton = Page.Locator("button:has-text('Смотреть')")
				.Or(Page.Locator("a:has-text('Смотреть')"));

			await Microsoft.Playwright.Assertions.Expect(watchButton.First).ToBeVisibleAsync();
			await watchButton.First.ClickAsync();

			await Page.WaitForURLAsync(new Regex(".*Movies/Watch.*"));
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync(new Regex(".*Movies/Watch.*"));

			await DeleteAndRecreateAccount();
		}

		[Then(@"пользователь не может просматривать фильмы")]
		public async Task ThenUserCannotWatchMovies()
		{
			await Page.GotoAsync($"{BaseUrl}/");
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var firstMovie = Page.Locator("a.list-group-item").First;
			await firstMovie.ClickAsync();
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var watchButton = Page.Locator("button:has-text('Смотреть')")
				.Or(Page.Locator("a:has-text('Смотреть')"));

			await watchButton.First.ClickAsync();

			await Page.WaitForURLAsync(new Regex(".*Subscription.*"));
			await Microsoft.Playwright.Assertions.Expect(Page).ToHaveURLAsync(new Regex(".*Subscription.*"));
		}

		[When(@"оставляет все поля пустыми")]
		public async Task WhenLeaveAllFieldsEmpty()
		{
			string[] fields = { "#CardNumber", "#CardHolder", "#ExpMonth", "#ExpYear", "#Cvv" };
			foreach (var fieldSelector in fields)
			{
				var field = Page.Locator(fieldSelector);
				if (await field.CountAsync() > 0)
				{
					await field.ClickAsync();
					await field.PressAsync("Control+a");
					await field.PressAsync("Delete");
				}
			}
		}
	}
}