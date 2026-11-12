using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace TestPlayrights
{
    /// <summary>
    /// Classe de base pour TOUS les tests Playwright.
    /// Gère le cycle de vie : Browser, Context, Page, Report.
    ///
    /// En local  : navigateur visible, DEBUG_PAUSE=true pour page.PauseAsync()
    /// En CI     : headless automatique, pas de pause
    /// </summary>
    [TestFixture]
    public abstract class TestBase
    {
        #region Playwright Objects

        protected IPlaywright? playwright;
        protected IBrowser? browser;
        protected IBrowserContext? context;
        protected IPage? page;

        #endregion

        #region Configuration du test

        // --- Navigateur ---
        protected string browserType = "chromium";
        protected string? browserName;
        protected bool isMobile = false;
        protected string? langue;

        // --- Métadonnées du test ---
        protected string? testName;
        protected string? descriptionTest;
        protected string? expectedResult;

        #endregion

        #region Données

        /// <summary>
        /// Tous les partenaires chargés depuis le CSV
        /// </summary>
        private List<Partenaire>? _tousLesPartenaires;

        protected List<Partenaire> tousLesPartenaires
        {
            get => _tousLesPartenaires ??= DataLoader.GetPartenaires(Serializer.PathToPartenaireCsv);
            set => _tousLesPartenaires = value;
        }

        /// <summary>
        /// Le partenaire sélectionné pour le test en cours
        /// </summary>
        protected Partenaire partenaireSelectionne = new Partenaire();

        /// <summary>
        /// Critères de sélection du partenaire (à overrider dans les classes enfant)
        /// </summary>
        protected virtual Dictionary<string, string?> criteresSelection { get; set; } = new();

        #endregion

        #region Rapport

        /// <summary>
        /// Rapport de test (peut être individuel ou partagé selon IsSharedReport)
        /// </summary>
        protected static ReportBuilder? Report;

        /// <summary>
        /// Compteurs pour les rapports partagés (thread-safe)
        /// </summary>
        protected static int _testCount = 0;
        protected static int _passedCount = 0;
        protected static int _failedCount = 0;
        protected static readonly object _reportLock = new();

        /// <summary>
        /// Override dans les classes qui utilisent un rapport partagé (OneTimeSetUp/TearDown)
        /// </summary>
        protected virtual bool IsSharedReport() => false;

        #endregion

        #region Setup

        /// <summary>
        /// Configure les paramètres à partir des arguments TestCase
        /// </summary>
        protected void ConfigurerTest(string nomNavigateur, bool mobile, string lg)
        {
            browserType = nomNavigateur;
            browserName = nomNavigateur switch
            {
                "chromium" => "Chrome",
                "firefox" => "Firefox",
                "webkit" => "Safari",
                _ => nomNavigateur
            };
            isMobile = mobile;
            langue = lg.ToUpper();
        }

        /// <summary>
        /// Initialise Playwright, le navigateur, le contexte et la page.
        /// Crée un rapport individuel si aucun rapport partagé n'existe.
        /// </summary>
        public virtual async Task Setup()
        {
            // --- Playwright + Browser ---
            playwright = await Playwright.CreateAsync();
            browser = await TestTools.InitializeBrowser(browserType, TestConfig.IsHeadless);
            context = await TestTools.CreateContext(browser, isMobile);
            page = await context.NewPageAsync();

            // Bandeau officiel Playwright de suivi des actions (local/headed uniquement)
            await TestTools.ActiverSuiviEtapes(page);

            // --- Rapport individuel (si pas de rapport partagé) ---
            if (Report == null)
            {
                CreerRapportIndividuel();
            }
        }

        /// <summary>
        /// Setup avec paramètres (pour TestCaseSource)
        /// </summary>
        protected async Task Setup(string browserType, bool isMobile, string langue)
        {
            ConfigurerTest(browserType, isMobile, langue);
            await Setup();
        }

        /// <summary>
        /// Crée un rapport individuel pour ce test
        /// </summary>
        private void CreerRapportIndividuel()
        {
            var nomRapport = $"{DateTime.Now:yyMMddHHmmss}_{testName}_{langue}_{browserName}{(isMobile ? "_Mobile" : "")}";
            var cheminRapport = Path.Combine(
                Environment.GetEnvironmentVariable("REPORTS_PATH") ?? Serializer.pathToReportFolder,
                testName ?? "Test",
                $"{nomRapport}.html"
            );

            Report = new ReportBuilder(cheminRapport, testName!);
            Report.AddTestCase(
                title: testName!,
                data: $"<br>Langue: {langue}<br>Navigateur: {browserType}{(isMobile ? " (Mobile)" : "")}",
                description: descriptionTest ?? "",
                expected: expectedResult ?? ""
            );
        }

        #endregion

        #region TearDown

        [TearDown]
        public virtual async Task TearDown()
        {
            var resultatTest = TestContext.CurrentContext.Result.Outcome.Status;
            await TestEvidence.CaptureAsync(page);

            // --- Capture d'écran en cas d'échec ---
            if (resultatTest == TestStatus.Failed && Report != null && page != null)
            {
                Console.WriteLine("[TEST] ECHEC");
                await Report.AddTestStep("Echec du test", page, testName ?? "Test");
                Report.SetTestResult(false, "Le test a echoue");
            }

            // --- Fermeture des ressources Playwright ---
            if (context != null) await context.CloseAsync();
            if (browser != null) await browser.CloseAsync();
            playwright?.Dispose();

            // --- Génération du rapport individuel en PDF ---
            if (Report != null && !IsSharedReport())
            {
                IPlaywright? pdfPlaywright = null;
                IBrowser? pdfBrowser = null;
                try
                {
                    pdfPlaywright = await Playwright.CreateAsync();
                    pdfBrowser = await pdfPlaywright.Chromium.LaunchAsync(
                        new BrowserTypeLaunchOptions { Headless = true });
                    await Report.BuildReportAsPdf(pdfBrowser, keepHtml: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestBase] Erreur generation PDF, fallback HTML : {ex.Message}");
                    Report.BuildReport();
                }
                finally
                {
                    if (pdfBrowser != null) await pdfBrowser.CloseAsync();
                    pdfPlaywright?.Dispose();
                }

                Report = null;
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// Pause le navigateur pour inspection (uniquement en local avec DEBUG_PAUSE=true).
        /// En CI, cette méthode ne fait rien.
        /// </summary>
        protected async Task DebugPause()
        {
            if (TestConfig.IsDebugPause && page != null)
            {
                await page.PauseAsync();
            }
        }

        #endregion

        #region GitHub Actions Logging

        /// <summary>
        /// Log structuré pour GitHub Actions (group/notice)
        /// </summary>
        protected void LogGitHubActions(string label, Partenaire? partenaire = null)
        {
            Console.WriteLine($"::group::Test {label}");
            Console.WriteLine($"::notice::Debut du test {label}");
            Console.WriteLine($"Navigateur: {browserType}");
            Console.WriteLine($"Mobile: {(isMobile ? "Oui" : "Non")}");
            Console.WriteLine($"Langue: {langue}");

            if (partenaire != null)
            {
                Console.WriteLine($"Partenaire: {partenaire.NumeroPartenaire}");
                Console.WriteLine($"Email: {partenaire.Email}");
                Console.WriteLine($"Compte contrat: {partenaire.CompteContrat}");
            }
        }

        protected void LogGitHubActionsSuccess(string label)
        {
            Console.WriteLine($"::notice::Test {label} reussi");
            Console.WriteLine("::endgroup::");
        }

        protected void LogGitHubActionsError(string label, string errorMessage)
        {
            Console.WriteLine($"::error::Test {label} echoue: {errorMessage}");
            Console.WriteLine("::endgroup::");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Label descriptif du test : "Chrome Desktop - FR"
        /// </summary>
        protected string GetTestLabel()
        {
            return $"{browserName} {(isMobile ? "Mobile" : "Desktop")} - {langue}";
        }

        #endregion
    }
}
