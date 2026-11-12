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

        /// <summary>Lignes détaillées de factures ouvertes</summary>
        private ILocator LignesFactures() =>
            _page.Locator(
                    "mygre-contract-account-list mat-expansion-panel:has(mat-expansion-panel-header[aria-expanded='true'])")
                .GetByRole(AriaRole.Table)
                .GetByRole(AriaRole.Row);

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
        /// Retourne les numéros des factures ouvertes dont le bouton de téléchargement est utilisable.
        /// Les numéros correspondent à leur position parmi les factures ouvertes affichées.
        /// </summary>
        public async Task<List<int>> ListerIndicesFacturesTelechargeables(string langue)
        {
            var boutons = await BoutonsFacturesTelechargeablesAsync(langue);
            return Enumerable.Range(1, boutons.Count).ToList();
        }

        private async Task<List<ILocator>> BoutonsFacturesTelechargeablesAsync(string langue)
        {
            var lignes = LignesFactures();
            var nombreLignes = await lignes.CountAsync();
            var boutonsTelechargeables = new List<ILocator>();
            var texteFacture = ResourceHelper.GetStringPortail("Bill", langue);
            var statutFacturePayee = ResourceHelper.GetStringPortail("PaidBillStatus", langue);

            for (var index = 0; index < nombreLignes; index++)
            {
                var ligne = lignes.Nth(index);
                if (!await ligne.IsVisibleAsync())
                {
                    continue;
                }

                var texteLigne = await ligne.InnerTextAsync();
                if (!texteLigne.Contains(texteFacture, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var boutons = await ligne.GetByRole(AriaRole.Button).AllAsync();
                var celluleStatut = ligne.GetByRole(AriaRole.Cell).First;
                var statut = await celluleStatut.CountAsync() > 0
                    ? (await celluleStatut.InnerTextAsync()).Trim()
                    : "";
                if (string.Equals(statut, statutFacturePayee, StringComparison.OrdinalIgnoreCase) ||
                    boutons.Count != 1)
                {
                    continue;
                }

                var bouton = boutons[0];
                if (await bouton.IsVisibleAsync() && await bouton.IsEnabledAsync())
                {
                    boutonsTelechargeables.Add(bouton);
                }
            }

            return boutonsTelechargeables;
        }

        /// <summary>
        /// Retourne le nombre de comptes de contrat affichés.
        /// </summary>
        public async Task<int> CompterComptesContrat(string langue)
        {
            var comptes = await BoutonsCompteContrat(langue).AllAsync();
            return comptes.Count;
        }

        /// <summary>
        /// Télécharge une facture par son index (1-based).
        /// Retourne l'objet Download pour sauvegarder le fichier.
        /// </summary>
        public async Task<IDownload> TelechargerFacture(string langue, int index)
        {
            var boutons = await BoutonsFacturesTelechargeablesAsync(langue);
            if (index < 1 || index > boutons.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Aucune facture téléchargeable à la position {index}.");
            }

            var download = await _page.RunAndWaitForDownloadAsync(async () =>
            {
                await boutons[index - 1].ClickAsync();
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
