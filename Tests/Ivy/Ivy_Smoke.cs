using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    /// <summary>
    /// Tests Smoke IVY (ivy-qua.cce24x7.net).
    /// Verifie les fonctionnalites de base : connexion SSO, recherche partenaire, navigation dashboard.
    /// Rapport partage entre toutes les combinaisons de langues.
    /// </summary>
    [TestFixture]
    [Category("CI_SMOKE_IVY")]
    public class Ivy_Smoke : TestBaseIvy
    {
        #region Configuration

        protected override Dictionary<string, string?> criteresSelection { get; set; } = new();

        /// <summary>Rapport partage entre toutes les langues</summary>
        protected override bool IsSharedReport() => true;

        #endregion

        #region Setup / Teardown

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var reportsBase = Environment.GetEnvironmentVariable("REPORTS_PATH") ?? Serializer.pathToReportFolder;
            var workflowName = Environment.GetEnvironmentVariable("WORKFLOW_NAME") ?? "";
            var dossier = Path.Combine(reportsBase, "Smoke_Ivy");

            Report = new ReportBuilder(dossier, "Smoke_Ivy", workflowName);
            _testCount = 0;
            _passedCount = 0;
            _failedCount = 0;
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown()
        {
            if (Report == null) return;

            string resultat = $"Tests executes: {_testCount} | Reussis: {_passedCount} | Echoues: {_failedCount}";
            Report.SetTestResult(_failedCount == 0, resultat);

            var pw = await Playwright.CreateAsync();
            var tempBrowser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            try
            {
                await Report.BuildReportAsPdf(tempBrowser, keepHtml: false);
            }
            finally
            {
                await tempBrowser.CloseAsync();
                pw.Dispose();
            }
        }

        #endregion

        #region Test Smoke

        /// <summary>
        /// Test Smoke IVY : connexion SSO, recherche partenaire, verification du dashboard.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task SmokeIvy(string navigateur, bool mobile, string lg)
        {
            Interlocked.Increment(ref _testCount);

            // --- Configuration ---
            testName = "SmokeIvy";
            await Setup(navigateur, mobile, lg);
            string label = $"IVY {lg}";
            LogGitHubActions(label);

            lock (_reportLock)
            {
                Report!.AddTextBlock($"Test: {label}", $"Langue: {lg}", "info");
            }

            try
            {
                // --- Login SSO ---
                var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
                partenaireSelectionne = p;
                Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | Email: {p.Email}");

                await LoginEtAllerAuDashboard(p);

                lock (_reportLock)
                {
                    Report!.AddTestStep($"{label} - Dashboard", page!).Wait();
                    Report.AddTextBlock($"{label} - Valide", "Connexion et dashboard OK", "success");
                }

                LogGitHubActionsSuccess(label);
                Interlocked.Increment(ref _passedCount);
            }
            catch (Exception ex)
            {
                lock (_reportLock)
                {
                    Report!.AddTextBlock($"{label} - Erreur", ex.Message, "error");
                }
                LogGitHubActionsError(label, ex.Message);
                Interlocked.Increment(ref _failedCount);
                throw;
            }
        }

        /// <summary>
        /// Test Smoke : depuis IVY, le bouton "Espace client" du header doit ouvrir
        /// un nouvel onglet vers le portail SAP (qu1.web.cce24x7.net).
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task OuverturePortailDepuisIvy(string navigateur, bool mobile, string lg)
        {
            Interlocked.Increment(ref _testCount);
                
            // --- Configuration ---
            testName = "OuverturePortailDepuisIvy";
            await Setup(navigateur, mobile, lg);
            string label = $"IVY Portail {lg}";
            LogGitHubActions(label);

            lock (_reportLock)
            {
                Report!.AddTextBlock($"Test: {label}", $"Langue: {lg}", "info");
            }

            try
            {
                // --- Login + recherche partenaire ---
                var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
                partenaireSelectionne = p;
                Console.WriteLine($"Partenaire: {p!.NumeroPartenaire} | Email: {p.Email}");

                await LoginEtAllerAuDashboard(p);

                lock (_reportLock)
                {
                    Report!.AddTestStep($"{label} - Dashboard", page!).Wait();
                }

                // --- Ouverture du portail via le composant Header ---
                var header = new ComponentsHeaderDashIvy(page!);
                var popupPortail = await header.OuvrirEspaceClientAsync(lg);

                await Assertions.Expect(popupPortail).ToHaveURLAsync(new Regex(@"qu1\.web\.cce24x7\.net"));

                lock (_reportLock)
                {
                    Report!.AddTestStep($"{label} - Onglet Portail", page!).Wait();
                    Report.AddTextBlock($"{label} - Valide", $"URL ouverte : {popupPortail.Url}", "success");
                }

                await popupPortail.CloseAsync();

                LogGitHubActionsSuccess(label);
                Interlocked.Increment(ref _passedCount);
            }
            catch (Exception ex)
            {
                lock (_reportLock)
                {
                    Report!.AddTextBlock($"{label} - Erreur", ex.Message, "error");
                }
                LogGitHubActionsError(label, ex.Message);
                Interlocked.Increment(ref _failedCount);
                throw;
            }
        }

        /// <summary>
        /// Test Smoke : rotation des langues FR -> DE -> EN -> FR sur l'interface
        /// principale IVY (apres login). Verifie qu'apres chaque changement, le
        /// label "Npart" du champ Numero de partenaire est traduit correctement.
        /// </summary>
        [Test]
        public async Task ChangementDeLangue()
        {
            
            Interlocked.Increment(ref _testCount);

            // --- Configuration ---
            testName = "ChangementDeLangue";
            await Setup("chromium", false, "FR");
            string label = "Changement de langue";
            LogGitHubActions(label);

            lock (_reportLock)
            {
                Report!.AddTextBlock($"Test: {label}", "Rotation FR -> DE -> EN -> FR", "info");
            }

            try
            {
                
                // --- Login initial en FR (la sequence Login se termine sur la page de recherche) ---
                await TestTools.LoginQual(page!, Serializer.EmailIvy, Serializer.MdpIvy, true, "FR");

                await page.Screencast.ShowActionsAsync(new() { Position = AnnotatePosition.TopLeft });
                var mainMenu = new PageMainMenuIvy(page!);

                await mainMenu.VerifierLangueAsync("FR");

                lock (_reportLock)
                {
                    Report!.AddTestStep($"{label} - FR initial", page!).Wait();
                }

                // --- Rotation a travers les 3 langues, fin en FR ---
                foreach (var lg in new[] { "DE", "EN", "FR" })
                {
                    await mainMenu.ChooseLanguageAsync(lg);
                    await mainMenu.VerifierLangueAsync(lg);

                    lock (_reportLock)
                    {
                        Report!.AddTestStep($"{label} - {lg}", page!).Wait();
                    }
                }

                lock (_reportLock)
                {
                    Report!.AddTextBlock($"{label} - Valide",
                        "Rotation FR / DE / EN / FR effectuee, UI traduite a chaque etape",
                        "success");
                }

                LogGitHubActionsSuccess(label);
                Interlocked.Increment(ref _passedCount);
            }
            catch (Exception ex)
            {
                lock (_reportLock)
                {
                    Report!.AddTextBlock($"{label} - Erreur", ex.Message, "error");
                }
                LogGitHubActionsError(label, ex.Message);
                Interlocked.Increment(ref _failedCount);
                throw;
            }
        }

/// <summary>
        /// Test Smoke : recherche du 1er partenaire du CSV et validation des donnees
        /// affichees dans le header (Nr. SAP, nom, email + boutons d'action).
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task TrouverPartenaire(string navigateur, bool mobile, string lg)
        {
            Interlocked.Increment(ref _testCount);

            // --- Configuration ---
            testName = "TrouverPartenaire";
            await Setup(navigateur, mobile, lg);
            string label = $"Trouver partenaire {lg}";
            LogGitHubActions(label);

            lock (_reportLock)
            {
                Report!.AddTextBlock($"Test: {label}", $"Langue: {lg}", "info");
            }

            try
            {
                // --- Login + recherche du 1er partenaire du CSV ---
                var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
                partenaireSelectionne = p;
                Console.WriteLine($"Partenaire: {p!.NumeroPartenaire} | Email: {p.Email}");

                await LoginEtAllerAuDashboard(p);

                lock (_reportLock)
                {
                    Report!.AddTestStep($"{label} - Partenaire affiche", page!).Wait();
                }

                // --- Validation des donnees du header via le composant ---
                var header = new ComponentsHeaderDashIvy(page!);
                await header.VerifierInfosPartenaireAsync(p, lg);
                await header.VerifierBoutonsHeaderAsync(lg);

                lock (_reportLock)
                {
                    Report!.AddTestStep($"{label} - Header valide", page!).Wait();
                    Report.AddTextBlock($"{label} - Valide",
                        $"Nr. SAP {p.NumeroPartenaire} / {p.Nom} / {p.Email} affiches dans le header",
                        "success");
                }

                LogGitHubActionsSuccess(label);
                Interlocked.Increment(ref _passedCount);
            }
            catch (Exception ex)
            {
                lock (_reportLock)
                {
                    Report!.AddTextBlock($"{label} - Erreur", ex.Message, "error");
                }
                LogGitHubActionsError(label, ex.Message);
                Interlocked.Increment(ref _failedCount);
                throw;
            }
        }

        #endregion
    }
}
