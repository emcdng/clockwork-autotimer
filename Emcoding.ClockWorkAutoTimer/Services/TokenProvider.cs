using Emcoding.ClockWorkAutoTimer.Options;
using Emcoding.ClockWorkAutoTimer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Emcoding.ClockWorkAutoTimer.Services;

public class TokenProvider(
    IOptions<ClockWorkAutoTimerOptions> options,
    ILogger<TokenProvider> logger) : ITokenProvider
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public async Task<string> RetrieveTokenAsync(CancellationToken cancellationToken = default)
    {
        var email = options.Value.UserEmail;
        var password = options.Value.UserPassword;
        var companyDomain = options.Value.SubDomain;

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("ClockWorkAutoTimerOptions.UserEmail is empty.");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("ClockWorkAutoTimerOptions.UserPassword is empty.");

        if (string.IsNullOrWhiteSpace(companyDomain))
            throw new InvalidOperationException("ClockWorkAutoTimerOptions.CompanyDomain is empty.");

        await using var _ = cancellationToken.Register(() => logger.LogWarning("Token request was cancelled."));
        await _sync.WaitAsync(cancellationToken);

        try
        {
            logger.LogInformation("Starting authentication process for domain {Domain}.atlassian.net.", companyDomain);

            if (_context is null || _page is null || _browser is null || _playwright is null)
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = options.Value.Headless
                });

                _context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    IgnoreHTTPSErrors = false
                });

                _context.SetDefaultTimeout(60000);
                _context.SetDefaultNavigationTimeout(90000);
                _page = await _context.NewPageAsync();
            }

            await _page.GotoAsync("https://id.atlassian.com/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 90000
            });
            
            logger.LogInformation("Login page loaded...");
            
            // Microsoft Login Button
            var microsoftButton = _page.Locator("#microsoft-auth-button");
            await microsoftButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await microsoftButton.ClickAsync();
            
            logger.LogInformation("Filling in e-mail...");
            
            // E-Mail address
            var loginInput = _page.Locator("input[name='loginfmt']");
            await loginInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await loginInput.FillAsync(options.Value.UserEmail);
            
            var nextButton = _page.Locator("input[type='submit']");
            await nextButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await nextButton.ClickAsync();            
            
            logger.LogInformation("Filling in password...");

            // Password
            var passwordInput = _page.Locator("input[name='passwd']");
            await passwordInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await passwordInput.FillAsync(options.Value.UserPassword);
            
            var signInButton = _page.Locator("input[type='submit']");
            await signInButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await signInButton.ClickAsync();  
            
            // Microsoft Authenticator Number
            var displaySignDiv = _page.Locator("#idRichContext_DisplaySign");
            await displaySignDiv.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            var authenticatorNumber = await displaySignDiv.InnerTextAsync();
            
            logger.LogWarning("Waiting for {Seconds} seconds to confirm number {AuthenticatorNumber} in your Microsoft Authenticator App to continue", options.Value.WaitingForTwoFactorSeconds,  authenticatorNumber);
            
            await WaitWithCountdownAsync(options.Value.WaitingForTwoFactorSeconds);
            
            var disappearedTask = displaySignDiv.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Detached // or Hidden
            });

            var navigatedTask = _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            var finished = await Task.WhenAny(disappearedTask, navigatedTask);

            logger.LogInformation(finished == disappearedTask
                ? "Microsoft Authenticator number was confirmed successfully."
                : "Page navigation/load detected.");
            
            logger.LogInformation($"Lade {companyDomain}.atlassian.net ...");
            
            await _page.GotoAsync($"https://{companyDomain}.atlassian.net", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 90000
            });
            
            logger.LogInformation("Waiting for JIRA response...");
            
            var targetUrl =
                $"https://{companyDomain}.atlassian.net/jira/apps/{options.Value.AppId}/{options.Value.AppEnvId}/my-work";

            logger.LogInformation("Waiting to receive ClockWork bearer token...");
            
            var authorizationHeader = await CaptureClockworkAuthorizationHeaderAsync(
                _page,
                targetUrl,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                logger.LogError("No clockwork bearer token received.");
            }
            else
            {
                logger.LogInformation("ClockWork bearer token received.");
            }
            
            return authorizationHeader.Replace("Bearer ", "");
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task CloseBrowserAsync()
    {
        await _sync.WaitAsync();

        try
        {
            if (_context is not null)
            {
                await _context.CloseAsync();
                _context = null;
            }

            if (_browser is not null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            _page = null;

            if (_playwright is not null)
            {
                _playwright.Dispose();
                _playwright = null;
            }
        }
        finally
        {
            _sync.Release();
        }
    }
    
    private static async Task<string> CaptureClockworkAuthorizationHeaderAsync(
        IPage page,
        string myWorkUrl,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnRequest(object? _, IRequest request)
        {
            if (!request.Url.StartsWith("https://app.clockwork.report/worklogs.json", StringComparison.OrdinalIgnoreCase))
                return;

            if (request.Headers.TryGetValue("authorization", out var auth) && !string.IsNullOrWhiteSpace(auth))
                tcs.TrySetResult(auth);
        }

        page.Request += OnRequest;

        try
        {
            await page.GotoAsync(myWorkUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 90000
            });

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var authHeader = await tcs.Task.WaitAsync(linkedCts.Token);
            return authHeader;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Authorization header from worklogs.json could not be captured within 2 minutes.");
        }
        finally
        {
            page.Request -= OnRequest;
        }
    }
    
    private async Task WaitWithCountdownAsync(int seconds)
    {
        for (var i = seconds; i > 0; i--)
        {
            await Task.Delay(1000);
        }
    }
}