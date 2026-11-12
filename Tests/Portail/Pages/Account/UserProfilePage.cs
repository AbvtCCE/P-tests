using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Page "Mes données" / profil utilisateur du portail.
    /// Gère : consultation et modification des données personnelles (adresse, téléphone)
    /// </summary>
    public class UserProfilePage
    {
        private readonly IPage _page;

        public UserProfilePage(IPage page)
        {
            _page = page;
        }

        #region Locators

        /// <summary>Lien pour ouvrir le formulaire de modification des coordonnées</summary>
        private ILocator LienModifierCorrespondance(string langue) =>
            _page.Locator("gfc-loading-overlay")
                 .Filter(new() { HasText = ResourceHelper.GetStringPortail("Corespondance", langue) })
                 .Locator("a");

        /// <summary>Champ téléphone mobile</summary>
        private ILocator ChampTelephoneMobile(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("mobilePhone", langue) });

        /// <summary>Champ code postal (NPA)</summary>
        private ILocator ChampCodePostal =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = "NPA" });

        /// <summary>Champ rue</summary>
        private ILocator ChampRue =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = "Rue" });

        /// <summary>Champ numéro de rue</summary>
        private ILocator ChampNumeroRue =>
            _page.GetByRole(AriaRole.Searchbox, new() { Name = "Numéro" });

        /// <summary>Bouton "Sauvegarder"</summary>
        private ILocator BoutonSauvegarder(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("save", langue) });

        #endregion

        #region Actions

        /// <summary>
        /// Ouvre le formulaire de modification des coordonnées
        /// </summary>
        public async Task OuvrirModificationCorrespondance(string langue)
        {
            await LienModifierCorrespondance(langue).WaitForAsync();
            await LienModifierCorrespondance(langue).ClickAsync();
        }

        /// <summary>
        /// Lit la valeur actuelle du champ téléphone mobile
        /// </summary>
        public async Task<string> LireTelephoneMobile(string langue)
        {
            return await ChampTelephoneMobile(langue).InputValueAsync();
        }

        /// <summary>
        /// Modifie le numéro de téléphone mobile
        /// </summary>
        public async Task ModifierTelephoneMobile(string langue, string nouveauNumero)
        {
            await ChampTelephoneMobile(langue).FillAsync(nouveauNumero);
        }

        /// <summary>
        /// Lit le code postal actuel
        /// </summary>
        public async Task<string> LireCodePostal()
        {
            return await ChampCodePostal.InputValueAsync();
        }

        /// <summary>
        /// Lit la rue actuelle
        /// </summary>
        public async Task<string> LireRue()
        {
            return await ChampRue.InputValueAsync();
        }

        /// <summary>
        /// Lit le numéro de rue actuel
        /// </summary>
        public async Task<string> LireNumeroRue()
        {
            return await ChampNumeroRue.InputValueAsync();
        }

        /// <summary>
        /// Sauvegarde les modifications
        /// </summary>
        public async Task Sauvegarder(string langue)
        {
            await BoutonSauvegarder(langue).ClickAsync();
        }

        #endregion
    }
}
