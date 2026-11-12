using System.Text.RegularExpressions;
using Microsoft.Playwright;
/*

o-O-o o--o  o-o  o-O-o     o-O-o  o-o   o-o  o     o-o
  |   |    |       |         |   o   o o   o |    |
  |   O-o   o-o    |         |   |   | |   | |     o-o
  |   |        |   |         |   o   o o   o |        |
  o   o--o o--o    o         o    o-o   o-o  O---oo--o



*/
namespace TestPlayrights
{
    public static class TestTools
    {
        #region Browser Management

        /// <summary>
        /// Crée et lance un navigateur Playwright.
        /// WebKit ne supporte pas --start-maximized.
        /// </summary>
        public static async Task<IBrowser> InitializeBrowser(string browserType = "chromium", bool headless = true)
        {
            var playwright = await Playwright.CreateAsync();

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = headless,
                SlowMo = TestConfig.SlowMo > 0 ? TestConfig.SlowMo : null,
                Args = browserType.ToLower() == "webkit"
                    ? Array.Empty<string>()
                    : new[] { "--start-maximized" }
            };

            return browserType.ToLower() switch
            {
                "firefox" => await playwright.Firefox.LaunchAsync(launchOptions),
                "webkit" => await playwright.Webkit.LaunchAsync(launchOptions),
                _ => await playwright.Chromium.LaunchAsync(launchOptions)
            };
        }

        /// <summary>
        /// Crée un contexte navigateur (desktop 1920x1080 ou iPhone 12 mobile)
        /// </summary>
        public static async Task<IBrowserContext> CreateContext(IBrowser browser, bool isMobile = false)
        {
            BrowserNewContextOptions options;

            if (isMobile)
            {
                var playwright = await Playwright.CreateAsync();
                var device = playwright.Devices["iPhone 12"];
                options = new BrowserNewContextOptions(device)
                {
                    IgnoreHTTPSErrors = true
                };
            }
            else
            {
                options = new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    IgnoreHTTPSErrors = true
                };
            }

            return await browser.NewContextAsync(options);
        }

        #endregion

        #region Screenshots

        /// <summary>
        /// Prend une capture d'écran de la page entière.
        /// Ajoute automatiquement l'extension .png si absente.
        /// </summary>
        public static async Task TakeAScreenshot(IPage page, string cheminCapture)
        {
            var path = cheminCapture;
            if (!Path.HasExtension(path) || !new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(path).ToLower()))
            {
                path = path + ".png";
            }
            await page.ScreenshotAsync(new()
            {
                Path = path,
                FullPage = true
            });
        }

        #endregion

        #region Portail - Authentification

        /// <summary>
        /// Login sur le portail : navigue vers l'URL, remplit email/password, soumet.
        /// Gère les popups d'information après connexion.
        /// </summary>
        public static async Task<bool> LoginPortail(IPage page, string email, string password, ReportBuilder? report = null, string? testName = null)
        {
            try
            {
                await page.GotoAsync(Serializer.GetUrlPortailQual());
                await page.WaitForSelectorAsync("input[name='username']", new() { Timeout = 5000 });

                await page.FillAsync("input[name='username']", email);
                
                await page.GetByRole(AriaRole.Button, new() { Name = "Suivant" }).ClickAsync();
                await page.FillAsync("input[name='password']", password);

                if (report != null && testName != null)
                {
                    await report.AddTestStep($"Connexion compte {email}", page, testName);
                }

                await page.ClickAsync("button[type='submit']");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await PopUpPortal(page);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur login Portail: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Trouve un partenaire par critères puis se connecte sur le portail
        /// </summary>
        public static async Task<Partenaire> LogWithCriteria(IPage page, List<Partenaire> tousLesPartenaires, Dictionary<string, string> criteres, ReportBuilder report, string testName)
        {
            Partenaire partenaireTrouve = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteres!)!;

            if (partenaireTrouve == null)
            {
                Assert.Fail("Aucun partenaire ne correspond aux criteres");
            }

            await LoginPortail(page, partenaireTrouve.Email, Serializer.mdp, report, testName);
            return partenaireTrouve;
        }

        public static async Task LoginToKeycloak(IPage page)
        {
             await page.GotoAsync(Serializer.GetUrlKeyClockQual());

             await page.GetByRole(AriaRole.Textbox, new() { Name = "Username or email" }).FillAsync(Serializer.EmailKeycloak);
             await page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(Serializer.MdpKeycloak);
             await page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        }

        public static async Task AnonymizeNameOnPage(IPage page)
        {
            bool allPageGood=false;

            while(!allPageGood)
            {
                var rows = page.Locator("tbody tr");
                var rowCount = await rows.CountAsync();
                Console.WriteLine("*******************************NOMBRE DE ROW TROUVER : "+rowCount);
                allPageGood=true;
                for (var r = 0; r < rowCount; r++)
                {
                    var cells = rows.Nth(r).Locator("td");

                    var col1 = (await cells.Nth(1).InnerTextAsync()).Trim();
                    var col2 = (await cells.Nth(2).InnerTextAsync()).Trim();
                    var col3 = (await cells.Nth(3).InnerTextAsync()).Trim();

                    if (col1 != col2)
                    {
                        Console.WriteLine(
                            $"ROW {r} | COL1 and COL2 differ | COL1={col1} | COL2={col2}"
                        );
                    }
                    else
                    {
                        if (!Regex.IsMatch(col1, @"^test4gre\+\d+@gmail\.com$"))
                    {
                        Console.WriteLine(
                            $"ROW {r} | Invalid email format | VALUE={col1}"
                        );
                    }

                    
                    if (!col3.Contains("Test", StringComparison.OrdinalIgnoreCase))
                    {
                        allPageGood=false;
                        Console.WriteLine(
                            $"ROW {r} | COL3 does not contain 'Test' | VALUE={col3}"
                        );
                        // La cellule Username contient un <a> qui navigue vers la fiche
                        // utilisateur (SPA Keycloak). Cliquer le <td> autour ne suit pas le lien.
                        var lienUtilisateur = cells.Nth(1).Locator("a");
                        await lienUtilisateur.WaitForAsync();
                        await lienUtilisateur.ClickAsync();

                        // TODO: check avec Manu et ajouter dans les mail du CAB

                        await page.GetByTestId("firstName-input").FillAsync("Test");
                        await page.GetByTestId("lastName-input").FillAsync("Test");
                        await page.GetByTestId("save-user").ClickAsync();
                        await page.GetByRole(AriaRole.Heading, new() { Name = "The user has been saved" }).WaitForAsync();
                        await page.GetByRole(AriaRole.Button, new() { Name = "Close alert: The user has" }).ClickAsync();
                        await page.GetByRole(AriaRole.Heading, new() { Name = "The user has been saved" }).WaitForAsync(new() { State = WaitForSelectorState.Hidden });
                        await page.GetByLabel("Breadcrumb").GetByRole(AriaRole.Link, new() { Name = "Users" }).ClickAsync();
                        await ClientViewBy10Page(page);
                    }

                    }

                    
                }
            }
            
        }
        public static async Task ClientViewBy10Page(IPage page)
        {
            await page.GetByRole(AriaRole.Textbox, new() { Name = "Search" }).FillAsync("test");
           await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
           await page.Locator("[id=\"users:title-top-toggle\"]").ClickAsync();

            var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Items per page" });
            await menu.WaitForAsync(new() { Timeout = 10_000 });
            await page.GetByRole(AriaRole.Menuitem, new() { Name = "50 per page" }).ClickAsync();

            var tbody = page.Locator("tbody");
            await tbody.WaitForAsync(new() { Timeout = 10_000 });

            Assert.That(
                await tbody.InnerTextAsync(),
                Does.Contain("test4gre+")
            );

        }

        /// <summary>
        /// Connecte un partenaire spécifique sur le portail (sans critères de recherche)
        /// </summary>
        public static async Task LogWithoutCriteria(IPage page, Partenaire partenaire, ReportBuilder report, string testName)
        {
            await LoginPortail(page, partenaire.Email, Serializer.mdp, report, testName);
        }

        #endregion

        #region Portail - Popups et Langue

        /// <summary>
        /// Ferme le popup d'information "Fermer" s'il apparaît sur le portail
        /// </summary>
        public static async Task PopUpPortal(IPage page)
        {
            try
            {
                var popUp = await page.GetByRole(AriaRole.Heading, new() { Name = "Information" }).AllAsync();
                if (popUp.Count > 0)
                {
                    var boutonFermer = page.GetByRole(AriaRole.Button, new() { Name = "Fermer" });
                    if (await boutonFermer.IsVisibleAsync())
                    {
                        await boutonFermer.ClickAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Popup handling: {ex.Message}");
            }
        }

        /// <summary>
        /// Sélectionne la langue sur le portail (FR ou DE).
        /// Gère le menu hamburger en mode mobile.
        /// </summary>
        public static async Task SelectLanguage(IPage page, bool isMobile, string langue)
        {
            if (isMobile)
            {
                await page.Locator("mygre-navigation").GetByRole(AriaRole.Button).ClickAsync();
            }

            switch (langue)
            {
                case "DE":
                    if (isMobile)
                    {
                        await page.GetByText("DE", new() { Exact = true }).ClickAsync();
                        await page.Locator("mygre-navigation").GetByRole(AriaRole.Button).ClickAsync();
                    }
                    else
                    {
                        await page.Locator("gfc-header").GetByText("DE").ClickAsync();
                    }
                    break;

                case "FR":
                    if (isMobile)
                    {
                        await page.GetByText("FR").ClickAsync();
                        await page.Locator("mygre-navigation").GetByRole(AriaRole.Button).ClickAsync();
                    }
                    else
                    {
                        await page.Locator("gfc-header").GetByText("FR").ClickAsync();
                    }
                    break;
            }
        }

        /// <summary>
        /// Sélectionne la langue sur le portail sans être connecté
        /// </summary>
        public static async Task SelectLanguageWithoutBeingConnected(IPage page, string langue)
        {
            await page.GetByText(langue, new() { Exact = true }).ClickAsync();
        }

        #endregion

        #region IVY - Authentification

        /// <summary>
        /// Connecte sur IVY et navigue vers le dashboard d'un partenaire
        /// </summary>
        public static async Task GoToDashboardIvy(IPage page, string langue, ReportBuilder report, string testName, Partenaire partenaire)
        {
            await LoginIvyQual(page, langue);
            await report.AddTestStep("Connexion", page, testName);
            await IvySearch(page, partenaire.NumeroPartenaire, langue);
            await report.AddTestStep("Partenaire selectionne", page, testName);
        }

        /// <summary>
        /// Login IVY via SSO (Windows/Entra), choix de langue et gestion des activités en cours
        /// </summary>
        public static async Task<int> LoginIvyQual(IPage page, string langue)
        {
            try
            {
                await IvyAuthentication.SignInAsync(page);

                PageOuvertureIvy ouverturePage = new PageOuvertureIvy(page);
                await ouverturePage.ChooseLanguageAsync(langue);
                await ouverturePage.ConfigureFinalAsync(langue);
                var interactionsFermees = await FermerToutesLesInteractionsOuvertes(page, langue);
                await FermerPagesSupplementaires(page);
                return interactionsFermees;
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("Execution context was destroyed"))
            {
                Console.WriteLine($"Contexte detruit pendant LoginIvyQual: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans LoginIvyQual: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region IVY - Navigation

        /// <summary>
        /// Recherche un partenaire dans IVY via la page de recherche
        /// </summary>
        public static async Task IvySearch(IPage page, int? numPartenaire, string langue)
        {
            PageSearchPartenaireIvy pageRecherche = new PageSearchPartenaireIvy(page);
            await pageRecherche.SearchByPartenaireAsync(numPartenaire, langue);
        }

        /// <summary>
        /// Recherche un partenaire dans IVY via les champs de formulaire
        /// </summary>
        public static async Task FindPartenaireIvy(IPage page, Partenaire partenaire, string langue)
        {
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("Npart", langue) })
                .FillAsync(partenaire.NumeroPartenaire.ToString());
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("search", langue) })
                .Last.ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("SelectionPerson", langue) })
                .ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("ChoosePartenaire", langue) })
                .ClickAsync();

            var popupPage = await page.WaitForPopupAsync();
            if (popupPage != null)
            {
                await popupPage.CloseAsync();
            }
        }

        /// <summary>
        /// Détecte la langue courante de l'interface IVY
        /// </summary>
        public static async Task<string> DetectLanguageAsync(IPage page)
        {
            var frParam = await page.GetByText("Paramètres utilisateur").AllAsync();
            var enParam = await page.GetByText("User Settings").AllAsync();
            var deParam = await page.GetByText("Benutzereinstellungen").AllAsync();

            if (deParam.Count > 0) return "DE";
            if (frParam.Count > 0) return "FR";
            if (enParam.Count > 0) return "EN";

            return "UNKNOWN";
        }

        #endregion

        #region IVY - Services

        /// <summary>
        /// Catégories de services disponibles dans IVY
        /// </summary>
        public enum Categories
        {
            Facturation,
            EmmDem,
            Destinataires,
            ProduitCompteursTarif,
            MutationsDonneesClients,
            ProprietairesGerances,
            InformationClients,
        }

        /// <summary>
        /// Services disponibles dans IVY
        /// </summary>
        public enum Services
        {
            creationDoubleCopieFacture,
            CreerUnBlocageDeRelance,
            CreerUnArrangementDePaiement,
            AnnulationArrangementDePaiement,
            AnnulerFacture,
            MutationDuPlanDePaiement,
            CreationDunReleveDeCompte,
            Emmenagement,
            Demenagement,
            ModifierEmmenagement,
            ModifierDemenagement,
            AnnulerEmmenagement,
            AnnulerDemenagement,
            GererDestinataireCorrespondance,
            GererBeneficiaire,
            GestionDesPayeurs,
            GererDestinataireFacture,
            MutationProduits,
            LectureReleveCompteur,
            MutationDesTraifs,
            GererLesCoodoneesBancaires,
            ChangementDeGerance,
            GererLeProprietaire,
            InformationDesClientsRapport
        }

        /// <summary>
        /// Sélectionne un service IVY par catégorie et type de service
        /// </summary>
        public static async Task SelectServiceIvy(IPage page, Categories categorie, Services service, string langue)
        {
            await page.GetByRole(AriaRole.Link, new() { Name = "Services " }).WaitForAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "Services " }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("NewService", langue) }).ClickAsync();

            string nomService = "";

            switch (categorie)
            {
                case Categories.Facturation:
                    
                    await page.GetByRole(AriaRole.Link, new() { Name = ResourceHelper.GetStringIvy("billing", langue) }).First.ClickAsync();
                    nomService = service switch
                    {
                        Services.creationDoubleCopieFacture => ResourceHelper.GetStringIvy("CopyBill", langue),
                        Services.CreerUnBlocageDeRelance => "Creer un blocage de relance",
                        Services.CreerUnArrangementDePaiement => "Creer un arrangement de paiement",
                        Services.AnnulationArrangementDePaiement => "Annulation arrangement de paiement",
                        Services.AnnulerFacture => "Annuler facture",
                        Services.MutationDuPlanDePaiement => "Mutation du plan de paiement",
                        Services.CreationDunReleveDeCompte => "Creation d'un releve de compte",
                        _ => ""
                    };
                    break;

                case Categories.EmmDem:
                    await page.GetByRole(AriaRole.Link, new() { Name = ResourceHelper.GetStringIvy("EmmDem", langue) }).ClickAsync();
                    nomService = service switch
                    {
                        Services.Emmenagement => ResourceHelper.GetStringIvy("Service_MoveIn", langue),
                        Services.Demenagement => ResourceHelper.GetStringIvy("Service_MoveOut", langue),
                        Services.ModifierEmmenagement => ResourceHelper.GetStringIvy("Service_ModifyMoveIn", langue),
                        Services.ModifierDemenagement => ResourceHelper.GetStringIvy("Service_ModifyMoveOut", langue),
                        Services.AnnulerEmmenagement => ResourceHelper.GetStringIvy("Service_CancelMoveIn", langue),
                        Services.AnnulerDemenagement => ResourceHelper.GetStringIvy("Service_CancelMoveOut", langue),
                        _ => ""
                    };
                    break;

                case Categories.Destinataires:
                    await page.GetByRole(AriaRole.Link, new() { Name = ResourceHelper.GetStringIvy("Recipients", langue) }).ClickAsync();
                    nomService = service switch
                    {
                        Services.GererDestinataireCorrespondance => ResourceHelper.GetStringIvy("AdminCorrespReci", langue),
                        Services.GererBeneficiaire => ResourceHelper.GetStringIvy("AdminPayee", langue),
                        Services.GestionDesPayeurs => ResourceHelper.GetStringIvy("AdminPayer", langue),
                        Services.GererDestinataireFacture => ResourceHelper.GetStringIvy("AdminBillToParty", langue),
                        _ => ""
                    };
                    break;

                case Categories.ProduitCompteursTarif:
                    await page.GetByRole(AriaRole.Link, new() { Name = ResourceHelper.GetStringIvy("ProductCptTarif", langue) }).ClickAsync();
                    nomService = service switch
                    {
                        Services.MutationProduits => ResourceHelper.GetStringIvy("MutationEnergieProduct", langue),
                        Services.LectureReleveCompteur => ResourceHelper.GetStringIvy("LectureCompteur", langue),
                        Services.MutationDesTraifs => ResourceHelper.GetStringIvy("MutationTarif", langue),
                        _ => ""
                    };
                    break;

                case Categories.MutationsDonneesClients:
                    await page.GetByRole(AriaRole.Link, new() { Name = ResourceHelper.GetStringIvy("MasterDataMutation", langue) }).ClickAsync();
                    nomService = ResourceHelper.GetStringIvy("MangeBankingDetails", langue);
                    break;

                case Categories.ProprietairesGerances:
                    await page.GetByRole(AriaRole.Link, new() { Name = "(2)" }).ClickAsync();
                    nomService = service switch
                    {
                        Services.ChangementDeGerance => ResourceHelper.GetStringIvy("Service_ChangementGerance", langue),
                        Services.GererLeProprietaire => ResourceHelper.GetStringIvy("administerOwner", langue),
                        _ => ""
                    };
                    break;

                case Categories.InformationClients:
                    await page.GetByRole(AriaRole.Link, new() { Name = "Information des clients / Rapports (1)" }).ClickAsync();
                    nomService = "Information des clients /";
                    break;
            }

            await page.GetByRole(AriaRole.Link, new() { Name = nomService }).First.ClickAsync();
        }

        /// <summary>
        /// Sélectionne un service IVY aléatoire (utile pour les tests de smoke)
        /// </summary>
        public static async Task SelectRandomServiceIvy(IPage page, string langue)
        {
            var random = new Random();

            var serviceParCategorie = new Dictionary<Categories, List<Services>>
            {
                { Categories.Facturation, new List<Services> {
                    Services.creationDoubleCopieFacture, Services.CreerUnBlocageDeRelance,
                    Services.CreerUnArrangementDePaiement, Services.AnnulationArrangementDePaiement,
                    Services.AnnulerFacture, Services.MutationDuPlanDePaiement, Services.CreationDunReleveDeCompte }},
                { Categories.EmmDem, new List<Services> {
                    Services.Emmenagement, Services.Demenagement,
                    Services.ModifierEmmenagement, Services.ModifierDemenagement,
                    Services.AnnulerEmmenagement, Services.AnnulerDemenagement }},
                { Categories.Destinataires, new List<Services> {
                    Services.GererDestinataireCorrespondance, Services.GererBeneficiaire,
                    Services.GestionDesPayeurs, Services.GererDestinataireFacture }},
                { Categories.ProduitCompteursTarif, new List<Services> {
                    Services.MutationProduits, Services.LectureReleveCompteur, Services.MutationDesTraifs }},
                { Categories.ProprietairesGerances, new List<Services> {
                    Services.ChangementDeGerance, Services.GererLeProprietaire }}
            };

            var categories = serviceParCategorie.Keys.ToList();
            var categorieChoisie = categories[random.Next(categories.Count)];
            var services = serviceParCategorie[categorieChoisie];
            var serviceChoisi = services[random.Next(services.Count)];

            await SelectServiceIvy(page, categorieChoisie, serviceChoisi, langue);
        }

        #endregion

        #region IVY - Gestion des activités et popups

        /// <summary>
        /// Reprend et termine toutes les interactions ouvertes, jusqu'à la limite de sécurité.
        /// </summary>
        public static async Task<int> FermerToutesLesInteractionsOuvertes(
            IPage page,
            string langue,
            int maximumInteractions = 50)
        {
            if (maximumInteractions <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumInteractions));

            var interactionsFermees = 0;
            while (interactionsFermees < maximumInteractions)
            {
                if (!await ResumeAndQuitOldActivity(page, langue))
                    return interactionsFermees;

                interactionsFermees++;
            }

            var boutonResume = page.GetByRole(
                AriaRole.Button,
                new() { Name = ResourceHelper.GetStringIvy("Resume", langue) });
            if (await boutonResume.IsVisibleAsync())
            {
                throw new InvalidOperationException(
                    $"Une interaction reste ouverte après la fermeture de {maximumInteractions} interactions.");
            }

            return interactionsFermees;
        }

        private static async Task<bool> ResumeAndQuitOldActivity(IPage page, string langue)
        {
            var boutonResume = page.GetByRole(
                AriaRole.Button,
                new() { Name = ResourceHelper.GetStringIvy("Resume", langue) });

            try
            {
                await boutonResume.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5000
                });
            }
            catch (TimeoutException)
            {
                return false;
            }

            var repriseCliquee = true;
            try
            {
                await boutonResume.ClickAsync();

                await page.GetByRole(
                    AriaRole.Button,
                    new() { Name = ResourceHelper.GetStringIvy("FinishInteraction", langue) })
                    .Last.WaitForAsync();
                await FermerPagesSupplementaires(page);

                var boutonTerminer = await page.GetByRole(
                    AriaRole.Button,
                    new() { Name = ResourceHelper.GetStringIvy("End", langue) }).AllAsync();
                if (boutonTerminer.Count > 0)
                {
                    await page.GetByRole(
                        AriaRole.Button,
                        new() { Name = ResourceHelper.GetStringIvy("End", langue) })
                        .Last.ClickAsync();
                }

                bool dialogConfirme = false;
                var overlayMask = page.Locator(".ui-widget-overlay.ui-dialog-mask").First;
                try
                {
                    await overlayMask.WaitForAsync(new() { Timeout = 5000, State = WaitForSelectorState.Visible });

                    var ouiButton = page.Locator(".ui-dialog:visible")
                        .GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Yes", langue) })
                        .First;
                    await ouiButton.ClickAsync();
                    dialogConfirme = true;

                    await overlayMask.WaitForAsync(new() { Timeout = 5000, State = WaitForSelectorState.Hidden });
                }
                catch (TimeoutException)
                {
                    dialogConfirme = false;
                }

                if (!dialogConfirme)
                {
                    await page.GetByRole(
                        AriaRole.Button,
                        new() { Name = ResourceHelper.GetStringIvy("FinishInteraction", langue) })
                        .Last.ClickAsync();
                }
                await FermerPagesSupplementaires(page);

                await page.WaitForTimeoutAsync(2000);
                await FermerPagesSupplementaires(page);

                await ValiderBoutonsSafely(page, langue);
                return true;
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("Execution context was destroyed"))
            {
                Console.WriteLine("Contexte detruit pendant ResumeAndQuitOldActivity");
                return repriseCliquee;
            }
        }

        /// <summary>
        /// Gère les boutons de validation IVY (Oui/Non) avec gestion d'erreurs
        /// </summary>
        private static async Task ValiderBoutonsSafely(IPage page, string langue)
        {
            var etapesValidation = new[]
            {
                new { Selector = ResourceHelper.GetStringIvy("Yes", langue), Timeout = 3500, IsRole = true },
                new { Selector = ResourceHelper.GetStringIvy("Yes", langue), Timeout = 1500, IsRole = true },
                new { Selector = "button[name=\"j_id_2i3\"]", Timeout = 1000, IsRole = false }
            };

            foreach (var etape in etapesValidation)
            {
                try
                {
                    if (page.IsClosed) break;

                    ILocator locator = etape.IsRole
                        ? page.GetByRole(AriaRole.Button, new() { Name = etape.Selector })
                        : page.Locator(etape.Selector);

                    bool existe = false;
                    try
                    {
                        await locator.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 1000 });
                        existe = true;
                    }
                    catch (TimeoutException) { }

                    if (existe)
                    {
                        await locator.ClickAsync();
                    }

                    if (etape.Timeout > 0)
                    {
                        await page.WaitForTimeoutAsync(etape.Timeout);
                    }
                }
                catch (PlaywrightException ex) when (
                    ex.Message.Contains("Execution context was destroyed") ||
                    ex.Message.Contains("Target page, context or browser has been closed"))
                {
                    Console.WriteLine($"Contexte detruit pendant validation: {etape.Selector}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur validation {etape.Selector}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Ferme les pages/onglets supplémentaires (popups IVY)
        /// </summary>
        public static async Task FermerPagesSupplementaires(IPage page)
        {
            try
            {
                if (page.IsClosed) return;

                var toutesLesPages = page.Context.Pages;
                if (toutesLesPages.Count > 1)
                {
                    var pagesAFermer = toutesLesPages.Skip(1).ToList();
                    foreach (var pageAFermer in pagesAFermer)
                    {
                        try
                        {
                            if (!pageAFermer.IsClosed)
                            {
                                await pageAFermer.CloseAsync();
                            }
                        }
                        catch (PlaywrightException ex)
                        {
                            Console.WriteLine($"Erreur fermeture page: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans FermerPagesSupplementaires: {ex.Message}");
            }
        }

        #endregion

        #region Suivi des étapes (annotations d'actions Playwright)

        /// <summary>
        /// Active le bandeau OFFICIEL Playwright de suivi des actions
        /// (<c>Screencast.ShowActions</c>, dispo dès Playwright 1.59) : le titre de chaque
        /// action et l'élément ciblé sont annotés directement sur la page pendant l'exécution.
        /// Ignoré en headless / CI (rien à afficher) et tolérant si l'API n'est pas dispo.
        /// </summary>
        public static async Task ActiverSuiviEtapes(IPage page)
        {
            if (TestConfig.IsHeadless) return;
            try
            {
                await page.Screencast.ShowActionsAsync(new ScreencastShowActionsOptions
                {
                    Position = AnnotatePosition.TopRight,
                    FontSize = 18,
                    Duration = 1500
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[info] Suivi des etapes Playwright indisponible: {ex.Message}");
            }
        }

        #endregion

        #region Attentes

        /// <summary>
        /// Durées d'attente nommées pour la lisibilité
        /// </summary>
        public static class Wait
        {
            public const int Quick = 500;
            public const int Normal = 3500;
            public const int PageLoad = 5000;
            public const int ApiCall = 7000;
            public const int Heavy = 10000;

            public static async Task ForElement(IPage page, string selector, int timeout = Normal)
            {
                await page.WaitForSelectorAsync(selector, new() { Timeout = timeout });
            }

            public static async Task ForNavigation(IPage page)
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            public static async Task QuickWait(IPage page) => await page.WaitForTimeoutAsync(Quick);
            public static async Task NormalWait(IPage page) => await page.WaitForTimeoutAsync(Normal);
            public static async Task LongWait(IPage page) => await page.WaitForTimeoutAsync(Heavy);
        }

        #endregion
    }
}
