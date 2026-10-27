using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Page des factures du portail.
    /// Gère : sélection compte contrat, téléchargement factures, historique
    /// </summary>
    public class BillsPage
    {
        private readonly IPage _page;

        public BillsPage(IPage page)
        {
            _page = page;
        }

        #region Locators

        /// <summary>Onglet "Factures"</summary>
        private ILocator OngletFactures(string langue) =>
            _page.GetByRole(AriaRole.Tab, new() { Name = ResourceHelper.GetStringPortail("Bills", langue) });

        /// <summary>Tous les boutons "Compte contrat"</summary>
        private ILocator BoutonsCompteContrat(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("CC", langue) });

        /// <summary>Bouton d'un compte contrat spécifique</summary>
        private ILocator BoutonCompteContratSpecifique(string langue, string numeroCompte) =>
            _page.GetByRole(AriaRole.Button, new() { Name = $"{ResourceHelper.GetStringPortail("CC", langue)} {numeroCompte}" });

        /// <summary>Ligne de facture ouverte avec bouton téléchargement</summary>
        private ILocator LignesFacturesOuvertes(string langue) =>
            _page.GetByRole(AriaRole.Row, new() { Name = ResourceHelper.GetStringPortail("OpenBill", langue) }).GetByRole(AriaRole.Button);

        /// <summary>Bouton télécharger facture par index (1-based)</summary>
        private ILocator BoutonTelechargerFacture(string langue, int index) =>
            _page.GetByRole(AriaRole.Row, new() { Name = $"{ResourceHelper.GetStringPortail("OpenBill", langue)} {index}" }).GetByRole(AriaRole.Button);

        /// <summary>Bouton télécharger le total</summary>
        private ILocator BoutonTelechargerTotal(string langue) =>
            _page.GetByRole(AriaRole.Row, new() { Name = ResourceHelper.GetStringPortail("TotalAmount", langue) }).GetByRole(AriaRole.Button);

        /// <summary>Bouton toggle pour voir l'historique</summary>
        private ILocator BoutonToggleHistorique(string langue, string numeroCompte) =>
            _page.GetByLabel($"{ResourceHelper.GetStringPortail("ContractAccount", langue)} {numeroCompte}")
                 .Locator("gfc-toggle-button").GetByRole(AriaRole.Button);

        /// <summary>Bouton "Paiement multiple" (plan de paiement)</summary>
        private ILocator BoutonPaiementMultiple(string langue) =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("MultiplePaiement", langue) });

        #endregion

        #region Actions

        /// <summary>
        /// Ouvre l'onglet Factures
        /// </summary>
        public async Task OuvrirOngletFactures(string langue)
        {
            await OngletFactures(langue).ClickAsync();
        }

        /// <summary>
        /// Sélectionne un compte contrat spécifique.
        /// Si un seul compte existe, ne fait rien.
        /// </summary>
        public async Task SelectionnerCompteContrat(string langue, string numeroCompte)
        {
            await BoutonsCompteContrat(langue).Last.WaitForAsync();
            var tousLesBoutons = await BoutonsCompteContrat(langue).AllAsync();
            if (tousLesBoutons.Count > 1)
            {
                await BoutonCompteContratSpecifique(langue, numeroCompte).ClickAsync();
            }
        }

        /// <summary>
        /// Retourne le nombre de factures ouvertes
        /// </summary>
        public async Task<int> CompterFacturesOuvertes(string langue)
        {
            var factures = await LignesFacturesOuvertes(langue).AllAsync();
            return factures.Count;
        }

        /// <summary>
        /// Télécharge une facture par son index (1-based).
        /// Retourne l'objet Download pour sauvegarder le fichier.
        /// </summary>
        public async Task<IDownload> TelechargerFacture(string langue, int index)
        {
            var download = await _page.RunAndWaitForDownloadAsync(async () =>
            {
                await BoutonTelechargerFacture(langue, index).ClickAsync();
            });
            return download;
        }

        /// <summary>
        /// Télécharge le récapitulatif total
        /// </summary>
        public async Task<IDownload> TelechargerTotal(string langue)
        {
            var download = await _page.RunAndWaitForDownloadAsync(async () =>
            {
                await BoutonTelechargerTotal(langue).ClickAsync();
            });
            return download;
        }

        /// <summary>
        /// Affiche l'historique des factures pour un compte contrat
        /// </summary>
        public async Task AfficherHistorique(string langue, string numeroCompte)
        {
            await BoutonToggleHistorique(langue, numeroCompte).ClickAsync();
        }

        /// <summary>
        /// Clique sur "Paiement multiple" pour lancer un plan de paiement
        /// </summary>
        public async Task CliquerPaiementMultiple(string langue)
        {
            await BoutonPaiementMultiple(langue).ClickAsync();
        }

        #endregion

        #region Assertions

        /// <summary>
        /// Vérifie que l'historique contient des factures
        /// </summary>
        public async Task<int> CompterFacturesHistorique(string langue)
        {
            var lignesPayees = await _page.Locator($"tr:has-text('{ResourceHelper.GetStringPortail("Bill", langue)}')").AllAsync();
            return lignesPayees.Count;
        }

        #endregion
    }
}
