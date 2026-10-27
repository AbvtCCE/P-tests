using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Page de connexion du portail (my-q.groupe-e.ch)
    /// Gère : email, mot de passe, soumission, lien mot de passe oublié
    /// </summary>
    public class LoginPage
    {
        private readonly IPage _page;

        public LoginPage(IPage page)
        {
            _page = page;
        }

        #region Locators

        /// <summary>Champ email / username</summary>
        private ILocator ChampEmail => _page.Locator("input[name='username']");

        /// <summary>Champ mot de passe</summary>
        private ILocator ChampMotDePasse => _page.Locator("input[name='password']");

        /// <summary>Bouton de soumission du formulaire</summary>
        private ILocator BoutonConnexion => _page.Locator("button[type='submit']");

        /// <summary>Bouton "Mot de passe oublié"</summary>
        private ILocator BoutonMotDePasseOublie(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("passwordForgotten", langue) });

        /// <summary>Champ email sur la page mot de passe oublié</summary>
        private ILocator ChampEmailReset => _page.GetByRole(AriaRole.Textbox, new() { Name = "E-mail" });

        /// <summary>Bouton "Envoyer" sur la page mot de passe oublié</summary>
        private ILocator BoutonEnvoyerReset(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("send", langue) });

        /// <summary>Bouton "Retour à la page de connexion"</summary>
        private ILocator BoutonRetourConnexion(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("BackToConnexionPage", langue) });

        /// <summary>Bouton "S'inscrire maintenant"</summary>
        private ILocator BoutonInscrireMaintenant(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("SubscribeNow", langue) });

        #endregion

        #region Actions

        /// <summary>
        /// Navigue vers la page de connexion du portail
        /// </summary>
        public async Task Naviguer()
        {
            await _page.GotoAsync(Serializer.GetUrlPortailQual());
        }

        /// <summary>
        /// Remplit le champ email
        /// </summary>
        public async Task RemplirEmail(string email)
        {
            await ChampEmail.WaitForAsync(new() { Timeout = 5000 });
            await ChampEmail.FillAsync(email);
        }

        /// <summary>
        /// Remplit le champ mot de passe
        /// </summary>
        public async Task RemplirMotDePasse(string motDePasse)
        {
            await ChampMotDePasse.FillAsync(motDePasse);
        }

        /// <summary>
        /// Clique sur le bouton de connexion et attend le chargement
        /// </summary>
        public async Task Soumettre()
        {
            await BoutonConnexion.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        /// <summary>
        /// Processus complet de connexion : email + mot de passe + soumission
        /// </summary>
        public async Task SeConnecter(string email, string motDePasse)
        {
            await Naviguer();
            await RemplirEmail(email);
            await RemplirMotDePasse(motDePasse);
            await Soumettre();
        }

        /// <summary>
        /// Clique sur "Mot de passe oublié"
        /// </summary>
        public async Task CliquerMotDePasseOublie(string langue)
        {
            await BoutonMotDePasseOublie(langue).ClickAsync();
        }

        /// <summary>
        /// Processus complet de reset mot de passe
        /// </summary>
        public async Task DemanderResetMotDePasse(string email, string langue)
        {
            await CliquerMotDePasseOublie(langue);
            await ChampEmailReset.FillAsync(email);
            await BoutonEnvoyerReset(langue).ClickAsync();
        }

        /// <summary>
        /// Retourne à la page de connexion après un reset
        /// </summary>
        public async Task RetourPageConnexion(string langue)
        {
            await BoutonRetourConnexion(langue).ClickAsync();
        }

        /// <summary>
        /// Clique sur "S'inscrire maintenant"
        /// </summary>
        public async Task CliquerInscrireMaintenant(string langue)
        {
            await BoutonInscrireMaintenant(langue).ClickAsync();
        }

        #endregion

        #region Assertions

        /// <summary>
        /// Vérifie que la page de connexion est affichée
        /// </summary>
        public async Task<bool> EstAffichee()
        {
            return await ChampEmail.IsVisibleAsync();
        }

        #endregion
    }
}
