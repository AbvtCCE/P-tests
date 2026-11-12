using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.PortailV2Copilot.Pages;

namespace TestPlayrights.PortailV2Copilot.Smoke
{
    /// <summary>
    /// Smoke test de connexion au Portail avec un partenaire inscrit.
    /// </summary>
    [TestFixture]
    [Category("CI_SMOKE")]
    public sealed class ConnexionSmokeTest
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        [SetUp]
        public async Task SetUpAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = TestConfig.IsHeadless
            });
            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            _page = await _context.NewPageAsync();
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await TestEvidence.CaptureAsync(_page);
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

        [TestCaseSource(nameof(GenererLangues))]
        public async Task ConnexionAvecPartenaireInscrit_AfficheLaPageAccueil(string langue)
        {
            var partenaire = DataLoader.FindClientWithCriteria(
                DataLoader.GetPartenaires(Serializer.PathToPartenaireCsv),
                new Dictionary<string, string?> { ["InscritSurLePortail"] = "true" });

            Assert.That(partenaire, Is.Not.Null,
                "Aucun partenaire inscrit au Portail n'a été trouvé dans le fichier de données.");
            Assert.That(partenaire!.Email, Is.Not.Null.And.Not.Empty,
                "Le partenaire sélectionné n'a pas d'adresse email.");

            var connexionPage = new ConnexionPage(_page!, langue);
            await connexionPage.NaviguerAsync();
            await connexionPage.SeConnecterAsync(partenaire.Email, Serializer.mdp);

            var accueilPage = new AccueilPage(_page!);
            if (!await accueilPage.DetecterPopupFactureEmailAsync())
            {
                await accueilPage.FermerPopupInformationSiPresentAsync();
            }
            await accueilPage.VerifierConnexionReussieAsync();
        }

        private static IEnumerable<TestCaseData> GenererLangues()
        {
            bool langueFr = LireBooleenEnvironnement("LNG_FR", true);
            bool langueDe = LireBooleenEnvironnement("LNG_DE", true);

            if (langueFr)
            {
                yield return new TestCaseData("FR").SetName("ConnexionPortail_FR");
            }

            if (langueDe)
            {
                yield return new TestCaseData("DE").SetName("ConnexionPortail_DE");
            }

            if (!langueFr && !langueDe)
            {
                yield return new TestCaseData("FR").SetName("ConnexionPortail_FR");
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
