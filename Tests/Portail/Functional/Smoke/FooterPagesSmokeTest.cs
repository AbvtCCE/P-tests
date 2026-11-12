using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights.PortailV2Copilot.Smoke
{
    [TestFixture]
    [Category("PORTAIL_PUBLIC_EXTERNAL")]
    public sealed class FooterPagesSmokeTest
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _pagePourPreuve;

        [SetUp]
        public async Task SetUpAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = TestConfig.IsHeadless
            });
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await TestEvidence.CaptureAsync(_pagePourPreuve);
            try
            {
                if (_context != null)
                {
                    await _context.CloseAsync();
                }
            }
            finally
            {
                try
                {
                    if (_browser != null)
                    {
                        await _browser.CloseAsync();
                    }
                }
                finally
                {
                    _playwright?.Dispose();
                }
            }
        }

        [TestCaseSource(nameof(GenererScenarios))]
        public async Task PageFooter_ContenuEtAccessibiliteVerifies(
            string langue,
            string libelleLien,
            string cheminAttendu,
            string extraitTitreAttendu,
            string extraitContenuAttendu)
        {
            _context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                Locale = langue == "DE" ? "de-CH" : "fr-CH"
            });
            var pageEntree = await _context.NewPageAsync();
            _pagePourPreuve = pageEntree;
            await pageEntree.GotoAsync(Serializer.GetUrlPortailQual());
            await TestTools.SelectLanguageWithoutBeingConnected(pageEntree, langue);

            var lienFooter = pageEntree.GetByRole(AriaRole.Link, new()
            {
                Name = libelleLien,
                Exact = true
            });
            await lienFooter.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var href = await lienFooter.GetAttributeAsync("href");
            Assert.That(href, Is.Not.Null.And.Not.Empty, $"Le lien « {libelleLien} » n'a pas d'URL.");
            Assert.That(new Uri(new Uri(pageEntree.Url), href!).AbsolutePath, Is.EqualTo(cheminAttendu));

            var cible = await lienFooter.GetAttributeAsync("target");
            var pageContenu = string.Equals(cible, "_blank", StringComparison.OrdinalIgnoreCase)
                ? await pageEntree.RunAndWaitForPopupAsync(() => lienFooter.ClickAsync())
                : pageEntree;
            _pagePourPreuve = pageContenu;

            await pageContenu.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.That(new Uri(pageContenu.Url).Host, Is.EqualTo("www.groupe-e.ch"));
            Assert.That(new Uri(pageContenu.Url).AbsolutePath, Is.EqualTo(cheminAttendu));
            await AttendrePageOuDefiAntiRobotAsync(pageContenu);

            await FermerBanniereCookiesSiPresenteAsync(pageContenu, langue);

            var titre = await pageContenu.TitleAsync();
            Assert.That(titre, Is.Not.Empty, "Le titre de la page est vide.");

            var titrePrincipal = pageContenu.Locator("main h1");
            await titrePrincipal.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var langueHtml = await pageContenu.Locator("html").GetAttributeAsync("lang");
            Assert.That(langueHtml, Does.StartWith(langue.ToLowerInvariant()),
                "La langue déclarée par la page ne correspond pas à la langue demandée.");

            Assert.That(await titrePrincipal.InnerTextAsync(), Does.Contain(extraitTitreAttendu));

            var contenuPrincipal = await pageContenu.Locator("main").InnerTextAsync();
            Assert.That(contenuPrincipal, Does.Contain(extraitContenuAttendu),
                "Le contenu attendu pour cette page n'a pas été trouvé.");

        }

        private static async Task FermerBanniereCookiesSiPresenteAsync(IPage page, string langue)
        {
            var dialogueCookies = page.GetByRole(AriaRole.Dialog);
            if (await dialogueCookies.CountAsync() == 0)
            {
                return;
            }

            var libelleFermer = langue == "FR" ? "Fermer" : "Schließen";
            await dialogueCookies.GetByRole(AriaRole.Button, new()
            {
                Name = libelleFermer,
                Exact = true
            }).ClickAsync();
            await dialogueCookies.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        private static async Task AttendrePageOuDefiAntiRobotAsync(IPage page)
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                    const heading = document.querySelector("main h1");
                    const bodyText = document.body?.innerText ?? "";
                    return (heading && heading.getClientRects().length > 0)
                        || /Vérification de sécurité en cours|Verify you are human|Sicherheitsüberprüfung wird durchgeführt|Bestätigen Sie, dass Sie ein Mensch sind/i.test(bodyText);
                }
                """);

            var texteVisible = await page.Locator("body").InnerTextAsync();
            if (texteVisible.Contains("Vérification de sécurité en cours", StringComparison.OrdinalIgnoreCase)
                || texteVisible.Contains("Verify you are human", StringComparison.OrdinalIgnoreCase)
                || texteVisible.Contains("Sicherheitsüberprüfung wird durchgeführt", StringComparison.OrdinalIgnoreCase)
                || texteVisible.Contains("Bestätigen Sie, dass Sie ein Mensch sind", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail("Le site public présente un défi anti-robot Cloudflare. Le test est bloqué; aucune interaction automatique n'a été tentée.");
            }
        }

        private static IEnumerable<TestCaseData> GenererScenarios()
        {
            var langues = new List<string>();
            if (LireBooleenEnvironnement("LNG_FR", true))
            {
                langues.Add("FR");
            }

            if (LireBooleenEnvironnement("LNG_DE", true))
            {
                langues.Add("DE");
            }

            if (langues.Count == 0)
            {
                langues.Add("FR");
            }

            foreach (var langue in langues)
            {
                if (langue == "FR")
                {
                    yield return new TestCaseData("FR", "Contact", "/fr/contact",
                        "Contactez-nous !", "Quel client êtes-vous ?")
                        .SetName("Footer_Contact_FR");
                    yield return new TestCaseData("FR", "Conditions générales internet",
                        "/fr/conditions-generales-internet", "Conditions générales",
                        "régissent la navigation sur le Site")
                        .SetName("Footer_ConditionsInternet_FR");
                    yield return new TestCaseData("FR", "Conditions générales de Groupe E SA",
                        "/fr/conditions-generales", "Conditions générales",
                        "Les conditions générales d'achat de Groupe E s'appliquent")
                        .SetName("Footer_ConditionsGroupe_FR");
                }
                else
                {
                    yield return new TestCaseData("DE", "Kontakt", "/de/kontakt",
                        "Kontaktieren Sie uns!", "Sie sind")
                        .SetName("Footer_Kontakt_DE");
                    yield return new TestCaseData("DE", "Allgemeine Nutzungsbedingungen",
                        "/de/allgemeine-nutzungsbedingungen", "Allgemeine Nutzungsbedingungen",
                        "Navigation auf der Website regeln")
                        .SetName("Footer_Nutzungsbedingungen_DE");
                    yield return new TestCaseData("DE", "Allgemeine Bedingungen der Groupe E AG",
                        "/de/allgemeine-geschaeftsbedingungen", "Allgemeine Geschäftsbedingungen",
                        "Die Allgemeinen Einkaufsbedingungen von Groupe E gelten")
                        .SetName("Footer_BedingungenGruppe_DE");
                }
            }
        }

        private static bool LireBooleenEnvironnement(string nomVariable, bool valeurParDefaut)
        {
            var valeur = Environment.GetEnvironmentVariable(nomVariable);
            return string.IsNullOrEmpty(valeur)
                ? valeurParDefaut
                : valeur.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
