using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    /// <summary>
    /// Tests fonctionnels IVY (ivy-qua.cce24x7.net).
    /// Organise par domaine : Bills, Moving, Partenaire, Consumption, Gerance.
    /// Chaque test utilise TestCaseSource pour les combinaisons de langues (FR/DE/EN).
    /// Note : les Page Objects IVY seront ajoutes ulterieurement avec l'inspecteur Playwright.
    /// </summary>
    [TestFixture]
    [Category("IVY_Fonctionnel")]
    public class Ivy_Fonctionnel : TestBaseIvy
    {
        #region Criteres de selection

        /// <summary>Aucun critere specifique</summary>
        private static readonly Dictionary<string, string?> CriteresAucun = new();

        #endregion

        // ========================================================================
        //  BILLS - Factures, IBAN, destinataires
        // ========================================================================

        #region Bills - Copie de facture

        /// <summary>
        /// Copie une facture existante et envoie par email.
        /// Necessite : numero de facture, compte contrat, lieu de consommation.
        /// </summary>
        [Test]
        [Category("LEGACY_EXTERNAL_EFFECT")]
        [Explicit("Crée et envoie une copie de facture par e-mail.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task CopieFacture(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Copie de facture";
            descriptionTest = "Copie d'une facture existante et envoi par email";
            expectedResult = "Le document est copie avec succes";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche partenaire ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            LogGitHubActions(testName, p);

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Selection du service ---
            await SelectionnerService(TestTools.Categories.Facturation, TestTools.Services.creationDoubleCopieFacture);
            await Report.AddTestStep("Service selectionne", page!, testName);

            // --- Recherche par compte contrat ---
            await page!.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("search2", lg) })
                .FillAsync(p.CompteContrat.ToString());
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("search2", lg) })
                .ClickAsync();
            await page.WaitForTimeoutAsync(4500);
            await Report.AddTestStep("Recherche compte de contrat", page, testName);

            // --- Selection du lieu de consommation ---
            if (p.LieuDeConsommation == null)
            {
                Assert.Fail("Donnee de LC manquante");
            }
            //await page.GetByRole(AriaRole.Gridcell, new() { Name = p.LieuDeConsommation.ToString() }).Last.ClickAsync();
            await page.Locator("tr").Filter(new() { Has = page.Locator("i.fa-check-circle") })
                       .Locator(".ui-radiobutton-box")
                       .ClickAsync();

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).Last.ClickAsync();
            await page.WaitForTimeoutAsync(4500);

            // --- Selection facture aleatoire ---
            var factures = await page.GetByRole(AriaRole.Gridcell).AllAsync();
            if (factures.Count == 0)
            {
                Report.SetTestResult(false, "Aucune facture trouvee");
                Assert.Fail("Aucune facture trouvee");
            }

            var random = new Random();
            await factures[random.Next(0, factures.Count)].ClickAsync();
            await Report.AddTestStep("Selection facture aleatoire", page, testName);

            Console.WriteLine($"Nombre de factures trouvees: {factures.Count}");
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).Last.ClickAsync();

            // --- Envoi par email ---
            await page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringIvy("SendConfirm", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = ResourceHelper.GetStringIvy("EmailIndivi", lg) }).ClickAsync();
            await page.WaitForTimeoutAsync(2500);

            var champEmail = page.Locator("[id=\"serviceFormId:template:j_id_jo\"]");
            
            await champEmail.ClickAsync();
            await champEmail.TypeAsync($"test4gre+{p.NumeroPartenaire}@gmail.com");
            await Report.AddTestStep("Envoi mail au compte gmail de test", page, testName);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Complete", lg) }).ClickAsync();

            // --- Verification erreur ---
            var errorContainer = page.Locator("[id=\"serviceFormId\\:tabContainerComponent\\:growlMessage_container\"]");
            var errorItem = errorContainer.Locator(".ui-growl-item");
            await page.WaitForTimeoutAsync(2000);

            if (await errorItem.IsVisibleAsync())
            {
                var errorText = await errorItem.TextContentAsync();
                Console.WriteLine($"Erreur detectee: {errorText}");
                Report.SetTestResult(false, "Le document joint n'a pas pu etre cree.");
                LogGitHubActionsError(testName, "Le document joint n'a pas pu etre cree.");
            }
            else
            {
                Console.WriteLine("Operation reussie, pas d'erreur visible");
                Report.SetTestResult(true, "Le document a pu etre copie");
                LogGitHubActionsSuccess(testName);
            }
        }

        #endregion

        #region Bills - Changement donnees bancaires

        /// <summary>
        /// Modifie l'IBAN du partenaire (rotation entre 2 IBANs valides).
        /// Verifie que le nouvel IBAN est bien enregistre.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Category("LEGACY_FINANCIAL")]
        [Explicit("Modifie durablement l'IBAN du partenaire.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task ChangementDonneesBancaires(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Changement des donnees bancaires";
            descriptionTest = "Modification de l'IBAN du partenaire";
            expectedResult = "Le nouvel IBAN est enregistre correctement";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            LogGitHubActions(testName, p);

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Service : gerer coordonnees bancaires ---
            await SelectionnerService(TestTools.Categories.MutationsDonneesClients, TestTools.Services.GererLesCoodoneesBancaires);
            await Report.AddTestStep("Selection service", page!, testName);

            // --- Recuperer l'ancien IBAN ---
            await page!.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("EditBankAccount", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("NumIban", lg) }).WaitForAsync();
            string ancienIban = await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("NumIban", lg) }).InputValueAsync();
            await Report.AddTestStep("Recuperation de l'ancien IBAN", page, testName);

            // --- Generer le nouvel IBAN (rotation) ---
            string nouvelIban = (ancienIban == "CH7700767000R09014621")
                ? "CH8700768015567131006"
                : "CH7700767000R09014621";

            // --- Saisir le nouvel IBAN ---
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("NumIban", lg) }).FillAsync(nouvelIban);
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("AccountHolder", lg) }).FillAsync($"{p.Prenom} {p.Nom}");
            await Report.AddTestStep("Saisie du nouveau numero IBAN", page, testName);

            // --- Sauvegarder et completer ---
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("save", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Complete", lg) }).ClickAsync();

            // --- Verification : re-ouvrir le service et verifier ---
            await SelectionnerService(TestTools.Categories.MutationsDonneesClients, TestTools.Services.GererLesCoodoneesBancaires);
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("EditBankAccount", lg) }).ClickAsync();

            string ibanVerification = await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("NumIban", lg) }).InputValueAsync();
            Assert.That(ibanVerification, Is.EqualTo(nouvelIban), "L'IBAN n'a pas ete modifie correctement");

            await Report.AddTestStep($"Validation ancien {ancienIban} - nouveau: {nouvelIban}", page, testName);
            LogGitHubActionsSuccess(testName);
        }

        #endregion

        #region Bills - Gestion destinataire facture

        /// <summary>
        /// Change le destinataire de facture pour un lieu de consommation.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Explicit("Modifie le destinataire de facturation avec des identifiants de données codés en dur.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task GestionDestinataireFacture(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Changement destinataire de facture";
            descriptionTest = "Changement du destinataire de facture";
            expectedResult = "Le destinataire est modifie";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Service ---
            await SelectionnerService(TestTools.Categories.Destinataires, TestTools.Services.GererDestinataireFacture);
            await Report.AddTestStep("Selection service", page!, testName);

            // --- Selectionner le lieu de consommation ---
            await page!.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("numCompteCompteur", lg) })
                .First.FillAsync(p.numeroCompteur.ToString());
            await page.GetByText(p.LieuDeConsommation).First.ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).Last.ClickAsync();

           // await page.PauseAsync();
            // --- Selectionner le type de facture ---
            var locator = page.Locator("[id=\"serviceFormId\\:j_id_jq\"] span").Nth(1);

            if (await locator.CountAsync() > 0)
            {
                await locator.ClickAsync();

                var option = page.GetByRole(AriaRole.Option, new() 
                { 
                    Name = "Facture + détail installation" 
                });

                if (await option.CountAsync() > 0)
                {
                    await option.ClickAsync();
                }
            }

            // --- Rechercher et selectionner le destinataire ---
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("search2", lg) }).FillAsync("6027826");
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("search", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Tab, new() { Name = "(1)" }).ClickAsync();
            await page.GetByRole(AriaRole.Gridcell, new() { Name = "6027826", Exact = true }).ClickAsync();

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Complete", lg) }).ClickAsync();

            await Report.AddTestStep("Destinataire modifie", page, testName);
        }

        #endregion

        // ========================================================================
        //  MOVING - Emmenagement, demenagement, annulation, modification
        // ========================================================================

        #region Moving - Emmenagement

        /// <summary>
        /// Cree un emmenagement : adresse, date, tarif, confirmation.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Explicit("Crée un emménagement dans les données métier IVY.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task Emmenagement(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Emmenagement";
            descriptionTest = "Creation d'un emmenagement dans IVY";
            expectedResult = "L'emmenagement est cree";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);
            

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Service emmenagement ---
            await SelectionnerService(TestTools.Categories.EmmDem, TestTools.Services.Emmenagement);
            await Report.AddTestStep("Selection service", page!, testName);

            // --- Nouveau lieu de consommation ---
            await page!.Locator(".ui-radiobutton-box").First.ClickAsync();
            await Report.AddTestStep("Nouvel LC", page, testName);
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).Last.ClickAsync();

            // --- Recherche adresse (depuis le CSV : rue + numero du partenaire) ---
            if (string.IsNullOrWhiteSpace(p.Rue))
            {
                Assert.Fail($"Le partenaire {p.NumeroPartenaire} n'a pas de rue dans le CSV (champ Rue requis pour l'emmenagement).");
            }
            string adresseEmmenagement = $"{p.Rue} {p.NumRue}".Trim();
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("search2", lg) })
                .FillAsync(adresseEmmenagement);
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("search2", lg) })
                .PressAsync("Enter");
            await Report.AddTestStep($"Adresse recherchee : {adresseEmmenagement}", page, testName);


            /// --- Selection du compteur --- -> Changer le Nth(4)
            //await page.Locator("td").Nth(4).ClickAsync();
            ///await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            // --- Selection du 1er LC disponible ---
            // On prend la 1re ligne reellement selectionnable : celle dont la checkbox
            // est active (= sans l'icone info-circle "emplacement non disponible" dans la
            // 1re colonne). NB : dans ce tableau les lignes a pastille verte ont deja un
            // futur emmenagement, donc leur checkbox est desactivee ; ce sont les lignes
            // sans le message (checkbox active) qui sont disponibles pour l'emmenagement.
            var row = page.Locator("#serviceFormId\\:premiseTable\\:premiseTable_data tr")
                .Filter(new() { Has = page.Locator("div[role='checkbox']:not(.ui-state-disabled)") })
                .First;

            // Sanity check : au moins un LC selectionnable doit exister
            await Assertions.Expect(row).ToBeVisibleAsync();

            // Checkbox = premiere colonne
            await row.Locator("div[role='checkbox']:not(.ui-state-disabled)").ClickAsync();
            await Report.AddTestStep("1er LC disponible selectionne", page, testName);

            

            // --- Date d'emmenagement ---
            string dateMoveIn = DateTime.Now.AddMonths(1).ToString("dd.MM.yy");
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("Service_DateMoveIn", lg) })
                .FillAsync(dateMoveIn);
            await Report.AddTestStep("Date auj + 1 mois", page, testName);
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();

            // --- Selection tarif BASIC ---
            await page.Locator("[id=\"serviceFormId\\:energyProduct\\:0\\:product_label\"]").ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = "BASIC tarif unitaire" }).ClickAsync();
            await Report.AddTestStep("Selection Tarif", page, testName);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();

            // --- Confirmation sans envoi de mail ---
            await page.Locator("[id=\"serviceFormId\\:template\\:templateDropdown\"] span").Nth(1).ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = ResourceHelper.GetStringIvy("DontSend", lg) }).ClickAsync();
            await page.Locator(".ui-radiobutton-box").Last.ClickAsync();
            await Report.AddTestStep("Confirmation", page, testName);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Complete", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("no", lg) }).ClickAsync();
        }

        #endregion

        #region Moving - Annuler emmenagement

        /// <summary>
        /// Annule un emmenagement existant.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Explicit("Annule un emménagement existant dans les données métier IVY.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task AnnulerEmmenagement(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Annuler emmenagement";
            descriptionTest = "Annulation d'un emmenagement";
            expectedResult = "L'emmenagement est annule";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Service annuler emmenagement ---
            await SelectionnerService(TestTools.Categories.EmmDem, TestTools.Services.AnnulerEmmenagement);
            await Report.AddTestStep("Selection service", page!, testName);

            // --- Selectionner l'emmenagement a annuler ---
            await page!.Locator("td").First.ClickAsync();
            await Report.AddTestStep("Selectionner le demenagement", page, testName);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            await Report.AddTestStep("Pas de facture normalement", page, testName);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            await Report.AddTestStep("Validation", page, testName);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Complete", lg) }).ClickAsync();
            await Report.AddTestStep("Final", page, testName);
        }

        #endregion

        #region Moving - Modifier emmenagement (TODO)

        /// <summary>
        /// Modifie la date d'un emmenagement existant.
        /// TODO : a completer avec les verifications.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Category("LEGACY_A_REPRENDRE")]
        [Explicit("Modifie un emménagement existant; assertions finales manquantes.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task ModifierEmmenagement(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Modifier emmenagement";
            descriptionTest = "Modification d'un emmenagement";
            expectedResult = "La date est modifiee";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Service modifier emmenagement ---
            await SelectionnerService(TestTools.Categories.EmmDem, TestTools.Services.ModifierEmmenagement);

            // --- Selectionner et modifier la date ---
            await page!.Locator("td").First.ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();

            string nouvelleDate = DateTime.Now.AddMonths(2).ToString("dd.MM.yy");
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("Service_DateMoveIn", lg) })
                .FillAsync(nouvelleDate);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Complete", lg) }).ClickAsync();

            await DebugPause();
        }

        #endregion

        // ========================================================================
        //  PARTENAIRE - Details, creation, historique, telephone
        // ========================================================================

        #region Partenaire - Changement details

        /// <summary>
        /// Modifie le numero de telephone et l'email du partenaire.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Explicit("Modifie durablement le téléphone du partenaire.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task ChangerDetailsPartenaire(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Changement des informations du partenaire";
            descriptionTest = "Verifier que les informations du partenaire peuvent etre modifiees";
            expectedResult = "Les donnees du partenaire sont modifiees";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            LogGitHubActions(testName, p);

            var random = new Random();
            int nouveauNumero = random.Next(100000000, 999999999);

            await LoginIvy();
            await TestTools.FindPartenaireIvy(page!, p, lg);
            await Report!.AddTestStep("Selection partenaire", page!, testName);

            // --- Ouvrir l'edition ---
            await page!.Locator("button.ui-button-icon-only:has(.fa-pencil)").Last.ClickAsync();

            // --- Verifier et modifier ---
            string valeurActuelle = await page.GetByRole(AriaRole.Textbox,
                new() { Name = ResourceHelper.GetStringIvy("natel", lg), Exact = true }).InputValueAsync();
            Assert.That(nouveauNumero.ToString(), Is.Not.EqualTo(valeurActuelle));

            await page.GetByRole(AriaRole.Textbox,
                new() { Name = ResourceHelper.GetStringIvy("natel", lg), Exact = true }).FillAsync(nouveauNumero.ToString());
            await page.GetByRole(AriaRole.Textbox, new() { Name = "E-Mail" }).FillAsync(p.Email);
            await Report.AddTestStep("Changement numero", page, testName);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("save", lg) }).ClickAsync();
            await Report.AddTestStep("Nouveau numero", page, testName);
            LogGitHubActionsSuccess(testName);
        }

        #endregion

        #region Partenaire - Changement tarif

        /// <summary>
        /// Modifie le numero de telephone et l'email du partenaire.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Category("LEGACY_FINANCIAL")]
        [Category("LEGACY_A_REPRENDRE")]
        [Explicit("Parcours de changement de tarif incomplet, sans vérification finale.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task ChangerTarif(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Changement du tarif du partenaire";
            descriptionTest = "Verifier que le tarif du partenaire peut etre modifie";
            expectedResult = "Les donnees du partenaire sont modifiees";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            LogGitHubActions(testName, p);

            var random = new Random();

            await LoginIvy();
            await TestTools.FindPartenaireIvy(page!, p, lg);
            await Report!.AddTestStep("Selection partenaire", page!, testName);

            await TestTools.SelectServiceIvy(page!, TestTools.Categories.ProduitCompteursTarif, TestTools.Services.MutationDesTraifs, lg);
            await Report.AddTestStep("Service Mutation des tarifs", page!, testName);

            // --- Selection du 1er contrat actif (icone verte fa-check-circle en derniere colonne) ---
            var lignesContrats = page!.Locator("[id$=':contractAccountsTable_data'] tr");
            var contratActif = lignesContrats
                .Filter(new() { Has = page.Locator("i.fa-check-circle") })
                .First;

            // Sanity check : au moins un contrat actif doit exister
            await Assertions.Expect(contratActif).ToBeVisibleAsync();

            await contratActif.Locator(".ui-radiobutton-box").ClickAsync();
            await Report.AddTestStep("1er contrat actif selectionne", page, testName);
            
             await page.GetByRole(AriaRole.Button, new() { Name = " Continuer" }).ClickAsync();

            string dateAujourdhui = DateTime.Now.ToString("dd.MM.yyyy");
            await page.Locator("[id$=':changeDate_input']").FillAsync(dateAujourdhui);
            
            await page.GetByRole(AriaRole.Button, new() { Name = " Continuer" }).ClickAsync();

        }

        #endregion

        #region Partenaire - Changement numero telephone

        /// <summary>
        /// Modifie le numero de telephone via le composant Header et verifie
        /// que la valeur est bien persistee dans IVY ET dans le CSV.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Explicit("Modifie le téléphone dans IVY et écrit aussi dans le CSV local.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task ChangementNumeroTel(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Changement numero de telephone";
            descriptionTest = "Modification du telephone via le composant Header et sync CSV";
            expectedResult = "Le nouveau numero est enregistre dans IVY et persiste dans le CSV";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            LogGitHubActions(testName, p);

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Generer un nouveau numero aleatoire ---
            var random = new Random();
            string nouveauTelephone = $"079 {random.Next(100, 999)} {random.Next(10, 99)} {random.Next(10, 99)}";

            // --- Modification via le composant (IVY + CSV) ---
            var header = new ComponentsHeaderDashIvy(page!);
            await header.ChangerTelephoneAsync(p!, nouveauTelephone);
            await Report.AddTestStep($"Telephone change: {nouveauTelephone}", page!, testName);

            // --- Verification dans IVY : re-ouvrir le dialog et lire la valeur ---
            await header.OuvrirDialogPartenaireAsync();
            await header.VerifierTelephoneDansDialogAsync(nouveauTelephone);
            await Report.AddTestStep("Valeur persistee dans IVY", page!, testName);
            await header.FermerDialogAsync();

            // --- Verification dans le CSV : relecture depuis le disque ---
            var pRelu = DataLoader.GetPartenaires(Serializer.PathToPartenaireCsv)
                .FirstOrDefault(x => x.NumeroPartenaire == p!.NumeroPartenaire);
            Assert.That(pRelu?.Telephone, Is.EqualTo(nouveauTelephone),
                "Le numero de telephone doit etre persiste dans le CSV");
            await Report.AddTestStep("Valeur persistee dans le CSV", page!, testName);

            LogGitHubActionsSuccess(testName);
        }

        #endregion

        #region Partenaire - Creation

        /// <summary>
        /// Cree un nouveau partenaire avec des donnees aleatoires.
        /// Gere les dialogues de validation (Apply, CreateAnyway).
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Explicit("Crée un partenaire dans IVY avec des données de test codées en dur.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task CreerPartenaire(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Creation de partenaire";
            descriptionTest = "Creation d'un nouveau partenaire";
            expectedResult = "Un partenaire est cree";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            LogGitHubActions(testName, p);

            await LoginIvy();
            await TestTools.FindPartenaireIvy(page!, p, lg);

            // --- Ouvrir la creation ---
            await page!.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("ChangePartenaire", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("newPartner", lg) }).ClickAsync();
            await Report!.AddTestStep("Ouverture de la creation de partenaire", page, testName);

            // --- Remplir le formulaire ---
            Random random = new Random();

            // Prenom et nom
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("Prenom", lg) }).First.FillAsync("test");
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("Nom", lg) }).Last.FillAsync("testing");

            // Titre (selection aleatoire)
            string[] titres = { "Monsieur", "Madame", "Famille", "Monsieur et Madame" };
            await page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringIvy("Title", lg) }).ClickAsync();
            string titreChoisi = titres[random.Next(titres.Length)];
            await page.Locator($"li.ui-autocomplete-item[data-item-label='{titreChoisi}']").ClickAsync();

            // Langue
            string[] languesPartenaire = { "Allemand", "Français" };
            await page.Locator("[id=\"businessPartnerFormId\\:language_button\"]").ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = languesPartenaire[random.Next(languesPartenaire.Length)] }).ClickAsync();

            // Type
            await page.Locator("[id=\"businessPartnerFormId\\:partnerVariety_button\"]").ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = "Standard" }).ClickAsync();

            // Adresse
            await page.Locator("[id=\"businessPartnerFormId\\:streetNr\"]").FillAsync("fert");
            await page.Locator("[id=\"businessPartnerFormId\\:nr\"]").FillAsync("45");
            await page.Locator("[id=\"businessPartnerFormId\\:zip\"]").FillAsync("1232");
            await page.Locator("[id=\"businessPartnerFormId\\:city\"]").FillAsync("fgdfgd");

            // Canton
            string[] cantons = { "Berne", "Genève", "Fribourg" };
            await page.Locator("[id=\"businessPartnerFormId\\:region_button\"]").ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = cantons[random.Next(cantons.Length)] }).ClickAsync();
            await Report.AddTestStep("Remplissage du formulaire", page, testName);

            // --- Sauvegarder et gerer les dialogues ---
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("save", lg) }).ClickAsync();

            await page.WaitForTimeoutAsync(3800);
            var boutonAppliquer = page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Apply", lg) });
            if (await boutonAppliquer.CountAsync() > 0) await boutonAppliquer.ClickAsync();

            await page.WaitForTimeoutAsync(3800);
            var boutonCreerQuandMeme = page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("CreateAnyway", lg) });
            if (await boutonCreerQuandMeme.CountAsync() > 0) await boutonCreerQuandMeme.ClickAsync();

            await page.WaitForTimeoutAsync(3800);
            await Report.AddTestStep("Fin", page, testName);
            LogGitHubActionsSuccess(testName);
        }

        #endregion

        #region Partenaire - Historique

        /// <summary>
        /// Consulte l'historique du partenaire (simple, avance, archive).
        /// Utilise le composant ComponentsHeaderDashIvy pour ouvrir le popup.
        /// </summary>
        [Test]
        [Category("LEGACY_READ_ONLY")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task HistoriquePartenaire(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Historique partenaire";
            descriptionTest = "Consultation de l'historique du partenaire";
            expectedResult = "L'historique est accessible et contient des donnees";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            LogGitHubActions(testName, p);

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Ouvrir l'historique via le composant Header ---
            var header = new ComponentsHeaderDashIvy(page!);
            var popupHistorique = await header.OuvrirHistoriqueAsync(lg);

            // --- Verifier le contenu ---
            var textes = await popupHistorique.Locator("#j_id_p\\:historyViewFormId\\:j_id_10_data tr td:first-child").AllTextContentsAsync();
            if (textes.Count == 0)
            {
                LogGitHubActionsError(testName, "Pas d'historique");
                Assert.Fail("Pas d'historique");
            }

            await Report.AddTestStep("Page historique", page!, testName);

            // Historique simple
            await popupHistorique.GetByText(ResourceHelper.GetStringIvy("Simplehistory", lg)).ClickAsync();
            await page!.WaitForTimeoutAsync(1500);
            await Report.AddTestStep("Simple history", page, testName);

            // Historique avance
            await popupHistorique.GetByText(ResourceHelper.GetStringIvy("AdvancedHistory", lg)).ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            await Report.AddTestStep("Advanced history", page, testName);

            // Archive
            await popupHistorique.Locator("[id=\"j_id_l\\:toggleSwitch\"] div").Nth(1).ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            await Report.AddTestStep("Archive", page, testName);

            LogGitHubActionsSuccess(testName);
        }

        #endregion

        // ========================================================================
        //  CONSUMPTION - Releves de compteur
        // ========================================================================

        #region Consumption - Releve de compteur

        /// <summary>
        /// Saisit des releves de compteur pour 3 types (02, 09, controle).
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Category("LEGACY_FINANCIAL")]
        [Explicit("Saisit des relevés de compteur pouvant influencer la facturation.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task SaisieReleveCompteur(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Lecture releve de compteur";
            descriptionTest = "Saisie de releves de compteur";
            expectedResult = "Les releves sont saisis";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Service releve compteur ---
            await SelectionnerService(TestTools.Categories.ProduitCompteursTarif, TestTools.Services.LectureReleveCompteur);
            await Report.AddTestStep("Selection service", page!, testName);

            // --- Selectionner le compteur ---
            await page!.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("numCompteCompteur", lg) })
                .First.FillAsync(p.numeroCompteur.ToString());
            await page.GetByText(p.LieuDeConsommation).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).Last.ClickAsync();

            await page.WaitForTimeoutAsync(3500);
            await page.Locator(".ui-radiobutton-icon").Nth(0).WaitForAsync();
            await page.Locator(".ui-radiobutton-icon").Nth(0).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).Last.ClickAsync();

            // --- Saisie pour chaque type de releve ---
            // Type 02
            await page.Locator("[id=\"serviceFormId\\:createMeterReadingCompId\\:type\"] span").Nth(1).ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = "02 - " }).ClickAsync();
            await SaisirDateReleve(lg);

            // Type 09
            await page.Locator("[id=\"serviceFormId\\:createMeterReadingCompId\\:type\"] span").Nth(1).ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = "09 -" }).ClickAsync();
            await SaisirDateReleve(lg);

            // Releve de controle
            await page.Locator("[id=\"serviceFormId\\:createMeterReadingCompId\\:type\"] span").Nth(1).ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = "- Relevé de contrôle" }).ClickAsync();
            await SaisirDateReleve(lg);

            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Complete", lg) }).ClickAsync();
        }

        /// <summary>Saisit une date aleatoire dans le mois courant pour un releve</summary>
        private async Task SaisirDateReleve(string lg)
        {
            var now = DateTime.Now;
            var jourMax = DateTime.DaysInMonth(now.Year, now.Month);
            var jourAleatoire = new Random().Next(1, jourMax + 1);

            await page!.GetByRole(AriaRole.Textbox, new() { Name = "Choose a date" })
                .FillAsync($"{jourAleatoire:D2}-{now.Month:D2}-{now.Year}");
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Create", lg) }).ClickAsync();
            await page.WaitForTimeoutAsync(1500);
        }

        #endregion

        // ========================================================================
        //  GERANCE - Changement de gerance
        // ========================================================================

        #region Gerance - Changement de gerance

        /// <summary>
        /// Change la gerance pour un lieu de consommation.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Explicit("Modifie la gérance du lieu de consommation avec un identifiant codé en dur.")]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task ChangementGerance(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Changer gerance";
            descriptionTest = "Changement de la gerance";
            expectedResult = "La gerance est modifiee";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Login et recherche ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;

            await LoginIvy();
            await Report!.AddTestStep("Connexion", page!, testName);

            await RechercherPartenaire(p);
            await Report.AddTestStep("Partenaire selectionne", page!, testName);

            // --- Service changement de gerance ---
            await SelectionnerService(TestTools.Categories.ProprietairesGerances, TestTools.Services.ChangementDeGerance);

            // --- Remplir LC et compte contrat ---
            await page!.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("numLc", lg) })
                .FillAsync(p.LieuDeConsommation);
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("numCompteCompteur", lg) })
                .FillAsync(p.CompteContrat.ToString());
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("numCompteCompteur", lg) })
                .PressAsync("Enter");

            await page.GetByRole(AriaRole.Gridcell, new() { Name = p.LieuDeConsommation }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();

            // --- Rechercher le nouveau gerant ---
            await page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("search2", lg) }).FillAsync("193049");
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("search", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Tab, new() { Name = ResourceHelper.GetStringIvy("Person", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Gridcell, new() { Name = "193049" }).WaitForAsync();
            await page.GetByRole(AriaRole.Gridcell, new() { Name = "193049" }).ClickAsync();

            // --- Completer ---
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("continue", lg) }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("Complete", lg) }).ClickAsync();

            await Report.AddTestStep("Gerance modifiee", page, testName);
        }

        #endregion
    }
}
