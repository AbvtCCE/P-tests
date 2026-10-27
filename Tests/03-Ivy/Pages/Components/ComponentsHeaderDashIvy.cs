using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TestPlayrights
{
    /// <summary>
    /// Composant Header du dashboard IVY.
    /// Present sur la plupart des pages apres recherche d'un partenaire.
    /// Centralise :
    ///   - Verifications des infos partenaire (SAP, nom, email)
    ///   - Boutons d'action : edition contact, edition partenaire, historique, espace client
    ///   - Dialog "Mutation du partenaire" (champs + actions)
    ///
    /// Les IDs JSF en j_id_* sont auto-generes et instables : on cible par
    /// icone (fa-pencil), titre (Espace client), texte du bouton (Historique)
    /// ou IDs nommes stables (businessPartnerFormId:name1, etc.).
    /// </summary>
    public class ComponentsHeaderDashIvy
    {
        private readonly IPage Page;

        public ComponentsHeaderDashIvy(IPage page)
        {
            Page = page;
        }

        #region Locators - Boutons du header

        private ILocator BoutonsCrayon => Page.Locator("button:has(.fa-pencil)");
        private ILocator BoutonEditionContact => BoutonsCrayon.Nth(0);
        private ILocator BoutonEditionPartenaire => BoutonsCrayon.Nth(1);
        private ILocator BoutonEspaceClient(string langue) =>
            Page.Locator($"button[title='{ResourceHelper.GetStringIvy("CustomerPortal", langue)}']");
        private ILocator BoutonHistorique(string langue) =>
            Page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("History", langue) });

        #endregion

        #region Locators - Dialog "Mutation du partenaire"

        private ILocator Dialog => Page.Locator("#businessPartnerDialog");
        private ILocator DialogTitre => Dialog.GetByText("Mutation du partenaire");

        private ILocator ChampPartnerId => Page.Locator("[id='businessPartnerFormId:partnerId']");
        private ILocator ChampName1 => Page.Locator("[id='businessPartnerFormId:name1']");
        private ILocator ChampName2 => Page.Locator("[id='businessPartnerFormId:name2']");
        private ILocator ChampEmail => Page.Locator("[id='businessPartnerFormId:email']");
        private ILocator ChampPhone => Page.Locator("[id='businessPartnerFormId:phone']");
        private ILocator ChampMobile => Page.Locator("[id='businessPartnerFormId:mobile']");
        private ILocator ChampPartnerType => Page.Locator("[id='businessPartnerFormId:partnerType_label']");
        private ILocator ChampPartnerVariety => Page.Locator("[id='businessPartnerFormId:partnerVariety_input']");
        private ILocator ChampLangue => Page.Locator("[id='businessPartnerFormId:language_input']");
        private ILocator ChampPhoneCountry => Page.Locator("[id='businessPartnerFormId:phoneCountry_input']");
        private ILocator ChampMobileCountry => Page.Locator("[id='businessPartnerFormId:mobileCountry_input']");
        private ILocator TableauAdresses => Page.Locator("[id='businessPartnerFormId:tabContainer:tabContainer']");

        private ILocator BoutonEnregistrer => Dialog.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" });
        private ILocator BoutonAnnuler => Dialog.GetByRole(AriaRole.Button, new() { Name = "Annuler" });
        private ILocator BoutonNouvelleAdresse => Dialog.GetByRole(AriaRole.Button, new() { Name = "Nouvelle adresse" });

        #endregion

        #region Actions - Dialog

        /// <summary>Ouvre le dialog "Mutation du partenaire" via le premier bouton crayon.</summary>
        public async Task OuvrirDialogPartenaireAsync()
        {
            await BoutonEditionContact.ClickAsync();
            await Assertions.Expect(Dialog).ToBeVisibleAsync();
        }

        /// <summary>Ferme le dialog en cliquant sur Annuler.</summary>
        public async Task FermerDialogAsync()
        {
            await BoutonAnnuler.ClickAsync();
            await Assertions.Expect(Dialog).Not.ToBeVisibleAsync();
        }

        #endregion

        #region Actions - Mise a jour de champs

        /// <summary>
        /// Change le numero de telephone du partenaire dans IVY ET dans le CSV.
        /// Ouvre le dialog, remplit le champ, sauvegarde, puis synchronise le modele + le CSV.
        /// </summary>
        public Task ChangerTelephoneAsync(Partenaire p, string nouvelleValeur) =>
            ChangerChampAsync(p, ChampPhone, nouvelleValeur, (pp, v) => pp.Telephone = v);

        /// <summary>
        /// Change le numero de mobile du partenaire dans IVY ET dans le CSV.
        /// Ouvre le dialog, remplit le champ, sauvegarde, puis synchronise le modele + le CSV.
        /// </summary>
        public Task ChangerMobileAsync(Partenaire p, string nouvelleValeur) =>
            ChangerChampAsync(p, ChampMobile, nouvelleValeur, (pp, v) => pp.Mobile = v);

        /// <summary>
        /// Logique commune : ouvre le dialog, modifie le champ IVY, sauvegarde,
        /// met a jour la propriete du Partenaire en memoire, puis ecrit le CSV.
        /// </summary>
        private async Task ChangerChampAsync(
            Partenaire p,
            ILocator champIvy,
            string nouvelleValeur,
            Action<Partenaire, string> appliquerAuModele)
        {
            await OuvrirDialogPartenaireAsync();
            await champIvy.FillAsync(nouvelleValeur);
            await BoutonEnregistrer.ClickAsync();
            await Assertions.Expect(Dialog).Not.ToBeVisibleAsync(new() { Timeout = 15_000 });

            appliquerAuModele(p, nouvelleValeur);
            DataLoader.MettreAJourPartenaire(Serializer.PathToPartenaireCsv, p);
        }

        #endregion

        #region Actions - Popups

        /// <summary>Clique sur Historique et retourne le popup ouvert (charge complete).</summary>
        public async Task<IPage> OuvrirHistoriqueAsync(string langue)
        {
            var popup = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await BoutonHistorique(langue).ClickAsync();
            });
            await popup.WaitForLoadStateAsync();
            return popup;
        }

        /// <summary>Clique sur Espace client et retourne le popup (nouvel onglet, sans attendre le load).</summary>
        public async Task<IPage> OuvrirEspaceClientAsync(string langue)
        {
            return await Page.RunAndWaitForPopupAsync(async () =>
            {
                await BoutonEspaceClient(langue).ClickAsync();
            });
        }

        #endregion

        #region Verifications

        /// <summary>
        /// Verifie que les infos partenaire (SAP, nom, email) sont visibles dans le header.
        /// Le libelle du numero SAP varie selon la langue : "Nr. SAP" en FR, "SAP NR." en DE, "SAP No." en EN.
        /// </summary>
        public async Task VerifierInfosPartenaireAsync(Partenaire p, string langue)
        {
            string sapLabel = ResourceHelper.GetStringIvy("SapNumber", langue);
            await Assertions.Expect(Page.GetByText($"{sapLabel}:{p.NumeroPartenaire}").First).ToBeVisibleAsync();
           
            await Assertions.Expect(Page.GetByText(p.Nom!).First).ToBeVisibleAsync();
            await Assertions.Expect(Page.GetByText(p.Email).First).ToBeVisibleAsync();
        }

        /// <summary>Verifie que les 4 boutons du header sont visibles.</summary>
        public async Task VerifierBoutonsHeaderAsync(string langue)
        {
            await Assertions.Expect(BoutonEditionContact).ToBeVisibleAsync();
            await Assertions.Expect(BoutonEditionPartenaire).ToBeVisibleAsync();
            await Assertions.Expect(BoutonHistorique(langue)).ToBeVisibleAsync();
            
            await Assertions.Expect(BoutonEspaceClient(langue)).ToBeVisibleAsync();
        }

        /// <summary>
        /// Verifie le contenu du dialog "Mutation du partenaire".
        /// Prerequis : le dialog doit etre ouvert (cf. OuvrirDialogPartenaireAsync).
        /// </summary>
        public async Task VerifierChampsDialogAsync(Partenaire p)
        {
            await Assertions.Expect(DialogTitre).ToBeVisibleAsync();

            // Valeurs comparees aux donnees du CSV
            await Assertions.Expect(ChampPartnerId).ToHaveValueAsync(p.NumeroPartenaire!.ToString()!);
            await Assertions.Expect(ChampName1).ToHaveValueAsync(p.Prenom!);
            await Assertions.Expect(ChampName2).ToHaveValueAsync(p.Nom!);
            await Assertions.Expect(ChampEmail).ToHaveValueAsync(p.Email);

            // Champs visibles (valeur variable selon le partenaire)
            await Assertions.Expect(ChampPhone).ToBeVisibleAsync();
            await Assertions.Expect(ChampMobile).ToBeVisibleAsync();
            await Assertions.Expect(ChampPartnerType).ToBeVisibleAsync();
            await Assertions.Expect(ChampPartnerVariety).ToBeVisibleAsync();
            await Assertions.Expect(ChampLangue).ToBeVisibleAsync();
            await Assertions.Expect(ChampPhoneCountry).ToBeVisibleAsync();
            await Assertions.Expect(ChampMobileCountry).ToBeVisibleAsync();
            await Assertions.Expect(TableauAdresses).ToBeVisibleAsync();

            // Boutons d'action du dialog
            await Assertions.Expect(BoutonEnregistrer).ToBeVisibleAsync();
            await Assertions.Expect(BoutonAnnuler).ToBeVisibleAsync();
            await Assertions.Expect(BoutonNouvelleAdresse).ToBeVisibleAsync();
        }

        /// <summary>
        /// Verifie la valeur du champ Telephone dans le dialog (prerequis : dialog ouvert).
        /// </summary>
        public async Task VerifierTelephoneDansDialogAsync(string valeurAttendue)
        {
            await Assertions.Expect(ChampPhone).ToHaveValueAsync(valeurAttendue);
        }

        /// <summary>
        /// Verifie la valeur du champ Mobile dans le dialog (prerequis : dialog ouvert).
        /// </summary>
        public async Task VerifierMobileDansDialogAsync(string valeurAttendue)
        {
            await Assertions.Expect(ChampMobile).ToHaveValueAsync(valeurAttendue);
        }

        /// <summary>
        /// Verification complete du header : infos partenaire, boutons, dialog (ouvre/verifie/ferme),
        /// historique (ouvre/ferme), espace client (ouvre/verifie URL/ferme).
        /// </summary>
        public async Task VerifierToutLeHeaderAsync(Partenaire p, string langue)
        {
            await VerifierInfosPartenaireAsync(p, langue);
            await VerifierBoutonsHeaderAsync(langue);

            // Dialog "Mutation du partenaire"
            await OuvrirDialogPartenaireAsync();
            await VerifierChampsDialogAsync(p);
            await FermerDialogAsync();

            // Historique : juste verifier que le popup s'ouvre
            var popupHist = await OuvrirHistoriqueAsync(langue);
            await popupHist.CloseAsync();

            // Espace client : verifier l'URL d'ouverture du nouvel onglet
            var popupPortail = await OuvrirEspaceClientAsync(langue);
            await Assertions.Expect(popupPortail).ToHaveURLAsync(new Regex(@"qu1\.web\.cce24x7\.net"));
            await popupPortail.CloseAsync();
        }

        #endregion
    }
}
