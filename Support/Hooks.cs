using Microsoft.Playwright;
using TechTalk.SpecFlow;
using System;
using System.Threading.Tasks;

namespace PlaywrightTests.Support
{
	[Binding]
	public class Hooks
	{
		private readonly ScenarioContext _scenarioContext;
		private static IPlaywright? _playwright;
		private static IBrowser? _browser;

		public Hooks(ScenarioContext scenarioContext)
		{
			_scenarioContext = scenarioContext;
		}

		[BeforeTestRun]
		public static async Task BeforeTestRun()
		{
			_playwright = await Playwright.CreateAsync();
			_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
			{
				Headless = false,
				SlowMo = 1000
			});
		}

		[BeforeScenario(Order = 1)]
		public async Task BeforeScenario()
		{
			if (_browser == null)
			{
				throw new InvalidOperationException("Browser is not initialized");
			}

			var context = await _browser.NewContextAsync(new BrowserNewContextOptions
			{
				ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
			});

			var page = await context.NewPageAsync();

			_scenarioContext.Add("Page", page);
			_scenarioContext.Add("Context", context);
			_scenarioContext.Add("Browser", _browser);
		}

		[AfterScenario(Order = 1)]
		public async Task AfterScenario()
		{
			if (_scenarioContext.ContainsKey("Context"))
			{
				var context = _scenarioContext.Get<IBrowserContext>("Context");
				if (context != null)
				{
					await context.CloseAsync();
				}
			}

			foreach (var key in _scenarioContext.Keys)
			{
				if (key.ToString().StartsWith("FreshContext_"))
				{
					var freshContext = _scenarioContext.Get<IBrowserContext>(key.ToString());
					if (freshContext != null)
					{
						await freshContext.CloseAsync();
					}
				}
			}
		}

		[AfterTestRun]
		public static async Task AfterTestRun()
		{
			if (_browser != null)
			{
				await _browser.CloseAsync();
			}
			_playwright?.Dispose();
		}
	}
}