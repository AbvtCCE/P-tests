using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TestPlayrights
{
    public class PageLoginWindows
    {
        private readonly IPage Page;

        private static readonly Regex StaySignedOutLabels =
            new("^(No|Non|Nein)$", RegexOptions.IgnoreCase);

        public PageLoginWindows(IPage page)
        {
            Page = page;
        }

        public ILocator EmailInput =>
            Page.Locator("#i0116, input[name='loginfmt'], input[autocomplete='username'], input[type='email']").First;
        public ILocator NextButton => Page.Locator("#idSIButton9");
        public ILocator PasswordInput => Page.Locator("input[type='password']:visible").First;
        public ILocator SignInButton => Page.Locator("#idSIButton9");
        public ILocator StaySignedOutButton => Page.GetByRole(AriaRole.Button, new() { NameRegex = StaySignedOutLabels });

        public async Task EnterEmailAsync(string username, int timeoutMs = 10_000)
        {
            await EmailInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
            try
            {
                await EmailInput.FillAsync(username);
            }
            catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
            {
                throw new InvalidOperationException(
                    $"Impossible de saisir l'adresse sur le formulaire SSO ({ex.GetType().Name}).");
            }
        }

        public async Task ClickNextAsync(int timeoutMs = 10_000)
        {
            await NextButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
            await NextButton.ClickAsync();
        }

        public async Task EnterPasswordAsync(string password, int timeoutMs = 10_000)
        {
            await PasswordInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
            try
            {
                await PasswordInput.FillAsync(password);
            }
            catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
            {
                if (!IsMicrosoftLoginPage())
                    return;

                throw new InvalidOperationException(
                    $"Impossible de saisir le mot de passe sur le formulaire SSO ({ex.GetType().Name}).");
            }
        }

        public async Task ClickSignInAsync(int timeoutMs = 10_000)
        {
            await SignInButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
            await SignInButton.ClickAsync();
        }

        public async Task SignInAsync(string username, string password, int timeoutMs = 45_000)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("L'adresse de connexion IVY n'est pas configurée.", nameof(username));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Le mot de passe IVY n'est pas configuré.", nameof(password));

            if (IsMicrosoftLoginPage() && !await PasswordInput.IsVisibleAsync())
            {
                try
                {
                    await EnterEmailAsync(username);
                }
                catch (TimeoutException)
                {
                    if (!await StaySignedOutButton.IsVisibleAsync())
                    {
                        var error = await ReadAuthenticationErrorAsync();
                        var detail = string.IsNullOrWhiteSpace(error) ? string.Empty : $" Détail Microsoft : {error}";
                        throw new InvalidOperationException(
                            $"Le formulaire Microsoft ne présente ni l'adresse ni le mot de passe IVY.{detail}");
                    }
                }

                if (await EmailInput.IsVisibleAsync())
                {
                    await ClickNextAsync();

                    try
                    {
                        await PasswordInput.WaitForAsync(new()
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = timeoutMs
                        });
                    }
                    catch (TimeoutException)
                    {
                        var error = await ReadAuthenticationErrorAsync();
                        var detail = string.IsNullOrWhiteSpace(error) ? string.Empty : $" Détail Microsoft : {error}";
                        throw new InvalidOperationException(
                            $"L'écran de saisie du mot de passe IVY n'est pas apparu après l'envoi de l'adresse.{detail}");
                    }
                }
            }

            if (IsMicrosoftLoginPage() && await PasswordInput.IsVisibleAsync())
            {
                await EnterPasswordAsync(password);
                if (IsMicrosoftLoginPage())
                    await ClickSignInAsync();
            }

            await WaitForIvyRedirectAsync(timeoutMs);
            await EnsureIvyCallbackSucceededAsync();
        }

        private async Task WaitForIvyRedirectAsync(int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                if (!IsMicrosoftLoginPage())
                    return;

                if (await StaySignedOutButton.IsVisibleAsync())
                {
                    await StaySignedOutButton.ClickAsync();
                    continue;
                }

                var error = await ReadAuthenticationErrorAsync();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new InvalidOperationException($"Microsoft SSO a refusé la connexion : {error}");
                }

                await Page.WaitForTimeoutAsync(250);
            }

            throw new InvalidOperationException(
                "Microsoft SSO n'a pas redirigé vers IVY dans le délai prévu. " +
                "Vérifier si une étape supplémentaire, comme une validation MFA, est requise.");
        }

        private async Task EnsureIvyCallbackSucceededAsync()
        {
            if (!Uri.TryCreate(Page.Url, UriKind.Absolute, out var currentUri) ||
                !currentUri.Host.Equals("ivy-qua.cce24x7.net", StringComparison.OrdinalIgnoreCase) ||
                !currentUri.AbsolutePath.Contains("redirect_uri", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var bodyText = await Page.Locator("body").InnerTextAsync();
            if (bodyText.Contains("Bad Request", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Microsoft SSO a accepté la connexion, mais le retour vers IVY répond HTTP 400. " +
                    "L'application IVY n'a pas encore chargé; la fenêtre d'activité ne peut donc pas être vérifiée.");
            }
        }

        private async Task<string> ReadAuthenticationErrorAsync()
        {
            var alert = Page.Locator("[role='alert']:visible").First;
            if (await alert.CountAsync() == 0)
                return string.Empty;

            return (await alert.InnerTextAsync()).Trim();
        }

        private bool IsMicrosoftLoginPage() =>
            Uri.TryCreate(Page.Url, UriKind.Absolute, out var currentUri) &&
            currentUri.Host.Equals("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase);
    }
}
