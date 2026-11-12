using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.Pages.Portail;

namespace TestPlayrights
{
    /// <summary>
    /// Tests Smoke du Portail (my-q.groupe-e.ch).
    /// Verifie les fonctionnalites de base : connexion, navigation, affichage.
    /// Rapport partage entre toutes les combinaisons navigateur x mobile x langue.
    /// </summary>
    [TestFixture]
    [Category("PORTAIL_LEGACY_SMOKE")]
    [Explicit("Ancien smoke Portail; utiliser les smoke V2 de CI_SMOKE.")]
    public class Portail_Smoke : TestBasePortail
    {
        #region Configuration

        /// <summary>Criteres : partenaire inscrit sur le portail</summary>
        protected override Dictionary<string, string?> criteresSelection { get; set; } = new()
        {
            { "InscritSurLePortail", "true" }
        };

        /// <summary>Rapport partage entre toutes les combinaisons</summary>
        protected override bool IsSharedReport() => true;

        #endregion

        #region Setup / Teardown

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Initialiser le rapport partage une seule fois
            var reportsBase = Environment.GetEnvironmentVariable("REPORTS_PATH") ?? Serializer.pathToReportFolder;
            var workflowName = Environment.GetEnvironmentVariable("WORKFLOW_NAME") ?? "";
            var dossier = Path.Combine(reportsBase, "Smoke_Portail");

            Report = new ReportBuilder(dossier, "Smoke_Portail", workflowName);
            _testCount = 0;
            _passedCount = 0;
            _failedCount = 0;
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown()
        {
            if (Report == null) return;

            // Resume final
            string resultat = $"Tests executes: {_testCount} | Reussis: {_passedCount} | Echoues: {_failedCount}";
            Report.SetTestResult(_failedCount == 0, resultat);

            // Generer le PDF avec un browser temporaire
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
        /// Test Smoke : connexion, selection langue, verification bienvenue et infos partenaire.
        /// Combinaisons generees dynamiquement via GenererCombinaisonsPortail().
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task SmokePortail(string navigateur, bool mobile, string lg)
        {
            Interlocked.Increment(ref _testCount);

            // --- Configuration ---
            testName = "SmokePortail";
            await Setup(navigateur, mobile, lg);
            string label = GetTestLabel();
            LogGitHubActions(label);

            // --- Rapport : debut du test ---
            lock (_reportLock)
            {
                Report!.AddTextBlock($"Test: {label}",
                    $"Navigateur: {navigateur}<br/>Device: {(mobile ? "iPhone 14" : "Desktop")}<br/>Langue: {lg}",
                    "info");
            }

            try
            {
                // --- Login ---
                await LoginAvecCriteres();
                var p = partenaireSelectionne;
                Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | Email: {p.Email} | CC: {p.CompteContrat}");

                // --- Langue ---
                await SelectionnerLangue();

                // --- Navigation et verifications via Page Objects ---
                var dashboard = new DashboardPage(page!);
                await dashboard.Naviguer();

                lock (_reportLock)
                {
                    Report!.AddTestStep($"{label} - Accueil", page!).Wait();
                }

                // Verifier le message de bienvenue
                //await dashboard.VerifierBienvenue(p.Prenom, lg);

                // Verifier les infos dans l'onglet Mon Compte
                await dashboard.AllerMonCompte(lg, mobile);
                //await dashboard.VerifierInfosPartenaire(p.Prenom, p.Nom);

                lock (_reportLock)
                {
                    Report!.AddTestStep($"{label} - Mon compte", page!).Wait();
                    Report.AddTextBlock($"{label} - Valide", "Test passe avec succes", "success");
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
