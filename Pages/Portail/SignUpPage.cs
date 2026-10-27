using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Page d'inscription du portail.
    /// Gère : inscription par code d'accès ou par dernière facture
    /// </summary>
    public class SignUpPage
    {
        private readonly IPage _page;

        public SignUpPage(IPage page)
        {
            _page = page;
        }

        #region Locators

        /// <summary>Radio "Code d'accès"</summary>
        private ILocator RadioCodeAcces(string langue) =>
            _page.GetByRole(AriaRole.Radio, new() { Name = ResourceHelper.GetStringPortail("AccessCode", langue) });

        /// <summary>Radio "Montant dernière facture"</summary>
        private ILocator RadioDerniereFacture(string langue) =>
            _page.GetByRole(AriaRole.Radio, new() { Name = ResourceHelper.GetStringPortail("LastBillAmount", langue) });

        /// <summary>Champ numéro de client</summary>
        private ILocator ChampNumeroClient(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("ClientNumber", langue) });

        /// <summary>Champ code d'accès</summary>
        private ILocator ChampCodeAcces(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("AccessCode", langue) });

        /// <summary>Champ compte contrat</summary>
        private ILocator ChampCompteContrat(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("CC", langue) });

        /// <summary>Champ montant dernière facture</summary>
        private ILocator ChampMontantDerniereFacture(string langue) =>
            _page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringPortail("LastBillAmount", langue) });

        /// <summary>Bouton "Suivant"</summary>
        private ILocator BoutonSuivant(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("next", langue) });

        #endregion

        #region Actions

        /// <summary>
        /// Sélectionne le mode d'inscription par code d'accès
        /// </summary>
        public async Task ChoisirInscriptionParCode(string langue)
        {
            await RadioCodeAcces(langue).CheckAsync();
        }

        /// <summary>
        /// Sélectionne le mode d'inscription par dernière facture
        /// </summary>
        public async Task ChoisirInscriptionParFacture(string langue)
        {
            await RadioDerniereFacture(langue).CheckAsync();
        }

        /// <summary>
        /// Remplit le formulaire d'inscription par code d'accès
        /// </summary>
        public async Task RemplirInscriptionParCode(string langue, string numeroClient, string codeAcces)
        {
            await ChoisirInscriptionParCode(langue);
            await ChampNumeroClient(langue).FillAsync(numeroClient);
            await ChampCodeAcces(langue).FillAsync(codeAcces);
        }

        /// <summary>
        /// Remplit le formulaire d'inscription par dernière facture
        /// </summary>
        public async Task RemplirInscriptionParFacture(string langue, string numeroClient, string compteContrat, string montant)
        {
            await ChoisirInscriptionParFacture(langue);
            await ChampNumeroClient(langue).FillAsync(numeroClient);
            await ChampCompteContrat(langue).FillAsync(compteContrat);
            await ChampMontantDerniereFacture(langue).FillAsync(montant);
        }

        /// <summary>
        /// Clique sur "Suivant" pour soumettre l'inscription
        /// </summary>
        public async Task CliquerSuivant(string langue)
        {
            await BoutonSuivant(langue).ClickAsync();
        }

        #endregion
    }
}
