using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TestPlayrights
{
    public class PageOuvertureIvy 
  {
    
    private readonly IPage Page;

    public PageOuvertureIvy(IPage page)
    {
        Page = page;
    }
    
// Locators (déclarations)
    // =========================

    // Bouton des paramètres (icône engrenage + libellé)
    private ILocator SettingsButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = " Settings" });

    // Combobox Langue (selon langue courante)
    private ILocator FRLanguageCombo =>
        Page.GetByRole(AriaRole.Combobox, new() { Name = "Langue" });
    private ILocator ENLanguageCombo =>
        Page.GetByRole(AriaRole.Combobox, new() { Name = "Language" });
    private ILocator DELanguageCombo =>
        Page.GetByRole(AriaRole.Combobox, new() { Name = "Sprache" });

    // Fallback FR (si pas de combobox ARIA)
    private ILocator FRFallbackLanguageLabel =>
        Page.Locator("[id=\"form-available:dashboard-parent:header-dashboard:form-language:language-box_label\"]");

    // Boutons Save/Enregistrer/Speichern
    private ILocator SaveFRButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = " Enregistrer" });
    private ILocator SaveENButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Save" });
    private ILocator SaveDEButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Speichern" });

    // Sélecteur de tenant + option "groupe-e"
    private ILocator TenantMenuLabel =>
        Page.Locator("[id=\"form-available\\:tenantMenuId_label\"]");
    private ILocator GroupeEOption =>
        Page.GetByRole(AriaRole.Option, new() { Name = "groupe-e" });

    // Bouton démarrer + choix "standard" (clé i18n)
    private ILocator StartButton(string langue) =>
        Page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("startInter", langue) });
    private ILocator StandardChoice(string langue) =>
        Page.Locator("text=" + ResourceHelper.GetStringIvy("standard", langue));

    // =========================
    // Méthodes d’action
    // =========================

    /// <summary>
    /// Détecte la langue courante via la présence de textes d’en-tête des paramètres.
    /// Retourne "FR", "EN", "DE" ou "UNKNOWN".
    /// </summary>
    public async Task<string> DetectLanguageAsync()
    {
        var deCount = await Page.GetByText("Benutzereinstellungen").CountAsync();
        if (deCount > 0) return "DE";

        var frCount = await Page.GetByText("Paramètres utilisateur").CountAsync();
        if (frCount > 0) return "FR";

        var enCount = await Page.GetByText("User Settings").CountAsync();
        if (enCount > 0) return "EN";

        return "UNKNOWN";
    }

    /// <summary>
    /// Ouvre les paramètres et positionne la langue sur FR/EN/DE,
    /// en respectant les variations d’UI par langue.
    /// </summary>
    public async Task ChooseLanguageAsync(string lg)
    {
        // Ouvrir la modale/section des paramètres
        await SettingsButton.ClickAsync();

        var current = await DetectLanguageAsync();

        switch (current)
        {
            case "FR":
                // Certaines versions FR présentent un combobox ARIA, d’autres un label cliquable
                if (await FRLanguageCombo.CountAsync() > 0)
                    await FRLanguageCombo.ClickAsync();
                else
                    await FRFallbackLanguageLabel.ClickAsync();

                switch (lg)
                {
                    case "FR":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Français" }).ClickAsync();
                        break;
                    case "EN":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Anglais" }).ClickAsync();
                        break;
                    case "DE":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Allemand" }).ClickAsync();
                        break;
                }
                await SaveFRButton.ClickAsync();
                break;

            case "EN":
                await ENLanguageCombo.ClickAsync();
                switch (lg)
                {
                    case "FR":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "French" }).ClickAsync();
                        break;
                    case "EN":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "English" }).ClickAsync();
                        break;
                    case "DE":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "German" }).ClickAsync();
                        break;
                }
                await SaveENButton.ClickAsync();
                break;

            case "DE":
                await DELanguageCombo.ClickAsync();
                switch (lg)
                {
                    case "FR":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Französisch" }).ClickAsync();
                        break;
                    case "EN":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Englisch" }).ClickAsync();
                        break;
                    case "DE":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Deutsch" }).ClickAsync();
                        break;
                }
                await SaveDEButton.ClickAsync();
                break;

            default:
                // Langue inconnue : tentative FR par défaut (combo ou fallback)
                if (await FRLanguageCombo.CountAsync() > 0)
                    await FRLanguageCombo.ClickAsync();
                else
                    await FRFallbackLanguageLabel.ClickAsync();

                // Essaie de basculer vers la langue cible côté FR (libellés FR)
                switch (lg)
                {
                    case "FR":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Français" }).ClickAsync();
                        break;
                    case "EN":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Anglais" }).ClickAsync();
                        break;
                    case "DE":
                        await Page.GetByRole(AriaRole.Option, new() { Name = "Allemand" }).ClickAsync();
                        break;
                }
                await SaveFRButton.ClickAsync();
                break;
        }
    }

    /// <summary>
    /// Étapes finales : choisir le tenant "groupe-e", cliquer "Démarrer l’intervention",
    /// puis choisir le scénario "standard" selon la langue.
    /// </summary>
    public async Task ConfigureFinalAsync(string langue)
    {
        await TenantMenuLabel.ClickAsync();
        await GroupeEOption.ClickAsync();

        await StartButton(langue).ClickAsync();
        await StandardChoice(langue).ClickAsync();
    }

   


  }
}