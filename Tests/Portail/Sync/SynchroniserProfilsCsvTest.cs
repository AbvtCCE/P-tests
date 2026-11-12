using System.Text;
using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.PortailV2Copilot.Pages;

namespace TestPlayrights.PortailV2Copilot.Sync
{
    /// <summary>
    /// Lit les profils des partenaires inscrits et prépare la mise à jour du CSV local.
    /// Le fichier n'est écrit que si PORTAIL_V2_APPLY_SYNC=true.
    /// </summary>
    [TestFixture]
    [Category("PORTAIL_DATA_SYNC")]
    public sealed class SynchroniserProfilsCsvTest
    {
        private const string Langue = "FR";
        private static readonly string[] ColonnesSynchronisees =
        {
            "Prenom", "Nom", "EmailCorrespondance", "Mobile", "Rue", "NumRue", "CodePostal",
            "AvertissementFactureParMail"
        };

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
        public async Task SynchroniserProfilsInscritsVersCsv()
        {
            var cheminCsv = Serializer.PathToPartenaireCsv;
            var lignesOriginales = await File.ReadAllLinesAsync(cheminCsv);
            Assert.That(lignesOriginales.Length, Is.GreaterThan(1),
                $"Le CSV est vide ou introuvable : {cheminCsv}");

            var entete = lignesOriginales[0].Split(';').Select(cellule => cellule.Trim()).ToList();
            var colonnesAjoutees = ColonnesSynchronisees
                .Where(colonne => !entete.Contains(colonne, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            entete.AddRange(colonnesAjoutees);

            var index = ColonnesSynchronisees.ToDictionary(
                colonne => colonne,
                colonne => entete.IndexOf(colonne),
                StringComparer.OrdinalIgnoreCase);
            var indexEmail = entete.IndexOf("Email");
            var indexInscription = entete.IndexOf("InscritSurLePortail");

            Assert.That(indexEmail, Is.GreaterThanOrEqualTo(0), "Colonne Email absente du CSV.");
            Assert.That(indexInscription, Is.GreaterThanOrEqualTo(0),
                "Colonne InscritSurLePortail absente du CSV.");

            foreach (var colonne in ColonnesSynchronisees)
            {
                Assert.That(index[colonne], Is.GreaterThanOrEqualTo(0),
                    $"Colonne {colonne} absente du CSV.");
            }

            var lignesInitiales = (string[])lignesOriginales.Clone();
            if (colonnesAjoutees.Length > 0)
            {
                lignesInitiales[0] = string.Join(";", entete);
                for (var ligneIndex = 1; ligneIndex < lignesInitiales.Length; ligneIndex++)
                {
                    if (!string.IsNullOrWhiteSpace(lignesInitiales[ligneIndex]))
                    {
                        lignesInitiales[ligneIndex] += string.Concat(
                            Enumerable.Repeat(";", colonnesAjoutees.Length));
                    }
                }
            }

            var lignesMisesAJour = (string[])lignesInitiales.Clone();
            var erreurs = new List<string>();
            var partenairesTraites = 0;
            var partenairesConnectes = 0;
            var profilsSansCorrespondance = 0;
            var avertissementsFactureEmail = 0;
            var cellulesModifiees = 0;
            var limite = LireLimite();
            var ligneCible = LireLigneCible();
            var appliquer = string.Equals(
                Environment.GetEnvironmentVariable("PORTAIL_V2_APPLY_SYNC"),
                "true",
                StringComparison.OrdinalIgnoreCase);
            Assert.That(!appliquer || (limite == 0 && !ligneCible.HasValue), Is.True,
                "L'écriture requiert le traitement complet : retirez PORTAIL_V2_SYNC_LIMIT et " +
                "PORTAIL_V2_SYNC_ROW.");

            for (var ligneIndex = 1; ligneIndex < lignesOriginales.Length; ligneIndex++)
            {
                if (string.IsNullOrWhiteSpace(lignesOriginales[ligneIndex]))
                {
                    continue;
                }

                if (ligneCible.HasValue && ligneIndex + 1 != ligneCible.Value)
                {
                    continue;
                }

                var cellules = lignesInitiales[ligneIndex].Split(';').ToList();
                if (cellules.Count <= indexInscription ||
                    !EstInscrit(cellules[indexInscription]))
                {
                    continue;
                }

                while (cellules.Count < entete.Count)
                {
                    cellules.Add("");
                }

                var emailConnexion = cellules[indexEmail].Trim();
                if (string.IsNullOrWhiteSpace(emailConnexion))
                {
                    erreurs.Add($"Ligne {ligneIndex + 1}: adresse email vide.");
                    continue;
                }

                if (limite > 0 && partenairesTraites >= limite)
                {
                    break;
                }

                partenairesTraites++;
                try
                {
                    var resultatProfil = await SynchroniserLigneAsync(
                        cellules,
                        index,
                        emailConnexion,
                        ligneIndex + 1);
                    lignesMisesAJour[ligneIndex] = string.Join(";", cellules);
                    partenairesConnectes++;
                    if (!resultatProfil.CorrespondanceDisponible)
                    {
                        profilsSansCorrespondance++;
                    }
                    if (resultatProfil.AvertissementFactureEmail)
                    {
                        avertissementsFactureEmail++;
                    }
                    cellulesModifiees += ColonnesSynchronisees.Count(colonne =>
                        !string.Equals(
                            lignesInitiales[ligneIndex].Split(';')[index[colonne]],
                            cellules[index[colonne]],
                            StringComparison.Ordinal));
                }
                catch (Exception ex)
                {
                    erreurs.Add($"Ligne {ligneIndex + 1}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            var lignesModifiees = Enumerable.Range(1, lignesOriginales.Length - 1)
                .Where(i => !string.Equals(lignesInitiales[i], lignesMisesAJour[i], StringComparison.Ordinal))
                .ToArray();

            Assert.That(ligneCible.HasValue == false || partenairesTraites > 0, Is.True,
                $"Aucun partenaire inscrit n'a été trouvé à la ligne CSV {ligneCible}.");

            if (appliquer &&
                (colonnesAjoutees.Length > 0 || lignesModifiees.Length > 0))
            {
                await SauvegarderCsvAvecBackupAsync(
                    cheminCsv,
                    lignesOriginales,
                    lignesMisesAJour,
                    entete);
            }

            var mode = !appliquer
                ? "aperçu"
                : erreurs.Count == 0
                    ? "écriture"
                    : "écriture partielle";
            TestContext.Out.WriteLine(
                $"Partenaires traités: {partenairesTraites}; profils lus: {partenairesConnectes}; " +
                $"sans formulaire de correspondance: {profilsSansCorrespondance}; " +
                $"popup facture e-mail détectée: {avertissementsFactureEmail}; " +
                $"lignes à mettre à jour: {lignesModifiees.Length}; champs modifiés: {cellulesModifiees}; " +
                $"colonnes à ajouter: {(colonnesAjoutees.Length == 0 ? "aucune" : string.Join(", ", colonnesAjoutees))}; " +
                $"mode: {mode}; erreurs: {erreurs.Count}.");

            Assert.That(erreurs, Is.Empty,
                $"La synchronisation est incomplète : {string.Join(" | ", erreurs)}");
        }

        private async Task<(bool CorrespondanceDisponible, bool AvertissementFactureEmail)> SynchroniserLigneAsync(
            IList<string> cellules,
            IReadOnlyDictionary<string, int> index,
            string email,
            int numeroLigne)
        {
            for (var tentative = 1; tentative <= 2; tentative++)
            {
                try
                {
                    await using var contexte = await _browser!.NewContextAsync(new BrowserNewContextOptions
                    {
                        IgnoreHTTPSErrors = true
                    });
                    var page = await contexte.NewPageAsync();
                    var connexion = new ConnexionPage(page, Langue);

                    await connexion.NaviguerAsync();
                    await connexion.SeConnecterAsync(email, Serializer.mdp);

                    var accueil = new AccueilPage(page);
                    var avertissementFactureEmail =
                        await accueil.DetecterPopupFactureEmailAsync();
                    if (avertissementFactureEmail)
                    {
                        SetSiPresent(cellules, index["AvertissementFactureParMail"], "True");
                    }
                    else
                    {
                        await accueil.FermerPopupInformationSiPresentAsync();
                    }
                    await accueil.VerifierConnexionReussieAsync();

                    var nomComplet = await accueil.LireNomCompletAsync(Langue);
                    AppliquerNomComplet(cellules, index, nomComplet);

                    if (avertissementFactureEmail)
                    {
                        await EnregistrerDerniereCaptureAsync(page);
                        return (false, true);
                    }

                    await accueil.OuvrirProfilAsync(Langue);
                    var avertissementApresOuvertureProfil =
                        await accueil.DetecterPopupFactureEmailAsync();
                    if (avertissementApresOuvertureProfil)
                    {
                        SetSiPresent(cellules, index["AvertissementFactureParMail"], "True");
                        await EnregistrerDerniereCaptureAsync(page);
                        return (false, true);
                    }
                    else
                    {
                        await accueil.FermerPopupInformationSiPresentAsync();
                    }
                    var profil = new ProfilPage(page, Langue);
                    if (!await profil.OuvrirCorrespondanceSiDisponibleAsync())
                    {
                        await EnregistrerDerniereCaptureAsync(page);
                        return (false, avertissementFactureEmail);
                    }

                    var coordonnees = await profil.LireCoordonneesAsync();
                    AppliquerValeurs(cellules, index, coordonnees);
                    await EnregistrerDerniereCaptureAsync(page);
                    return (true, avertissementFactureEmail);
                }
                catch (TimeoutException) when (tentative == 1)
                {
                    TestContext.Out.WriteLine(
                        $"Ligne {numeroLigne}: timeout navigateur; nouvelle tentative unique.");
                }
            }

            throw new TimeoutException(
                $"Ligne {numeroLigne}: la synchronisation a expiré après deux tentatives.");
        }

        private async Task EnregistrerDerniereCaptureAsync(IPage page)
        {
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
                    $"PREUVE: capture du dernier profil impossible: {exception.GetType().Name}: {exception.Message}");
            }
        }

        private static void AppliquerNomComplet(
            IList<string> cellules,
            IReadOnlyDictionary<string, int> index,
            string messageBienvenue)
        {
            var prefixeBienvenue = ResourceHelper.GetStringPortail("Welcome", Langue);
            var nomComplet = messageBienvenue
                .Replace(prefixeBienvenue, "", StringComparison.OrdinalIgnoreCase)
                .Replace("!", "")
                .Trim();

            var ancienPrenom = cellules[index["Prenom"]].Trim();
            if (!string.IsNullOrWhiteSpace(ancienPrenom) &&
                nomComplet.StartsWith(ancienPrenom, StringComparison.OrdinalIgnoreCase) &&
                (nomComplet.Length == ancienPrenom.Length ||
                 char.IsWhiteSpace(nomComplet[ancienPrenom.Length])))
            {
                SetSiPresent(cellules, index["Prenom"], ancienPrenom);
                SetSiPresent(cellules, index["Nom"], nomComplet[ancienPrenom.Length..].Trim());
                return;
            }

            var parties = nomComplet.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parties.Length > 0)
            {
                SetSiPresent(cellules, index["Prenom"], parties[0]);
                if (parties.Length > 1)
                {
                    SetSiPresent(cellules, index["Nom"], string.Join(" ", parties.Skip(1)));
                }
            }
        }

        private static void AppliquerValeurs(
            IList<string> cellules,
            IReadOnlyDictionary<string, int> index,
            IReadOnlyDictionary<string, string> valeurs)
        {
            foreach (var valeur in valeurs)
            {
                if (index.TryGetValue(valeur.Key, out var colonneIndex))
                {
                    SetSiPresent(cellules, colonneIndex, valeur.Value);
                }
            }
        }

        private static void SetSiPresent(IList<string> cellules, int colonneIndex, string valeur)
        {
            if (!string.IsNullOrWhiteSpace(valeur) && colonneIndex < cellules.Count)
            {
                cellules[colonneIndex] = valeur.Replace(';', ',').Trim();
            }
        }

        private static bool EstInscrit(string valeur) =>
            valeur.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ||
            valeur.Trim() == "1";

        private static int LireLimite()
        {
            var valeur = Environment.GetEnvironmentVariable("PORTAIL_V2_SYNC_LIMIT");
            if (string.IsNullOrWhiteSpace(valeur))
            {
                return 0;
            }

            if (int.TryParse(valeur, out var limite) && limite > 0)
            {
                return limite;
            }

            throw new InvalidOperationException(
                "PORTAIL_V2_SYNC_LIMIT doit être un entier strictement positif.");
        }

        private static int? LireLigneCible()
        {
            var valeur = Environment.GetEnvironmentVariable("PORTAIL_V2_SYNC_ROW");
            if (string.IsNullOrWhiteSpace(valeur))
            {
                return null;
            }

            if (int.TryParse(valeur, out var ligne) && ligne > 1)
            {
                return ligne;
            }

            throw new InvalidOperationException(
                "PORTAIL_V2_SYNC_ROW doit être le numéro de ligne CSV (entête = ligne 1).");
        }

        private static async Task SauvegarderCsvAvecBackupAsync(
            string cheminCsv,
            string[] lignesOriginales,
            string[] lignesMisesAJour,
            IReadOnlyCollection<string> entete)
        {
            var dossierBackup = Path.Combine(
                Serializer.pathToReportFolder,
                "PortailV2Copilot",
                "Backups");
            Directory.CreateDirectory(dossierBackup);

            var cheminBackup = Path.Combine(
                dossierBackup,
                $"Partenaires_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            await File.WriteAllLinesAsync(cheminBackup, lignesOriginales, new UTF8Encoding(false));

            var cheminTemporaire = $"{cheminCsv}.{Guid.NewGuid():N}.tmp";
            try
            {
                lignesMisesAJour[0] = string.Join(";", entete);
                await File.WriteAllLinesAsync(
                    cheminTemporaire,
                    lignesMisesAJour,
                    new UTF8Encoding(false));
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
    }
}
