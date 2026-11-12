using System.Diagnostics;
using Microsoft.Playwright;
using TestPlayrights.Pages.Portail;
using TestPlayrights.PortailV2Copilot.Pages;

namespace TestPlayrights.PortailV2Copilot.Smoke
{
    [TestFixture]
    [Category("CI_SMOKE")]
    public sealed class PerformanceEtNonFonctionnelSmokeTest
    {
        private const string Langue = "FR";
        private static readonly TimeSpan BudgetParcours = TimeSpan.FromSeconds(20);
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

        [TestCase("Mon compte")]
        [TestCase("Produits et contrats")]
        [TestCase("Consommation")]
        [TestCase("Factures")]
        public async Task NavigationConnectee_RespecteLeBudgetPerformance(string parcours)
        {
            var partenaire = TrouverPartenaire(parcours);
            await SeConnecterAsync(partenaire);

            var chronometre = Stopwatch.StartNew();
            await VerifierParcoursAsync(parcours, partenaire);
            chronometre.Stop();

            TestContext.Out.WriteLine(
                $"PERF | parcours={parcours} | duree_ms={chronometre.Elapsed.TotalMilliseconds:F0} " +
                $"| budget_ms={BudgetParcours.TotalMilliseconds:F0} | apres_connexion=true");
            Assert.That(chronometre.Elapsed, Is.LessThanOrEqualTo(BudgetParcours),
                $"Le parcours « {parcours} » a dépassé le budget provisoire de " +
                $"{BudgetParcours.TotalSeconds:F0} s après connexion.");
        }

        [Test]
        public async Task TableauDeBord_MobileSansDebordementHorizontalNiErreurJavaScript()
        {
            var erreursJavaScript = new List<string>();
            _page!.PageError += (_, erreur) => erreursJavaScript.Add(erreur);

            var partenaire = TrouverPartenaire("Mon compte");
            await SeConnecterAsync(partenaire);
            await _page.SetViewportSizeAsync(390, 844);
            await _page.Locator("[role='tab']").First.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible
            });

            var debordementHorizontal = await _page.EvaluateAsync<int>(
                "() => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) " +
                "- document.documentElement.clientWidth");

            Assert.That(debordementHorizontal, Is.LessThanOrEqualTo(1),
                "Le tableau de bord mobile déborde horizontalement.");
            Assert.That(erreursJavaScript, Is.Empty,
                $"Des exceptions JavaScript non interceptées ont été émises : " +
                $"{string.Join(" | ", erreursJavaScript)}");
        }

        private async Task VerifierParcoursAsync(string parcours, Partenaire partenaire)
        {
            var dashboard = new DashboardPage(_page!);
            switch (parcours)
            {
                case "Mon compte":
                    await dashboard.AllerMonCompte(Langue);
                    await dashboard.AllerMesDonnees(Langue);
                    var profil = new ProfilPage(_page!, Langue);
                    Assert.That(await profil.OuvrirCorrespondanceSiDisponibleAsync(), Is.True,
                        "Le formulaire de coordonnées du compte n'est pas accessible.");
                    Assert.That(await profil.LireCoordonneesAsync(), Is.Not.Empty,
                        "Aucune coordonnée n'est affichée dans Mon compte.");
                    break;

                case "Produits et contrats":
                    await dashboard.AllerProduits(Langue);
                    var panneaux = _page!.Locator("mygre-contract-premise-list mat-expansion-panel");
                    await panneaux.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                    var produits = await new ProduitsContratsPage(_page).ExtraireProduits();
                    Assert.That(produits, Is.Not.Empty, "Aucun contrat n'est affiché.");
                    var produitsActifs = produits.Where(produit => produit.Actif).ToList();
                    Assert.That(produitsActifs, Is.Not.Empty, "Aucun contrat actif n'est affiché.");
                    var produitsActifsSansDetails = produitsActifs
                        .Where(produit => string.IsNullOrWhiteSpace(produit.Details))
                        .Select(produit => produit.Intitule);
                    Assert.That(produitsActifsSansDetails, Is.Empty,
                        "Un ou plusieurs contrats actifs ne révèlent pas leur détail : " +
                        string.Join(", ", produitsActifsSansDetails));
                    break;

                case "Consommation":
                    var consommation = new ConsommationPage(_page!);
                    await consommation.Naviguer();
                    _ = await consommation.ExtraireCompteurs();
                    break;

                case "Factures":
                    await dashboard.AllerFactures(Langue);
                    var factures = new BillsPage(_page!);
                    await factures.SelectionnerCompteContrat(
                        Langue, partenaire.CompteContrat!.Value.ToString());
                    Assert.That(await factures.CompterComptesContrat(Langue), Is.GreaterThan(0),
                        "Aucun compte de contrat n'est affiché.");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(parcours), parcours,
                        "Parcours de performance non pris en charge.");
            }
        }

        private async Task SeConnecterAsync(Partenaire partenaire)
        {
            Assert.That(partenaire.Email, Is.Not.Null.And.Not.Empty,
                "Le partenaire sélectionné n'a pas d'adresse e-mail de connexion.");

            var connexion = new ConnexionPage(_page!, Langue);
            await connexion.NaviguerAsync();
            await connexion.SeConnecterAsync(partenaire.Email, Serializer.mdp);

            var accueil = new AccueilPage(_page!);
            Assert.That(await accueil.DetecterPopupFactureEmailAsync(), Is.False,
                "Une fenêtre de consentement bloque la navigation; le test ne modifie pas ce choix.");
            await accueil.FermerPopupInformationSiPresentAsync();
            await accueil.VerifierConnexionReussieAsync();
        }

        private static Partenaire TrouverPartenaire(string parcours)
        {
            Func<Partenaire, bool> critere = parcours switch
            {
                "Mon compte" => partenaire => partenaire.NumeroPartenaire.HasValue,
                "Produits et contrats" or "Consommation" =>
                    partenaire => partenaire.CompteContrat.HasValue && partenaire.numeroCompteur.HasValue,
                "Factures" =>
                    partenaire => partenaire.FactureOuverte == true && partenaire.CompteContrat.HasValue,
                _ => throw new ArgumentOutOfRangeException(nameof(parcours), parcours,
                    "Parcours de performance non pris en charge.")
            };

            var partenaire = DataLoader.GetPartenaires(Serializer.PathToPartenaireCsv)
                .FirstOrDefault(candidate =>
                    candidate.InscritSurLePortail == true &&
                    candidate.AvertissementFactureParMail != true &&
                    candidate.AvertissementAutre != true &&
                    !string.IsNullOrWhiteSpace(candidate.Email) &&
                    critere(candidate));

            Assert.That(partenaire, Is.Not.Null,
                $"Aucun partenaire inscrit adapté au parcours « {parcours} » n'a été trouvé.");
            return partenaire!;
        }
    }
}
