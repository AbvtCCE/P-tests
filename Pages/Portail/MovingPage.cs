using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Page d'emménagement / déménagement du portail.
    /// Gère : formulaires d'emménagement et de déménagement
    /// </summary>
    public class MovingPage
    {
        private readonly IPage _page;

        public MovingPage(IPage page)
        {
            _page = page;
        }

        #region Locators - Champs communs

        /// <summary>Champ code postal (NPA)</summary>
        private ILocator ChampCodePostal(string langue) =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringPortail("NPA", langue) });

        /// <summary>Listbox NPA (suggestions)</summary>
        private ILocator ListboxCodePostal(string langue) =>
            _page.GetByRole(AriaRole.Listbox, new() { Name = ResourceHelper.GetStringPortail("NPA", langue) });

        /// <summary>Champ rue</summary>
        private ILocator ChampRue(string langue) =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringPortail("rue", langue) });

        /// <summary>Champ numéro de rue</summary>
        private ILocator ChampNumeroRue(string langue) =>
            _page.GetByRole(AriaRole.Searchbox, new() { Name = ResourceHelper.GetStringPortail("number", langue) });

        /// <summary>Champ date de remise des clés</summary>
        private ILocator ChampDateRemiseCles(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("DateRemiseKey", langue) });

        /// <summary>Champ description de l'objet/habitation</summary>
        private ILocator ChampDescriptionObjet(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("descObj", langue) });

        /// <summary>Bouton "Suivant"</summary>
        private ILocator BoutonSuivant(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("next", langue) });

        /// <summary>Bouton "Envoyer"</summary>
        private ILocator BoutonEnvoyer(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("send", langue) });

        /// <summary>Bouton "Fermer" après confirmation</summary>
        private ILocator BoutonFermer(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("close", langue) });

        /// <summary>Bouton "Annuler"</summary>
        private ILocator BoutonAnnuler(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("cancel", langue) });

        #endregion

        #region Locators - Emménagement

        /// <summary>Champ nom de l'ancien locataire</summary>
        private ILocator ChampNomAncienLocataire(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("name", langue) });

        /// <summary>Champ prénom de l'ancien locataire</summary>
        private ILocator ChampPrenomAncienLocataire(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("lastname", langue), Exact = true });

        /// <summary>Checkbox "Je ne connais pas l'ancien locataire"</summary>
        private ILocator CheckboxNeConnaisPasLocataire(string langue) =>
            _page.GetByRole(AriaRole.Checkbox, new() { Name = ResourceHelper.GetStringPortail("IDKTPL", langue) });

        /// <summary>Message "Hors zone GRE"</summary>
        private ILocator MessageHorsZone(string langue) =>
            _page.Locator("div.th-bg--primary-light.pa--1.mr--2.mb--2.gfc-font-label");

        #endregion

        #region Locators - Déménagement

        /// <summary>Sélection du lieu de consommation actuel</summary>
        private ILocator ComboboxLieuConsommation(string langue) =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringPortail("lc", langue) });

        /// <summary>Sélection de la civilité (Mr/Mme)</summary>
        private ILocator ComboboxCivilite(string langue) =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringPortail("cvivilite", langue) });

        /// <summary>Option "Monsieur"</summary>
        private ILocator OptionMonsieur(string langue) =>
            _page.GetByRole(AriaRole.Option, new() { Name = ResourceHelper.GetStringPortail("mr", langue) });

        /// <summary>Champ nom du nouveau locataire</summary>
        private ILocator ChampNomNouveauLocataire(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("name", langue) });

        /// <summary>Champ prénom du nouveau locataire</summary>
        private ILocator ChampPrenomNouveauLocataire(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("lastname", langue) }).Last;

        #endregion

        #region Actions - Emménagement

        /// <summary>
        /// Remplit le formulaire d'emménagement
        /// </summary>
        public async Task RemplirEmmenagement(string langue, string codePostal, string rue, string numero, string date)
        {
            await ChampCodePostal(langue).FillAsync(codePostal);
            await ListboxCodePostal(langue).ClickAsync();
            await ChampDateRemiseCles(langue).FillAsync(date);
            await ChampRue(langue).FillAsync(rue);
            // Attendre et sélectionner la suggestion de rue
            await _page.GetByRole(AriaRole.Listbox, new() { Name = rue }).ClickAsync();
            await ChampNumeroRue(langue).FillAsync(numero);
        }

        /// <summary>
        /// Remplit les infos de l'ancien locataire
        /// </summary>
        public async Task RemplirAncienLocataire(string langue, string nom, string prenom)
        {
            await ChampNomAncienLocataire(langue).FillAsync(nom);
            await ChampPrenomAncienLocataire(langue).FillAsync(prenom);
        }

        /// <summary>
        /// Coche "Je ne connais pas l'ancien locataire"
        /// </summary>
        public async Task CocherNeConnaisPasLocataire(string langue)
        {
            await CheckboxNeConnaisPasLocataire(langue).CheckAsync();
        }

        #endregion

        #region Actions - Déménagement

        /// <summary>
        /// Sélectionne le lieu de consommation actuel dans le dropdown
        /// </summary>
        public async Task SelectionnerLieuConsommation(string langue, string optionTexte)
        {
            await ComboboxLieuConsommation(langue).Locator("div").First.ClickAsync();
            await _page.GetByRole(AriaRole.Option, new() { Name = optionTexte }).ClickAsync();
        }

        /// <summary>
        /// Remplit la civilité et le nom du nouveau locataire
        /// </summary>
        public async Task RemplirNouveauLocataire(string langue, string nom, string prenom)
        {
            await ComboboxCivilite(langue).Locator("div").First.ClickAsync();
            await OptionMonsieur(langue).ClickAsync();
            await ChampNomNouveauLocataire(langue).FillAsync(nom);
            await ChampPrenomNouveauLocataire(langue).FillAsync(prenom);
        }

        /// <summary>
        /// Remplit l'adresse de destination (déménagement)
        /// </summary>
        public async Task RemplirAdresseDestination(string langue, string codePostal, string rue, string numero, string descriptionObjet, string date)
        {
            await ChampCodePostal(langue).ClickAsync();
            await ChampCodePostal(langue).FillAsync(codePostal);
            await ListboxCodePostal(langue).ClickAsync();
            await ChampRue(langue).FillAsync(rue);
            await ChampNumeroRue(langue).FillAsync(numero);
            await ChampDescriptionObjet(langue).FillAsync(descriptionObjet);
            await ChampDateRemiseCles(langue).FillAsync(date);
        }

        #endregion

        #region Actions - Boutons communs

        /// <summary>Clique sur "Suivant"</summary>
        public async Task CliquerSuivant(string langue) => await BoutonSuivant(langue).ClickAsync();

        /// <summary>Clique sur "Envoyer"</summary>
        public async Task CliquerEnvoyer(string langue) => await BoutonEnvoyer(langue).ClickAsync();

        /// <summary>Clique sur "Fermer"</summary>
        public async Task CliquerFermer(string langue) => await BoutonFermer(langue).ClickAsync();

        /// <summary>Clique sur "Annuler"</summary>
        public async Task CliquerAnnuler(string langue) => await BoutonAnnuler(langue).ClickAsync();

        #endregion

        #region Assertions

        /// <summary>
        /// Vérifie qu'un texte est visible sur la page (ex: adresse de confirmation)
        /// </summary>
        public async Task<bool> EstTexteVisible(string texte)
        {
            return await _page.GetByText(texte).IsVisibleAsync();
        }

        /// <summary>
        /// Vérifie le message "Hors zone GRE"
        /// </summary>
        public async Task<string> LireMessageHorsZone(string langue)
        {
            return await MessageHorsZone(langue).InnerTextAsync();
        }

        /// <summary>
        /// Vérifie que le bouton submit est désactivé (hors zone)
        /// </summary>
        public async Task<bool> EstBoutonSubmitDesactive()
        {
            return await _page.Locator("button[type='submit']").First.IsDisabledAsync();
        }

        #endregion
    }
}
