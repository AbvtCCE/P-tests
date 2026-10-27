using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    /// <summary>
    /// Classe de base pour les tests du Portail (my-q.groupe-e.ch).
    /// Hérite de TestBase et ajoute :
    /// - Login Portail
    /// - Sélection de langue (FR/DE)
    /// - Gestion des popups d'information
    ///
    /// Combinaisons testées : navigateur × mobile × langue
    /// </summary>
    [TestFixture]
    [Category("Portail")]
    public abstract class TestBasePortail : TestBase
    {
        #region Login Portail

        /// <summary>
        /// Connecte le partenaire sélectionné sur le portail.
        /// Utilise les critères définis dans criteresSelection pour trouver le bon partenaire.
        /// </summary>
        protected async Task LoginAvecCriteres()
        {
            partenaireSelectionne = await TestTools.LogWithCriteria(
                page!, tousLesPartenaires, criteresSelection!, Report!, testName!);

            Console.WriteLine($"Partenaire selectionne: {partenaireSelectionne.NumeroPartenaire} ({partenaireSelectionne.Email})");
        }

        /// <summary>
        /// Connecte un partenaire spécifique sur le portail (sans critères)
        /// </summary>
        protected async Task LoginPartenaire(Partenaire partenaire)
        {
            await TestTools.LogWithoutCriteria(page!, partenaire, Report!, testName!);
        }

        protected async Task LogKeyClock(IPage page)
        {
            await TestTools.LoginToKeycloak(page);
        }

        protected async Task RemoveErrorKeyClock(IPage page)
        {
            await page.GetByRole(AriaRole.Heading, new() { Name = "Danger alert: Could not find" }).WaitForAsync();
            var msgDanger=await page.GetByRole(AriaRole.Heading, new() { Name = "Danger alert: Could not find" }).CountAsync();
            if(msgDanger>0)
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "Press here to refresh and" }).ClickAsync();     
                await page.GetByRole(AriaRole.Tabpanel, new() { Name = "Server info" }).WaitForAsync(new() { Timeout = 10_000 });
                Assert.That(await page.GetByRole(AriaRole.Tabpanel, new() { Name = "Server info" }).IsVisibleAsync(), Is.True);

            }
            await page.GetByTestId("realmSelectorToggle").ClickAsync();
           await page.GetByRole(AriaRole.Menuitem, new() { Name = "my-groupe-e" }).ClickAsync();
        }

        

        #endregion

        #region Langue

        /// <summary>
        /// Sélectionne la langue sur le portail (FR ou DE).
        /// Gère automatiquement le menu mobile si isMobile=true.
        /// </summary>
        protected async Task SelectionnerLangue()
        {
            await TestTools.SelectLanguage(page!, isMobile, langue!);
        }

        #endregion

        #region Popups

        /// <summary>
        /// Ferme le popup d'information "Fermer" s'il apparaît
        /// </summary>
        protected async Task FermerPopupInformation()
        {
            await TestTools.PopUpPortal(page!);
        }

        #endregion

        #region Génération des combinaisons de tests

        /// <summary>
        /// Génère les combinaisons browser × mobile × langue pour les TestCaseSource.
        /// Respecte les env vars : RUN_BROWSER_*, RUN_MOBILE, LNG_*
        /// Firefox + Mobile est exclu (non supporté par Playwright).
        /// </summary>
        public static IEnumerable<TestCaseData> GenererCombinaisonsPortail()
        {
            // --- Navigateurs ---
            bool runChrome = GetEnvBool("RUN_BROWSER_CHROME", true);
            bool runFirefox = GetEnvBool("RUN_BROWSER_FIREFOX", false);
            bool runSafari = GetEnvBool("RUN_BROWSER_SAFARI", false);

            var navigateurs = new List<string>();
            if (runChrome) navigateurs.Add("chromium");
            if (runFirefox) navigateurs.Add("firefox");
            if (runSafari) navigateurs.Add("webkit");
            if (navigateurs.Count == 0) navigateurs.Add("chromium");

            // --- Mobile ---
            bool runMobile = GetEnvBool("RUN_MOBILE", false);
            var optionsMobile = new List<bool> { false };
            if (runMobile) optionsMobile.Add(true);

            // --- Langues ---
            bool lngFr = GetEnvBool("LNG_FR", true);
            bool lngDe = GetEnvBool("LNG_DE", true);

            var langues = new List<string>();
            if (lngFr) langues.Add("FR");
            if (lngDe) langues.Add("DE");
            if (langues.Count == 0) langues.Add("FR");

            // --- Génération des combinaisons ---
            foreach (var nav in navigateurs)
            {
                foreach (var mobile in optionsMobile)
                {
                    // Firefox ne supporte pas isMobile dans Playwright
                    if (nav == "firefox" && mobile) continue;

                    foreach (var lg in langues)
                    {
                        string labelNav = nav switch
                        {
                            "chromium" => "Chrome",
                            "firefox" => "Firefox",
                            "webkit" => "Safari",
                            _ => nav
                        };
                        string labelDevice = mobile ? "Mobile" : "Desktop";
                        string nomTestCase = $"{labelNav}_{labelDevice}_{lg}";

                        yield return new TestCaseData(nav, mobile, lg)
                            .SetArgDisplayNames(nomTestCase);
                    }
                }
            }
        }

        /// <summary>
        /// Lit une variable d'environnement booléenne avec valeur par défaut
        /// </summary>
        protected static bool GetEnvBool(string nomVariable, bool valeurParDefaut)
        {
            var valeur = Environment.GetEnvironmentVariable(nomVariable);
            if (string.IsNullOrEmpty(valeur)) return valeurParDefaut;
            return valeur.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
