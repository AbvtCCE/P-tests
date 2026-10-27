using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Page de formulaire de contact du portail.
    /// Gère : sélection du thème, de l'action souhaitée
    /// </summary>
    public class ContactPage
    {
        private readonly IPage _page;

        public ContactPage(IPage page)
        {
            _page = page;
        }

        #region Locators

        /// <summary>Combobox "Thème"</summary>
        private ILocator ComboboxTheme(string langue) =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringPortail("theme", langue) });

        /// <summary>Combobox "Que souhaitez-vous faire ?"</summary>
        private ILocator ComboboxAction(string langue) =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringPortail("WhatYouWToDo", langue) });

        /// <summary>Combobox "Rendez-vous"</summary>
        private ILocator ComboboxRendezVous(string langue) =>
            _page.GetByRole(AriaRole.Combobox, new() { Name = ResourceHelper.GetStringPortail("RDVC", langue) });

        /// <summary>Bouton "Résoudre le problème soi-même"</summary>
        private ILocator BoutonResoudreSoiMeme =>
            _page.GetByRole(AriaRole.Button, new() { Name = "Résoudre le problème soi-même" });

        #endregion

        #region Actions

        /// <summary>
        /// Sélectionne un thème dans le dropdown
        /// </summary>
        public async Task SelectionnerTheme(string langue, string cle)
        {
            await ComboboxTheme(langue).GetByRole(AriaRole.Img).ClickAsync();
            await _page.GetByText(ResourceHelper.GetStringPortail(cle, langue)).ClickAsync();
        }

        /// <summary>
        /// Sélectionne une action dans le dropdown "Que souhaitez-vous faire ?"
        /// </summary>
        public async Task SelectionnerAction(string langue, string cle)
        {
            await ComboboxAction(langue).GetByRole(AriaRole.Img).ClickAsync();
            await _page.GetByText(ResourceHelper.GetStringPortail(cle, langue)).ClickAsync();
        }

        /// <summary>
        /// Sélectionne une action par option (AriaRole.Option)
        /// </summary>
        public async Task SelectionnerActionParOption(string langue, string cle)
        {
            await ComboboxAction(langue).GetByRole(AriaRole.Img).ClickAsync();
            await _page.GetByRole(AriaRole.Option, new() { Name = ResourceHelper.GetStringPortail(cle, langue) }).ClickAsync();
        }

        /// <summary>
        /// Sélectionne un sous-thème (ex: "Tarifs d'électricité", "Facture d'électricité")
        /// </summary>
        public async Task SelectionnerSousTheme(string langue, string cle)
        {
            await _page.GetByText(ResourceHelper.GetStringPortail(cle, langue)).Last.ClickAsync();
        }

        /// <summary>
        /// Sélectionne une option dans le combobox rendez-vous
        /// </summary>
        public async Task SelectionnerRendezVous(string langue, string cle)
        {
            await ComboboxRendezVous(langue).GetByRole(AriaRole.Img).ClickAsync();
            await _page.GetByText(ResourceHelper.GetStringPortail(cle, langue), new() { Exact = true }).Last.ClickAsync();
        }

        /// <summary>
        /// Clique sur "Résoudre le problème soi-même"
        /// </summary>
        public async Task CliquerResoudreSoiMeme()
        {
            await BoutonResoudreSoiMeme.ClickAsync();
        }

        #endregion
    }
}
