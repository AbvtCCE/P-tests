using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.Pages.Portail;

namespace TestPlayrights
{
    /// <summary>
    /// Tests fonctionnels du Portail (my-q.groupe-e.ch).
    /// Organise par domaine : Bills, User, Moving.
    /// Chaque test utilise TestCaseSource pour les combinaisons navigateur x mobile x langue.
    /// </summary>
    [TestFixture]
    [Category("Portail_Fonctionnel")]
    public class Portail_Fonctionnel : TestBasePortail
    {
        #region Criteres de selection

        /// <summary>Partenaire inscrit sur le portail</summary>
        private static readonly Dictionary<string, string?> CriteresInscrit = new()
        {
            { "InscritSurLePortail", "true" }
        };

        /// <summary>Partenaire inscrit avec facture ouverte</summary>
        private static readonly Dictionary<string, string?> CriteresInscritAvecFacture = new()
        {
            { "InscritSurLePortail", "true" },
            { "FactureOuverte", "true" }
        };

        /// <summary>Aucun critere (pour inscription)</summary>
        private static readonly Dictionary<string, string?> CriteresAucun = new();

        #endregion

        // ========================================================================
        //  BILLS - Factures, plans de paiement, telechargements
        // ========================================================================

        #region Bills - Plan de paiement

        /// <summary>
        /// Cree un plan de paiement sur 6 mois.
        /// Criteres : inscrit + facture ouverte.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Category("LEGACY_FINANCIAL")]
        [Explicit("Crée un plan de paiement sur un compte métier.")]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task PlanDePaiement(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Plan de paiement";
            descriptionTest = "Un plan de paiement peut etre cree";
            expectedResult = "Le plan de paiement se cree";
            criteresSelection = new(CriteresInscritAvecFacture);
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LoginAvecCriteres();
            var p = partenaireSelectionne;
            await SelectionnerLangue();
            Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | CC: {p.CompteContrat}");

            // --- Plan de paiement via Page Object ---
            var paymentPage = new PaymentPlanPage(page!, lg);
            await paymentPage.CreerPlanDePaiement(p.CompteContrat.ToString());

            await Report!.AddTestStep("Plan de paiement cree", page!, testName);
            Report.SetTestResult(true, "Le plan de paiement a ete cree avec succes");
        }

        #endregion

        #region Bills - Telechargement factures

        /// <summary>
        /// Telecharge les factures ouvertes en PDF.
        /// Criteres : inscrit + facture ouverte.
        /// </summary>
        [Test]
        [Category("LEGACY_READ_ONLY")]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task TelechargementFactures(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Telechargement des factures";
            descriptionTest = "L'utilisateur peut voir et telecharger ses factures";
            expectedResult = "Les factures s'affichent et peuvent etre telechargees";
            criteresSelection = new(CriteresInscritAvecFacture);
            await Setup(navigateur, mobile, lg);

            // Recharger les données après une éventuelle synchronisation du CSV.
            tousLesPartenaires = DataLoader.GetPartenaires(Serializer.PathToPartenaireCsv);
            Assert.That(
                DataLoader.FindClientWithCriteria(tousLesPartenaires, CriteresInscritAvecFacture),
                Is.Not.Null,
                "Aucun compte inscrit n'est marqué FactureOuverte=true dans le CSV. " +
                "Lancez la synchronisation des factures avant ce test.");

            // --- Login et langue ---
            await LoginAvecCriteres();
            var p = partenaireSelectionne;
            await SelectionnerLangue();
            Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | CC: {p.CompteContrat}");

            // --- Navigation via Page Objects ---
            var dashboard = new DashboardPage(page!);
            var billsPage = new BillsPage(page!);

            await dashboard.NaviguerOverview();

            // Ouvrir l'onglet Factures
            await dashboard.AllerFactures(lg, mobile);

            // Selectionner le compte contrat
            await billsPage.SelectionnerCompteContrat(lg, p.CompteContrat.ToString());
            await Report!.AddTestStep("Ouverture du compte de contrat", page!, testName);

            // Telecharger chaque facture
            var indicesFactures = await billsPage.ListerIndicesFacturesTelechargeables(lg);
            int nbFactures = indicesFactures.Count;
            Console.WriteLine($"Nombre de factures: {nbFactures}");
            Assert.That(nbFactures, Is.GreaterThan(0),
                $"Le compte de contrat {p.CompteContrat} est marqué FactureOuverte=true, " +
                "mais aucune facture avec bouton de téléchargement actif n'est affichée. " +
                "Relancez la synchronisation du CSV.");

            var dossierTelechargement = Path.Combine(Serializer.pathToReportFolder, testName);
            Directory.CreateDirectory(dossierTelechargement);
            foreach (var indexFacture in indicesFactures)
            {
                var download = await billsPage.TelechargerFacture(lg, indexFacture);
                var cheminFacture = Path.Combine(dossierTelechargement, download.SuggestedFilename);
                await download.SaveAsAsync(cheminFacture);
                Assert.That(File.Exists(cheminFacture), Is.True,
                    $"Le fichier de facture {indexFacture} n'a pas été enregistré.");
                Assert.That(new FileInfo(cheminFacture).Length, Is.GreaterThan(0),
                    $"Le fichier de facture {indexFacture} est vide.");
                await page!.WaitForTimeoutAsync(1500);
            }

            await Report.AddTestStep("Factures telechargees", page!, testName);
            Report.SetTestResult(true, $"{nbFactures} facture(s) telechargee(s)");
        }

        #endregion

        #region Bills - Historique factures (TODO)

        /// <summary>
        /// Verifie l'affichage de l'historique des factures.
        /// TODO : test a completer - le toggle et les assertions sont instables.
        /// </summary>
        [Test]
        [Category("LEGACY_READ_ONLY")]
        [Category("LEGACY_A_REPRENDRE")]
        [Explicit("Historique instable : assertions à fiabiliser avant exécution.")]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task VoirHistoriqueFacture(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Historique des factures";
            descriptionTest = "Les anciennes factures s'affichent";
            expectedResult = "L'historique contient des factures payees";
            criteresSelection = new(CriteresInscritAvecFacture);
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LoginAvecCriteres();
            var p = partenaireSelectionne;
            await SelectionnerLangue();

            // --- Navigation ---
            var dashboard = new DashboardPage(page!);
            var billsPage = new BillsPage(page!);

            await dashboard.NaviguerOverview();
            await dashboard.AllerFactures(lg, mobile);
            await billsPage.SelectionnerCompteContrat(lg, p.CompteContrat.ToString());

            // Afficher l'historique
            await billsPage.AfficherHistorique(lg, p.CompteContrat.ToString());

            // Verifier qu'il y a des factures dans l'historique
            int nbFacturesHistorique = await billsPage.CompterFacturesHistorique(lg);
            Assert.Greater(nbFacturesHistorique, 0, "Le nombre de factures payees est incorrect.");

            await Report!.AddTestStep("Historique des factures", page!, testName);
            Report.SetTestResult(true, $"{nbFacturesHistorique} facture(s) dans l'historique");
        }

        #endregion

        // ========================================================================
        //  USER - Donnees partenaire, mot de passe, inscription
        // ========================================================================

        #region User - Changement details partenaire

        /// <summary>
        /// Change le numero de telephone mobile du partenaire.
        /// Verifie que la nouvelle valeur est bien enregistree.
        /// </summary>
        [Test]
        [Category("LEGACY_MUTATION")]
        [Explicit("Modifie durablement le téléphone du partenaire.")]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task ChangerDetailsPartenaire(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Changement de details partenaire";
            descriptionTest = "Changement du numero de telephone du partenaire";
            expectedResult = "Le numero de telephone est change";
            criteresSelection = new(CriteresInscrit);
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LoginAvecCriteres();
            var p = partenaireSelectionne;
            await SelectionnerLangue();
            Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | Email: {p.Email}");

            // --- Navigation et modification via Page Objects ---
            var dashboard = new DashboardPage(page!);
            var profil = new UserProfilePage(page!);

            // Aller sur Mes Donnees et ouvrir la modification
            await dashboard.AllerMesDonnees(lg);
            await profil.OuvrirModificationCorrespondance(lg);

            // Generer un nouveau numero aleatoire
            var random = new Random();
            int nouveauNumero = random.Next(100000000, 999999999);

            // Lire l'ancien numero
            string ancienNumero = await profil.LireTelephoneMobile(lg);

            // Modifier et sauvegarder
            await profil.ModifierTelephoneMobile(lg, $"+41 {nouveauNumero}");
            await Report!.AddTestStep("Nouveau numero entre", page!, testName,
                $"Ancien: {ancienNumero} <br>Nouveau: +41 {nouveauNumero}");
        
            // Attendre que le bouton Sauvegarder soit enable
            await page!.Locator("button:has-text('Sauvegarder'), button:has-text('Speichern')").WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await page!.WaitForTimeoutAsync(500);
            
            await profil.Sauvegarder(lg);

            // Verifier que la valeur a change
            await profil.OuvrirModificationCorrespondance(lg);
            string valeurVerification = await profil.LireTelephoneMobile(lg);
            Assert.That(ancienNumero, Is.Not.EqualTo(valeurVerification),
                "Le numero de telephone n'a pas ete modifie");

            await Report.AddTestStep("Verification du changement", page!, testName);
            Report.SetTestResult(true, "Le numero de natel a bien ete change");
        }

        #endregion

        #region User - Reset mot de passe

        /// <summary>
        /// Demande un reset de mot de passe via la page de connexion.
        /// Se deconnecte d'abord, puis utilise "Mot de passe oublie".
        /// </summary>
        [Test]
        [Category("LEGACY_EXTERNAL_EFFECT")]
        [Explicit("Déclenche une demande de réinitialisation envoyée par e-mail.")]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task ResetMotDePasse(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Reset du mot de passe";
            descriptionTest = "L'utilisateur peut changer son mot de passe";
            expectedResult = "Le mail de reset doit etre recu";
            criteresSelection = new(CriteresInscrit);
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LoginAvecCriteres();
            var p = partenaireSelectionne;
            await SelectionnerLangue();
            Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | Email: {p.Email}");

            // --- Deconnexion via Page Object ---
            var dashboard = new DashboardPage(page!);
            await dashboard.SeDeconnecter(p.Prenom, p.Nom, lg, mobile);
            await Report!.AddTestStep("Deconnexion", page!, testName);

            // --- Reset mot de passe via Page Object ---
            var loginPage = new LoginPage(page!);
            await loginPage.DemanderResetMotDePasse(p.Email, lg);
            await Report.AddTestStep("Page mot de passe oublie", page!, testName);

            // Retour a la page de connexion
            await loginPage.RetourPageConnexion(lg);
            await Report.AddTestStep("Retour a la page de connexion", page!, testName);

            Report.SetTestResult(true, "Demande de changement de mot de passe envoyee");

        }

        #endregion

       

        #region User - Inscription par code d'acces

        /* /// <summary>
        /// Inscription d'un partenaire via son code d'acces.
        /// Ne necessite pas de connexion prealable.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task InscriptionParCode(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Enregistrement partenaire par code";
            descriptionTest = "Le partenaire peut s'enregistrer";
            expectedResult = "Le mail de validation doit etre recu";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Selectionner le partenaire (sans login) ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | Email: {p.Email}");

            // --- Naviguer vers le portail sans connexion ---
            await page!.GotoAsync(Serializer.GetActiveUrl());
            await TestTools.SelectLanguageWithoutBeingConnected(page, lg);
            await Report!.AddTestStep("Ouverture sans connexion", page, testName);

            // --- Inscription via Page Objects ---
            var loginPage = new LoginPage(page);
            await loginPage.CliquerInscrireMaintenant(lg);

            var signUpPage = new SignUpPage(page);
            await signUpPage.RemplirInscriptionParCode(lg, p.NumeroPartenaire.ToString(), "0101");
            await Report.AddTestStep("Saisie des donnees", page, testName);

            await signUpPage.CliquerSuivant(lg);
            Report.SetTestResult(true, "Formulaire d'inscription par code soumis");
        } */

        #endregion

        #region User - Inscription par derniere facture

       /*  /// <summary>
        /// Inscription d'un partenaire via le montant de sa derniere facture.
        /// Ne necessite pas de connexion prealable.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task InscriptionParFacture(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Enregistrement partenaire par facture";
            descriptionTest = "Le partenaire peut s'enregistrer";
            expectedResult = "Le mail de validation doit etre recu";
            criteresSelection = new(CriteresAucun);
            await Setup(navigateur, mobile, lg);

            // --- Selectionner le partenaire (sans login) ---
            var p = DataLoader.FindClientWithCriteria(tousLesPartenaires, criteresSelection);
            partenaireSelectionne = p;
            Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | Email: {p.Email}");

            // --- Naviguer vers le portail sans connexion ---
            await page!.GotoAsync(Serializer.GetActiveUrl());
            await TestTools.SelectLanguageWithoutBeingConnected(page, lg);
            await Report!.AddTestStep("Ouverture de la page", page, testName);

            // --- Inscription via Page Objects ---
            var loginPage = new LoginPage(page);
            await loginPage.CliquerInscrireMaintenant(lg);

            var signUpPage = new SignUpPage(page);
            await signUpPage.RemplirInscriptionParFacture(lg,
                p.NumeroPartenaire.ToString(), "12345678", "9101112");
            await Report.AddTestStep("Saisie des donnees", page, testName);

            await signUpPage.CliquerSuivant(lg);
            Report.SetTestResult(true, "Formulaire d'inscription par facture soumis"); 
        }
        */
        #endregion

        // ========================================================================
        //  MOVING - Emmenagement, demenagement
        // ========================================================================

        #region Moving - Emmenagement

        /// <summary>
        /// Remplit le formulaire d'emmenagement dans la zone GRE.
        /// Verifie que l'adresse et la date sont visibles a la confirmation.
        /// </summary>
        /* [Test]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task Emmenagement(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Emmenagement";
            descriptionTest = "L'utilisateur peut annoncer un emmenagement";
            expectedResult = "Le formulaire est soumis avec succes";
            criteresSelection = new(CriteresInscrit);
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LoginAvecCriteres();
            var p = partenaireSelectionne;
            await SelectionnerLangue();

            // --- Donnees du formulaire ---
            string codePostal = "1741";
            string rue = "route du Rossi";
            string numeroRue = "32";
            string dateDemenagement = DateTime.Today.AddMonths(1).ToString("dd.MM.yy");
            string nomAncienLocataire = "Aissa";
            string prenomAncienLocataire = "Bovet";

            // --- Navigation et formulaire via Page Objects ---
            var dashboard = new DashboardPage(page!);
            var movingPage = new MovingPage(page!);

            await dashboard.AllerEmmenagement(lg);

            // Remplir l'adresse
            await movingPage.RemplirEmmenagement(lg, codePostal, rue, numeroRue, dateDemenagement);

            // Remplir l'ancien locataire et cocher "je ne connais pas"
            await movingPage.RemplirAncienLocataire(lg, nomAncienLocataire, prenomAncienLocataire);
            await movingPage.CocherNeConnaisPasLocataire(lg);

            await DebugPause();

            // Soumettre
            await movingPage.CliquerSuivant(lg);

            // Verifier l'adresse et la date a la confirmation
            Assert.IsTrue(await movingPage.EstTexteVisible(rue), "La rue n'est pas visible a la confirmation");
            Assert.IsTrue(await movingPage.EstTexteVisible(DateTime.Today.AddMonths(1).ToString("dd.MM.yyyy")),
                "La date n'est pas visible a la confirmation");

            await movingPage.CliquerEnvoyer(lg);
            await movingPage.CliquerFermer(lg);

            await Report!.AddTestStep("Emmenagement soumis", page!, testName);
            Report.SetTestResult(true, "Formulaire d'emmenagement soumis avec succes");
        } */

        #endregion

        #region Moving - Demenagement (TODO)

       /*  /// <summary>
        /// Remplit le formulaire de demenagement.
        /// TODO : test a completer - les donnees de destination sont hardcodees.
        /// </summary>
        [Test]
        [Category("LEGACY_A_REPRENDRE")]
        [Explicit("Scénario de déménagement incomplet avec données codées en dur.")]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task Demenagement(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Demenagement";
            descriptionTest = "L'utilisateur peut annoncer son demenagement";
            expectedResult = "Le formulaire de demenagement est soumis";
            criteresSelection = new(CriteresInscrit);
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LoginAvecCriteres();
            var p = partenaireSelectionne;
            await SelectionnerLangue();
            Console.WriteLine($"Partenaire: {p.NumeroPartenaire} | LC: {p.LieuDeConsommation}");

            // --- Navigation ---
            var dashboard = new DashboardPage(page!);
            var movingPage = new MovingPage(page!);

            await dashboard.AllerDemenagement(lg);

            // --- Lieu de consommation actuel ---
            // TODO: le texte de l'option est hardcode, il faudrait le recuperer dynamiquement
            await movingPage.SelectionnerLieuConsommation(lg, p.LieuDeConsommation);

            // Date de remise des cles
            string dateRemise = DateTime.Today.AddMonths(1).ToString("dd.MM.yy");
            await page!.GetByRole(AriaRole.Textbox,
                new() { Name = ResourceHelper.GetStringPortail("DateRemiseKey", lg) }).FillAsync(dateRemise);

            // Nouveau locataire
            await movingPage.RemplirNouveauLocataire(lg, "Dupont", "Jean");

            // Adresse du nouveau lieu
            await movingPage.RemplirAdresseDestination(lg,
                "1700", "route de la Singine", "2", "appartement", "31.08.2025");

            await DebugPause();

            // TODO: soumettre le formulaire quand le test sera stable
            // await movingPage.CliquerSuivant(lg);
            // await movingPage.CliquerEnvoyer(lg);
            // await movingPage.CliquerFermer(lg);
           
        } 
            */
        #endregion
    }
}
