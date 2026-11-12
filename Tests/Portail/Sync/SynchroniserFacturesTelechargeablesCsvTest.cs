using System.Text;
using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.Pages.Portail;
using TestPlayrights.PortailV2Copilot.Pages;

namespace TestPlayrights.PortailV2Copilot.Sync
{
    /// <summary>
    /// Vérifie sur le Portail Qualité si chaque partenaire inscrit a des factures
    /// avec un bouton de téléchargement actif, puis prépare FactureOuverte dans le CSV.
    /// L'écriture est désactivée par défaut.
    /// </summary>
    [TestFixture]
    [Category("PORTAIL_DATA_SYNC")]
    [Explicit("Parcourt les comptes inscrits; l'écriture du CSV requiert PORTAIL_FACTURES_APPLY_SYNC=true.")]
    public sealed class SynchroniserFacturesTelechargeablesCsvTest
    {
        private const string Langue = "FR";
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private byte[]? _derniereCapture;
        private string? _urlDerniereCapture;

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
            await TestEvidence.CaptureAsync(
                page: null,
                screenshotBytes: _derniereCapture,
                pageUrl: _urlDerniereCapture);
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

        [Test]
        public async Task SynchroniserFacturesTelechargeablesVersCsv()
        {
            var cheminCsv = Serializer.PathToPartenaireCsv;
            var lignesOriginales = await File.ReadAllLinesAsync(cheminCsv);
            Assert.That(lignesOriginales.Length, Is.GreaterThan(1),
                $"Le CSV est vide ou introuvable : {cheminCsv}");

            var entete = lignesOriginales[0].Split(';').Select(cellule => cellule.Trim()).ToList();
            var indexNumero = IndexColonne(entete, "NumeroPartenaire");
            var indexEmail = IndexColonne(entete, "Email");
            var indexCompte = IndexColonne(entete, "CompteContrat");
            var indexInscription = IndexColonne(entete, "InscritSurLePortail");
            var indexFactureOuverte = IndexColonne(entete, "FactureOuverte");

            var lignes = lignesOriginales
                .Select(ligne => ligne.Split(';').ToList())
                .ToArray();

            var groupesComptes = Enumerable.Range(1, lignes.Length - 1)
                .Where(index => !string.IsNullOrWhiteSpace(lignesOriginales[index]))
                .Where(index => lignes[index].Count > indexInscription &&
                    EstVrai(lignes[index][indexInscription]))
                .Where(index => lignes[index].Count > indexCompte &&
                    !string.IsNullOrWhiteSpace(lignes[index][indexCompte]))
                .GroupBy(index => (
                    NumeroPartenaire: Valeur(lignes[index], indexNumero),
                    CompteContrat: Valeur(lignes[index], indexCompte)))
                .Select(groupe => new CompteCsv(
                    groupe.Key.NumeroPartenaire,
                    groupe.Key.CompteContrat,
                    Valeur(lignes[groupe.First()], indexEmail),
                    groupe.ToArray()))
                .ToArray();

            var limite = LireLimite();
            var ligneCible = LireLigneCible();
            var appliquer = string.Equals(
                Environment.GetEnvironmentVariable("PORTAIL_FACTURES_APPLY_SYNC"),
                "true",
                StringComparison.OrdinalIgnoreCase);
            var comptesAAnalyser = groupesComptes
                .Where(compte => !ligneCible.HasValue ||
                    compte.LignesCsv.Any(index => index + 1 == ligneCible.Value))
                .Take(limite == 0 ? int.MaxValue : limite)
                .ToArray();

            Assert.That(comptesAAnalyser, Is.Not.Empty,
                ligneCible.HasValue
                    ? $"Aucun compte inscrit avec numéro de contrat n'a été trouvé à la ligne CSV {ligneCible}."
                    : "Aucun compte inscrit avec numéro de contrat n'a été trouvé dans le CSV.");

            var erreurs = new List<string>();
            var comptesAvecFactures = 0;
            var comptesSansFactures = 0;
            var lignesModifiees = 0;

            foreach (var compte in comptesAAnalyser)
            {
                if (string.IsNullOrWhiteSpace(compte.Email))
                {
                    erreurs.Add($"Ligne CSV {compte.LignesCsv[0] + 1}: adresse e-mail de connexion absente.");
                    continue;
                }

                try
                {
                    var indicesFactures = await VerifierCompteAsync(compte);
                    var factureOuverte = indicesFactures.Count > 0;
                    if (factureOuverte)
                    {
                        comptesAvecFactures++;
                    }
                    else
                    {
                        comptesSansFactures++;
                    }

                    foreach (var indexLigne in compte.LignesCsv)
                    {
                        while (lignes[indexLigne].Count < entete.Count)
                        {
                            lignes[indexLigne].Add("");
                        }

                        var valeur = factureOuverte ? "True" : "False";
                        if (!string.Equals(
                            lignes[indexLigne][indexFactureOuverte],
                            valeur,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            lignes[indexLigne][indexFactureOuverte] = valeur;
                            lignesModifiees++;
                        }
                    }

                    TestContext.Out.WriteLine(
                        $"Ligne CSV {compte.LignesCsv[0] + 1}: " +
                        $"{indicesFactures.Count} facture(s) téléchargeable(s).");
                }
                catch (Exception exception)
                {
                    erreurs.Add(
                        $"Ligne CSV {compte.LignesCsv[0] + 1}: " +
                        $"{exception.GetType().Name}: {exception.Message}");
                }
            }

            TestContext.Out.WriteLine(
                $"Comptes analysés: {comptesAAnalyser.Length}; " +
                $"avec factures: {comptesAvecFactures}; sans facture: {comptesSansFactures}; " +
                $"lignes CSV modifiées: {lignesModifiees}; " +
                $"mode: {(appliquer ? "écriture" : "aperçu")}; erreurs: {erreurs.Count}.");

            Assert.That(erreurs, Is.Empty,
                $"Synchronisation incomplète; le CSV n'a pas été écrit : {string.Join(" | ", erreurs)}");

            if (appliquer && lignesModifiees > 0)
            {
                await SauvegarderCsvAvecBackupAsync(cheminCsv, lignesOriginales, lignes);
            }
        }

        private async Task<List<int>> VerifierCompteAsync(CompteCsv compte)
        {
            await using var contexte = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            var page = await contexte.NewPageAsync();
            await TestTools.ActiverSuiviEtapes(page);

            var connexion = new ConnexionPage(page, Langue);
            await connexion.NaviguerAsync();
            await connexion.SeConnecterAsync(compte.Email, Serializer.mdp);

            var accueil = new AccueilPage(page);
            if (await accueil.DetecterPopupFactureEmailAsync())
            {
                throw new InvalidOperationException(
                    "La fenêtre de consentement aux factures par e-mail empêche de vérifier les factures; " +
                    "le test ne modifie pas cette préférence.");
            }

            await accueil.FermerPopupInformationSiPresentAsync();
            await accueil.VerifierConnexionReussieAsync();

            var tableauDeBord = new DashboardPage(page);
            await tableauDeBord.AllerFactures(Langue);

            var factures = new BillsPage(page);
            await factures.SelectionnerCompteContrat(Langue, compte.CompteContrat);
            var indicesFactures = await factures.ListerIndicesFacturesTelechargeables(Langue);

            try
            {
                _derniereCapture = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = true,
                    Animations = ScreenshotAnimations.Disabled,
                    Timeout = 15000
                });
                _urlDerniereCapture = page.Url;
            }
            catch (Exception exception)
            {
                TestContext.Out.WriteLine(
                    $"PREUVE: capture de la page Factures impossible: " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }

            return indicesFactures;
        }

        private static int IndexColonne(IReadOnlyList<string> entete, string nom)
        {
            var index = -1;
            for (var i = 0; i < entete.Count; i++)
            {
                if (string.Equals(entete[i], nom, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            Assert.That(index, Is.GreaterThanOrEqualTo(0), $"Colonne {nom} absente du CSV.");
            return index;
        }

        private static string Valeur(IReadOnlyList<string> cellules, int index) =>
            index < cellules.Count ? cellules[index].Trim() : "";

        private static bool EstVrai(string valeur) =>
            valeur.Equals("true", StringComparison.OrdinalIgnoreCase) || valeur == "1";

        private static int LireLimite()
        {
            var valeur = Environment.GetEnvironmentVariable("PORTAIL_FACTURES_SYNC_LIMIT");
            if (string.IsNullOrWhiteSpace(valeur))
            {
                return 0;
            }

            if (int.TryParse(valeur, out var limite) && limite > 0)
            {
                return limite;
            }

            throw new InvalidOperationException(
                "PORTAIL_FACTURES_SYNC_LIMIT doit être un entier strictement positif.");
        }

        private static int? LireLigneCible()
        {
            var valeur = Environment.GetEnvironmentVariable("PORTAIL_FACTURES_SYNC_ROW");
            if (string.IsNullOrWhiteSpace(valeur))
            {
                return null;
            }

            if (int.TryParse(valeur, out var ligne) && ligne > 1)
            {
                return ligne;
            }

            throw new InvalidOperationException(
                "PORTAIL_FACTURES_SYNC_ROW doit être le numéro de ligne CSV (entête = ligne 1).");
        }

        private static async Task SauvegarderCsvAvecBackupAsync(
            string cheminCsv,
            IReadOnlyList<string> lignesOriginales,
            IReadOnlyList<List<string>> lignes)
        {
            var dossierBackup = Path.Combine(
                Serializer.pathToReportFolder,
                "PortailV2Copilot",
                "Backups");
            Directory.CreateDirectory(dossierBackup);

            var cheminBackup = Path.Combine(
                dossierBackup,
                $"Partenaires_{DateTime.Now:yyyyMMdd_HHmmss}_avant_sync_factures.csv");
            await File.WriteAllLinesAsync(
                cheminBackup,
                lignesOriginales,
                new UTF8Encoding(false));

            var cheminTemporaire = $"{cheminCsv}.{Guid.NewGuid():N}.tmp";
            try
            {
                var lignesCsv = new string[lignes.Count];
                lignesCsv[0] = string.Join(";", lignes[0].Select(cellule => cellule.Trim()));
                for (var i = 1; i < lignes.Count; i++)
                {
                    lignesCsv[i] = string.Join(";", lignes[i]);
                }

                await File.WriteAllLinesAsync(cheminTemporaire, lignesCsv, new UTF8Encoding(false));
                File.Move(cheminTemporaire, cheminCsv, true);
                TestContext.Out.WriteLine($"Sauvegarde originale: {cheminBackup}");
            }
            finally
            {
                if (File.Exists(cheminTemporaire))
                {
                    File.Delete(cheminTemporaire);
                }
            }
        }

        private sealed record CompteCsv(
            string NumeroPartenaire,
            string CompteContrat,
            string Email,
            int[] LignesCsv);
    }
}
