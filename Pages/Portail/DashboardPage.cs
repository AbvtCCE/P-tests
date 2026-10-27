using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Page d'accueil / Dashboard du portail après connexion.
    /// Gère : navigation par onglets, sélection de langue, popups, déconnexion
    /// </summary>
    public class DashboardPage
    {
        private readonly IPage _page;

        public DashboardPage(IPage page)
        {
            _page = page;
        }

        #region Locators - Navigation

        /// <summary>Bouton hamburger menu mobile</summary>
        private ILocator BoutonMenuMobile =>
            _page.Locator("mygre-navigation").GetByRole(AriaRole.Button);

        /// <summary>Onglet "Mon compte"</summary>
        private ILocator OngletMonCompte(string langue) =>
            _page.GetByRole(AriaRole.Tab, new() { Name = ResourceHelper.GetStringPortail("MyAccount", langue) });

        /// <summary>Onglet "Factures"</summary>
        private ILocator OngletFactures(string langue) =>
            _page.GetByRole(AriaRole.Tab, new() { Name = ResourceHelper.GetStringPortail("Bills", langue) });

        /// <summary>Onglet "Produits & contrats"</summary>
        private ILocator OngletProduits(string langue) =>
            _page.GetByRole(AriaRole.Tab, new() { Name = ResourceHelper.GetStringPortail("ProductsContracts", langue) });

        /// <summary>Lien "Mes données"</summary>
        private ILocator LienMesDonnees(string langue) =>
            _page.GetByText(ResourceHelper.GetStringPortail("MyData", langue)).First;

        /// <summary>Lien "Contact"</summary>
        private ILocator LienContact(string langue) =>
            _page.GetByRole(AriaRole.Link, new() { Name = ResourceHelper.GetStringPortail("contact", langue) });

        /// <summary>Lien "Annoncer emménagement"</summary>
        private ILocator LienEmmenagement(string langue) =>
            _page.GetByRole(AriaRole.Link, new() { Name = ResourceHelper.GetStringPortail("InscritLoge", langue) });

        /// <summary>Lien "Annoncer déménagement"</summary>
        private ILocator LienDemenagement(string langue) =>
            _page.GetByRole(AriaRole.Link, new() { Name = ResourceHelper.GetStringPortail("annonceDemenagement", langue) });

        #endregion

        #region Locators - Langue

        /// <summary>Sélecteur de langue FR (header desktop)</summary>
        private ILocator BoutonLangueFrDesktop => _page.Locator("gfc-header").GetByText("FR");

        /// <summary>Sélecteur de langue DE (header desktop)</summary>
        private ILocator BoutonLangueDeDesktop => _page.Locator("gfc-header").GetByText("DE");

        /// <summary>Sélecteur de langue FR (menu mobile)</summary>
        private ILocator BoutonLangueFrMobile => _page.GetByText("FR");

        /// <summary>Sélecteur de langue DE (menu mobile)</summary>
        private ILocator BoutonLangueDeMobile => _page.GetByText("DE", new() { Exact = true });

        #endregion

        #region Locators - Popups et Déconnexion

        /// <summary>Popup "Information" avec bouton Fermer</summary>
        private ILocator PopupInformation =>
            _page.GetByRole(AriaRole.Heading, new() { Name = "Information" });

        /// <summary>Bouton "Fermer" du popup</summary>
        private ILocator BoutonFermerPopup =>
            _page.GetByRole(AriaRole.Button, new() { Name = "Fermer" });

        /// <summary>Bouton du nom utilisateur (pour déconnexion desktop)</summary>
        private ILocator BoutonUtilisateur(string prenom, string nom) =>
            _page.GetByRole(AriaRole.Button, new() { Name = $"{prenom} {nom}" });

        /// <summary>Menu item "Déconnexion"</summary>
        private ILocator MenuDeconnexion(string langue) =>
            _page.GetByRole(AriaRole.Menuitem, new() { Name = ResourceHelper.GetStringPortail("deco", langue) });

        /// <summary>Texte "Déconnexion" dans le menu mobile</summary>
        private ILocator TexteDeconnexionMobile(string langue) =>
            _page.GetByText(ResourceHelper.GetStringPortail("deco", langue));

        #endregion

        #region Locators - Contenu

        /// <summary>Message de bienvenue "Bonjour Prénom"</summary>
        private ILocator MessageBienvenue(string prenom, string langue) =>
            _page.GetByText($"{ResourceHelper.GetStringPortail("Welcome", langue)} {prenom}");

        /// <summary>En-tête de bienvenue "Bienvenue {prénom}" (sans connaître le prénom)</summary>
        private ILocator EnteteBienvenue(string langue) =>
            _page.GetByText(ResourceHelper.GetStringPortail("Welcome", langue)).First;

        /// <summary>Grille d'informations du partenaire</summary>
        private ILocator GrilleInfos => _page.Locator("mat-grid-list");

        #endregion

        #region Actions - Navigation

        /// <summary>
        /// Navigue vers la page d'accueil du portail
        /// </summary>
        public async Task Naviguer()
        {
            await _page.GotoAsync(Serializer.GetUrlPortailQual());
        }

        /// <summary>
        /// Navigue vers la page overview
        /// </summary>
        public async Task NaviguerOverview()
        {
            await _page.GotoAsync("https://my-q.groupe-e.ch/overview");
        }

        /// <summary>
        /// Ouvre le menu mobile (hamburger)
        /// </summary>
        public async Task OuvrirMenuMobile()
        {
            await BoutonMenuMobile.ClickAsync();
        }

        /// <summary>
        /// Clique sur l'onglet "Mon compte"
        /// </summary>
        public async Task AllerMonCompte(string langue, bool isMobile = false)
        {
            if (isMobile) await OuvrirMenuMobile();
            await OngletMonCompte(langue).ClickAsync();
        }

        /// <summary>
        /// Clique sur l'onglet "Factures"
        /// </summary>
        public async Task AllerFactures(string langue, bool isMobile = false)
        {
            if (isMobile) await OuvrirMenuMobile();
            await OngletFactures(langue).ClickAsync();
        }

        /// <summary>
        /// Clique sur l'onglet "Produits & contrats"
        /// </summary>
        public async Task AllerProduits(string langue, bool isMobile = false)
        {
            if (isMobile) await OuvrirMenuMobile();
            await OngletProduits(langue).ClickAsync();
        }

        /// <summary>
        /// Clique sur "Mes données"
        /// </summary>
        public async Task AllerMesDonnees(string langue)
        {
            await LienMesDonnees(langue).ClickAsync();
        }

        /// <summary>
        /// Clique sur le lien "Contact"
        /// </summary>
        public async Task AllerContact(string langue)
        {
            await LienContact(langue).ClickAsync();
        }

        /// <summary>
        /// Clique sur "Annoncer emménagement"
        /// </summary>
        public async Task AllerEmmenagement(string langue)
        {
            await LienEmmenagement(langue).ClickAsync();
        }

        /// <summary>
        /// Clique sur "Annoncer déménagement"
        /// </summary>
        public async Task AllerDemenagement(string langue)
        {
            await LienDemenagement(langue).ClickAsync();
        }

        #endregion

        #region Actions - Langue

        /// <summary>
        /// Sélectionne la langue sur le portail (gère mobile/desktop)
        /// </summary>
        public async Task SelectionnerLangue(string langue, bool isMobile = false)
        {
            if (isMobile)
            {
                await OuvrirMenuMobile();
                switch (langue)
                {
                    case "DE":
                        await BoutonLangueDeMobile.ClickAsync();
                        break;
                    case "FR":
                        await BoutonLangueFrMobile.ClickAsync();
                        break;
                }
                await OuvrirMenuMobile();
            }
            else
            {
                switch (langue)
                {
                    case "DE":
                        await BoutonLangueDeDesktop.ClickAsync();
                        break;
                    case "FR":
                        await BoutonLangueFrDesktop.ClickAsync();
                        break;
                }
            }
        }

        #endregion

        #region Actions - Popups

        /// <summary>
        /// Ferme le popup d'information s'il est visible
        /// </summary>
        public async Task FermerPopupSiPresent()
        {
            try
            {
                var popups = await PopupInformation.AllAsync();
                if (popups.Count > 0 && await BoutonFermerPopup.IsVisibleAsync())
                {
                    await BoutonFermerPopup.ClickAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Popup handling: {ex.Message}");
            }
        }

        #endregion

        #region Actions - Déconnexion

        /// <summary>
        /// Se déconnecte du portail (gère mobile/desktop)
        /// </summary>
        public async Task SeDeconnecter(string prenom, string nom, string langue, bool isMobile = false)
        {
            if (isMobile)
            {
                await OuvrirMenuMobile();
                await TexteDeconnexionMobile(langue).ClickAsync();
            }
            else
            {
                await BoutonUtilisateur(prenom, nom).ClickAsync();
                await MenuDeconnexion(langue).ClickAsync();
            }
        }

        #endregion

        #region Assertions

        /// <summary>
        /// Vérifie que le message de bienvenue est affiché.
        /// Le prénom est nettoyé (un éventuel "!" du message "Bienvenue X Y!" est ignoré).
        /// </summary>
        public async Task VerifierBienvenue(string prenom, string langue)
        {
            await MessageBienvenue(NettoyerValeur(prenom), langue).First.WaitForAsync();
        }

        /// <summary>
        /// Vérifie que la grille contient le prénom ET le nom, chacun vérifié INDÉPENDAMMENT
        /// (un éventuel "!" est retiré avant comparaison).
        /// </summary>
        public async Task VerifierInfosPartenaire(string prenom, string nom)
        {
            await Assertions.Expect(GrilleInfos).ToContainTextAsync(NettoyerValeur(prenom));
            await Assertions.Expect(GrilleInfos).ToContainTextAsync(NettoyerValeur(nom));
        }

        /// <summary>Retire un éventuel "!" final et les espaces superflus d'une valeur.</summary>
        private static string NettoyerValeur(string? valeur) =>
            (valeur ?? "").Replace("!", "").Trim();

        #endregion

        #region Lecture (synchronisation CSV)

        /// <summary>
        /// Lit le texte complet de l'en-tête de bienvenue (ex. "Bienvenue {prénom}").
        /// Utile pour extraire le prénom affiché sans le connaître à l'avance.
        /// </summary>
        public async Task<string> LireTexteBienvenue(string langue)
        {
            await EnteteBienvenue(langue).WaitForAsync(new() { Timeout = 10000 });
            return (await EnteteBienvenue(langue).InnerTextAsync()).Trim();
        }

        /// <summary>
        /// Lit le texte brut de la grille d'informations du partenaire (onglet "Mon compte").
        /// Le contenu (labels/valeurs) est ensuite parsé par l'appelant.
        /// </summary>
        public async Task<string> LireTexteGrille()
        {
            await GrilleInfos.First.WaitForAsync(new() { Timeout = 10000 });
            return (await GrilleInfos.First.InnerTextAsync()).Trim();
        }

        #endregion
    }
}
