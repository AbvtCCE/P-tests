using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TestPlayrights
{
    public class PageLoginWindows 
  {
    
    private readonly IPage Page;

    public PageLoginWindows(IPage page)
    {
        Page = page;
    }

    // 🔹 Déclarations des inputs / contrôles (locators)
    public ILocator EmailInput =>
        Page.GetByRole(AriaRole.Textbox, new() { Name = "someone@example.com" });

    public ILocator NextButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Suivant" });

    public ILocator PasswordInput =>
        Page.GetByRole(AriaRole.Textbox, new() { Name = "Entrer le mot de passe pour" });

    public ILocator SignInButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" });

    public ILocator YesButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Oui" });

    // 🔹 Méthodes d'action (simples et réutilisables)

    /// <summary>Attend que le champ email soit visible et le remplit.</summary>
    public async Task EnterEmailAsync(string username, int timeoutMs = 10_000)
    {
        await EmailInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
        await EmailInput.FillAsync(username);
    }

    /// <summary>Clique sur le bouton "Suivant".</summary>
    public async Task ClickNextAsync(int timeoutMs = 10_000)
    {
        await NextButton.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });
        await NextButton.ClickAsync();
    }

    /// <summary>Attend que le champ mot de passe soit visible et le remplit.</summary>
    public async Task EnterPasswordAsync(string password, int timeoutMs = 10_000)
    {
        await PasswordInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
        await PasswordInput.FillAsync(password);
    }

    /// <summary>Clique sur "Se connecter".</summary>
    public async Task ClickSignInAsync(int timeoutMs = 10_000)
    {
        await SignInButton.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });
        await SignInButton.ClickAsync();
    }

    /// <summary>Clique sur "Oui" (ex. rester connecté / consentement).</summary>
    public async Task ClickYesAsync(int timeoutMs = 10_000)
    {
        await YesButton.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });
        await YesButton.ClickAsync();
    }

    // 🔹 Méthode composée : enchaîne tout le flux d’authent Windows/Entra/SSO (selon ta page)
    public async Task SignInAsync(string username, string password)
    {
        await EnterEmailAsync(username);
        await ClickNextAsync();
        // Ils ont fait sauté la partie mot de passes
        //await EnterPasswordAsync(password);
        //await ClickSignInAsync();
        //await ClickYesAsync();
    }


  }
}