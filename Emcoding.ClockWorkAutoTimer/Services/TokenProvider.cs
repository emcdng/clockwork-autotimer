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
            
            var moreOptionsButton = _page.GetByTestId("more-options-button-idf-testid");

            if (await moreOptionsButton.CountAsync() > 0)
            {
                await moreOptionsButton.ClickAsync();
                await moreOptionsButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Microsoft" }).ClickAsync();
            }
            else
            {
                var alternativeMicrosoftButton = _page.Locator("#microsoft-auth-button");
                await alternativeMicrosoftButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                await alternativeMicrosoftButton.ClickAsync();
            }
            
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
            
            // Check whether MFA is required or if the authentication flow proceeds without it
            var displaySignDiv = _page.Locator("#idRichContext_DisplaySign");
            var kmsiButton = _page.Locator("#idSIButton9");

            var isMfaRequired = false;
            var detectionTimeout = TimeSpan.FromSeconds(10);
            var detectionStart = DateTime.UtcNow;

            while (DateTime.UtcNow - detectionStart < detectionTimeout)
            {
                try
                {
                    if (await displaySignDiv.IsVisibleAsync())
                    {
                        isMfaRequired = true;
                        break;
                    }

                    if (await kmsiButton.IsVisibleAsync())
                    {
                        break;
                    }

                    var currentUrl = _page.Url;
                    if (!string.IsNullOrEmpty(currentUrl) &&
                        !currentUrl.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) &&
                        !currentUrl.Contains("login.live.com", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
                catch
                {
                    // Ignore transient exceptions during rapid redirects
                }

                await Task.Delay(200);
            }

            if (isMfaRequired)
            {
                var authenticatorNumber = await displaySignDiv.InnerTextAsync();

                logger.LogWarning("Waiting up to {Seconds} seconds to confirm number {AuthenticatorNumber} in your Microsoft Authenticator App to continue", options.Value.WaitingForTwoFactorSeconds, authenticatorNumber);

                try
                {
                    using var mfaTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.Value.WaitingForTwoFactorSeconds));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, mfaTimeoutCts.Token);

                    var disappearedTask = displaySignDiv.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Hidden
                    });
                    var navigatedTask = _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    var completedTask = await Task.WhenAny(disappearedTask, navigatedTask).WaitAsync(linkedCts.Token);

                    logger.LogInformation(completedTask == disappearedTask
                        ? "Microsoft Authenticator number was confirmed successfully."
                        : "Page navigation/load detected.");
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("MFA wait period ended.");
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "MFA wait finished with exception: {Message}", ex.Message);
                }
            }
            else
            {
                logger.LogInformation("MFA was not required or was skipped.");
            }

            // Handle optional 'Stay signed in?' prompt and wait for redirection to Atlassian to complete
            var authRedirectTimeout = TimeSpan.FromSeconds(60);
            var authRedirectStart = DateTime.UtcNow;

            while (DateTime.UtcNow - authRedirectStart < authRedirectTimeout)
            {
                try
                {
                    if (await kmsiButton.IsVisibleAsync())
                    {
                        logger.LogInformation("Confirming 'Stay signed in' prompt...");
                        await kmsiButton.ClickAsync();
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "KMSI prompt check: {Message}", ex.Message);
                }

                var currentUrl = _page.Url ?? string.Empty;

                var isOnMicrosoft = currentUrl.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) ||
                                    currentUrl.Contains("login.live.com", StringComparison.OrdinalIgnoreCase) ||
                                    currentUrl.Contains("msft.sts.microsoft.com", StringComparison.OrdinalIgnoreCase);

                var isAtlassianLoginOrAuth = currentUrl.Contains("id.atlassian.com/login", StringComparison.OrdinalIgnoreCase) ||
                                             currentUrl.Contains("id.atlassian.com/v2/login", StringComparison.OrdinalIgnoreCase) ||
                                             currentUrl.Contains("id.atlassian.com/logout", StringComparison.OrdinalIgnoreCase) ||
                                             currentUrl.Contains("auth.atlassian.com", StringComparison.OrdinalIgnoreCase) ||
                                             currentUrl.Contains("/login/saml", StringComparison.OrdinalIgnoreCase) ||
                                             currentUrl.Contains("/callback", StringComparison.OrdinalIgnoreCase) ||
                                             currentUrl.Contains("/login/authorize", StringComparison.OrdinalIgnoreCase) ||
                                             currentUrl.Contains("social_login_managed_user", StringComparison.OrdinalIgnoreCase);

                var isAuthComplete = !isOnMicrosoft && !isAtlassianLoginOrAuth && !string.IsNullOrWhiteSpace(currentUrl) && (
                    currentUrl.Contains("home.atlassian.com", StringComparison.OrdinalIgnoreCase) ||
                    currentUrl.Contains("start.atlassian.com", StringComparison.OrdinalIgnoreCase) ||
                    currentUrl.Contains($"{companyDomain}.atlassian.net", StringComparison.OrdinalIgnoreCase) ||
                    currentUrl.Contains("id.atlassian.com", StringComparison.OrdinalIgnoreCase) ||
                    currentUrl.Contains("atlassian.net", StringComparison.OrdinalIgnoreCase));

                if (isAuthComplete)
                {
                    logger.LogInformation("Authentication completed successfully ({Url}). Navigating to Jira...", currentUrl);
                    break;
                }

                await Task.Delay(500);
            }

            try
            {
                await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 15000 });
            }
            catch
            {
                // Ignore load state timeout if already transitioned
            }
            
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
            
            return CleanToken(authorizationHeader);
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

        void TryExtractTokenFromRequest(IRequest request)
        {
            try
            {
                var url = request.Url;
                if (!url.Contains("clockwork.report", StringComparison.OrdinalIgnoreCase))
                    return;

                // 1. Check Authorization header (case-insensitive)
                foreach (var header in request.Headers)
                {
                    if (string.Equals(header.Key, "authorization", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(header.Value))
                    {
                        var token = CleanToken(header.Value);
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            tcs.TrySetResult(token);
                            return;
                        }
                    }
                }

                // 2. Check query parameter `jwt`
                var match = System.Text.RegularExpressions.Regex.Match(url, @"[?&]jwt=([^&#]+)");
                if (match.Success)
                {
                    var jwtParam = Uri.UnescapeDataString(match.Groups[1].Value);
                    var token = CleanToken(jwtParam);
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        tcs.TrySetResult(token);
                        return;
                    }
                }
            }
            catch
            {
                // Ignore extraction failures on individual requests
            }
        }

        void OnRequest(object? _, IRequest request) => TryExtractTokenFromRequest(request);

        page.Request += OnRequest;

        try
        {
            // Check existing frames in case Clockwork is already loaded/loading
            foreach (var frame in page.Frames)
            {
                var frameUrl = frame.Url;
                if (!string.IsNullOrEmpty(frameUrl) && frameUrl.Contains("clockwork.report", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(frameUrl, @"[?&]jwt=([^&#]+)");
                    if (match.Success)
                    {
                        var jwtParam = Uri.UnescapeDataString(match.Groups[1].Value);
                        var token = CleanToken(jwtParam);
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            return token;
                        }
                    }
                }
            }

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
            throw new TimeoutException("Authorization token from clockwork.report could not be captured within 2 minutes.");
        }
        finally
        {
            page.Request -= OnRequest;
        }
    }

    private static string CleanToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        var trimmed = token.Trim();
        if (trimmed.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return trimmed[7..].Trim();
        if (trimmed.StartsWith("JWT ", StringComparison.OrdinalIgnoreCase))
            return trimmed[4..].Trim();

        return trimmed;
    }
}