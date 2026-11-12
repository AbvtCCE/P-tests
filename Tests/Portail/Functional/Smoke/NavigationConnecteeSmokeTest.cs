using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.Pages.Portail;
using TestPlayrights.PortailV2Copilot.Pages;

namespace TestPlayrights.PortailV2Copilot.Smoke
{
    [TestFixture]
    [Category("CI_SMOKE")]
    public sealed class NavigationConnecteeSmokeTest
    {
        private const string Langue = "FR";
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

        [Test]
        public async Task MonCompte_AfficheLesInformationsDuPartenaire()
        {
            var partenaire = TrouverPartenaire(partenaire =>
                partenaire.NumeroPartenaire.HasValue,
                "un partenaire inscrit avec numéro client");
            await SeConnecterAsync(partenaire);

            var dashboard = new DashboardPage(_page!);
            await dashboard.AllerMonCompte(Langue);
            await dashboard.AllerMesDonnees(Langue);

            var profilPage = new ProfilPage(_page!, Langue);
            Assert.That(await profilPage.OuvrirCorrespondanceSiDisponibleAsync(), Is.True,
                "Le formulaire de coordonnées du compte n'est pas accessible.");
            var coordonnees = await profilPage.LireCoordonneesAsync();
            Assert.That(coordonnees, Is.Not.Empty,
                "Aucune coordonnée du partenaire n'est affichée dans Mon compte.");
        }

        [Test]
        public async Task ProduitsEtContrats_LesContratsSontAffichesEtDepliables()
        {
            var partenaire = TrouverPartenaire(partenaire =>
                partenaire.CompteContrat.HasValue && partenaire.numeroCompteur.HasValue,
                "un partenaire inscrit avec compte de contrat et compteur");
            await SeConnecterAsync(partenaire);

            var dashboard = new DashboardPage(_page!);
            await dashboard.AllerProduits(Langue);

            var panneaux = _page!.Locator("mygre-contract-premise-list mat-expansion-panel");
            await panneaux.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var produitsPage = new ProduitsContratsPage(_page);
            var produits = await produitsPage.ExtraireProduits();
            var nombrePanneaux = await panneaux.CountAsync();

            Assert.That(produits, Has.Count.EqualTo(nombrePanneaux),
                "Tous les contrats affichés doivent être lus.");
            Assert.That(produits, Is.Not.Empty, "Aucun contrat n'est affiché.");
            Assert.That(produits.Any(produit => !string.IsNullOrWhiteSpace(produit.Intitule)), Is.True,
                "Aucun contrat n'a de titre lisible.");

            var produitsActifs = produits.Where(produit => produit.Actif).ToList();
            Assert.That(produitsActifs, Is.Not.Empty, "Aucun contrat actif n'est affiché.");
            var produitsActifsSansDetails = produitsActifs
                .Where(produit => string.IsNullOrWhiteSpace(produit.Details))
                .Select(produit => produit.Intitule);
            Assert.That(produitsActifsSansDetails, Is.Empty,
                "Un ou plusieurs contrats actifs ne révèlent pas leur détail : " +
                string.Join(", ", produitsActifsSansDetails));
        }

        [Test]
        public async Task Consommation_SansDonneesAucuneErreurNestAffichee()
        {
            var partenaire = TrouverPartenaire(partenaire =>
                partenaire.CompteContrat.HasValue && partenaire.numeroCompteur.HasValue,
                "un partenaire inscrit avec compte de contrat et compteur");
            await SeConnecterAsync(partenaire);

            var consommationPage = new ConsommationPage(_page!);
            await consommationPage.Naviguer();

            var compteurs = await consommationPage.ExtraireCompteurs();
            await VerifierAucuneErreurAfficheeAsync(_page!);

            TestContext.Out.WriteLine(
                compteurs.Count == 0
                    ? "Aucune donnée de consommation en qualification; aucune erreur n'est affichée."
                    : $"Compteurs de consommation affichés: {compteurs.Count}; aucune erreur n'est affichée.");
        }

        [Test]
        public async Task Factures_LOngletEtCompteDeContratSontAffiches()
        {
            var partenaire = TrouverPartenaire(partenaire =>
                partenaire.FactureOuverte == true && partenaire.CompteContrat.HasValue,
                "un partenaire inscrit avec une facture ouverte");
            await SeConnecterAsync(partenaire);

            var dashboard = new DashboardPage(_page!);
            await dashboard.AllerFactures(Langue);

            var billsPage = new BillsPage(_page!);
            await billsPage.SelectionnerCompteContrat(Langue, partenaire.CompteContrat!.Value.ToString());

            Assert.That(await billsPage.CompterComptesContrat(Langue), Is.GreaterThan(0),
                "Aucun compte de contrat n'est affiché dans l'onglet Factures.");
        }

        private async Task SeConnecterAsync(Partenaire partenaire)
        {
            Assert.That(partenaire.Email, Is.Not.Null.And.Not.Empty,
                "Le partenaire sélectionné n'a pas d'adresse e-mail de connexion.");

            var connexionPage = new ConnexionPage(_page!, Langue);
            await connexionPage.NaviguerAsync();
            await connexionPage.SeConnecterAsync(partenaire.Email, Serializer.mdp);

            var accueilPage = new AccueilPage(_page!);
            Assert.That(await accueilPage.DetecterPopupFactureEmailAsync(), Is.False,
                "Une fenêtre de consentement aux factures par e-mail bloque la navigation; " +
                "le test ne valide ni ne modifie ce choix.");
            await accueilPage.FermerPopupInformationSiPresentAsync();
            await accueilPage.VerifierConnexionReussieAsync();
        }

        private static Partenaire TrouverPartenaire(
            Func<Partenaire, bool> critere,
            string description)
        {
            var partenaire = DataLoader.GetPartenaires(Serializer.PathToPartenaireCsv)
                .FirstOrDefault(candidate =>
                    candidate.InscritSurLePortail == true &&
                    candidate.AvertissementFactureParMail != true &&
                    candidate.AvertissementAutre != true &&
                    !string.IsNullOrWhiteSpace(candidate.Email) &&
                    critere(candidate));

            Assert.That(partenaire, Is.Not.Null,
                $"Aucun {description} n'a été trouvé dans le fichier de données.");
            return partenaire!;
        }

        private static async Task VerifierAucuneErreurAfficheeAsync(IPage page)
        {
            var elementsErreur = page.Locator(
                "[role='alert'], mat-error, .alert-danger, .alert-error, .error-message, [class*='error-banner']");
            var erreurs = new List<string>();
            var elements = await elementsErreur.AllAsync();

            foreach (var element in elements)
            {
                if (await element.IsVisibleAsync())
                {
                    var message = (await element.InnerTextAsync()).Trim();
                    erreurs.Add(string.IsNullOrWhiteSpace(message) ? "Message d'erreur visible sans texte." : message);
                }
            }

            var textePage = await page.Locator("body").InnerTextAsync();
            var erreurTextuelle = Regex.Match(
                textePage,
                @"\b(erreur|error|échec|échoué|failed|impossible|something went wrong)\b",
                RegexOptions.IgnoreCase);
            if (erreurTextuelle.Success)
            {
                erreurs.Add($"Texte d'erreur affiché : {erreurTextuelle.Value}");
            }

            Assert.That(erreurs, Is.Empty,
                $"La page Consommation affiche une erreur : {string.Join(" | ", erreurs)}");
        }
    }
}
