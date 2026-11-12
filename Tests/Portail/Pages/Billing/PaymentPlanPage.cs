using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Page du plan de paiement du portail.
    /// Gère : sélection compte contrat, choix du plan, acceptation des conditions, envoi
    /// </summary>
    public class PaymentPlanPage
    {
        private readonly IPage _page;
        private readonly string _langue;

        public PaymentPlanPage(IPage page, string langue)
        {
            _page = page;
            _langue = langue;
        }

        #region Locators

        /// <summary>Onglet "Factures"</summary>
        private ILocator OngletFactures =>
            _page.GetByRole(AriaRole.Tab, new() { Name = ResourceHelper.GetStringPortail("Bills", _langue) });

        /// <summary>Bouton du compte contrat</summary>
        private ILocator BoutonCompteContrat(string numeroCompte) =>
            _page.GetByRole(AriaRole.Button, new() { Name = $"{ResourceHelper.GetStringPortail("CC", _langue)} {numeroCompte}" });

        /// <summary>Bouton "Paiement multiple"</summary>
        private ILocator BoutonPaiementMultiple =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("MultiplePaiement", _langue) });

        /// <summary>Bouton "Suivant"</summary>
        private ILocator BoutonSuivant =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("next", _langue) });

        /// <summary>Radio "6 mois"</summary>
        private ILocator Radio6Mois =>
            _page.GetByRole(AriaRole.Radio, new() { Name = ResourceHelper.GetStringPortail("6Months", _langue) });

        /// <summary>Checkbox "J'ai lu et j'accepte"</summary>
        private ILocator CheckboxAccepter =>
            _page.GetByRole(AriaRole.Checkbox, new() { Name = ResourceHelper.GetStringPortail("readAndAccept", _langue) });

        /// <summary>Bouton "Envoyer"</summary>
        private ILocator BoutonEnvoyer =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("send", _langue) });

        /// <summary>Bouton "Retour au résumé"</summary>
        private ILocator BoutonRetourResume =>
            _page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringPortail("BackToResume", _langue) });

        #endregion

        #region Actions

        /// <summary>Ouvre l'onglet Factures</summary>
        public async Task OuvrirOngletFactures() => await OngletFactures.ClickAsync();

        /// <summary>Sélectionne le compte contrat</summary>
        public async Task SelectionnerCompteContrat(string numeroCompte) => await BoutonCompteContrat(numeroCompte).ClickAsync();

        /// <summary>Clique sur "Paiement multiple"</summary>
        public async Task CliquerPaiementMultiple() => await BoutonPaiementMultiple.ClickAsync();

        /// <summary>Clique sur "Suivant"</summary>
        public async Task CliquerSuivant() => await BoutonSuivant.ClickAsync();

        /// <summary>Sélectionne le plan sur 6 mois</summary>
        public async Task Selectionner6Mois() => await Radio6Mois.CheckAsync();

        /// <summary>Accepte les conditions</summary>
        public async Task AccepterConditions() => await CheckboxAccepter.CheckAsync();

        /// <summary>Envoie le plan de paiement</summary>
        public async Task EnvoyerPlanDePaiement() => await BoutonEnvoyer.ClickAsync();

        /// <summary>Retourne au résumé</summary>
        public async Task RetournerAuResume() => await BoutonRetourResume.ClickAsync();

        /// <summary>
        /// Processus complet de création d'un plan de paiement
        /// </summary>
        public async Task CreerPlanDePaiement(string numeroCompteContrat)
        {
            await OuvrirOngletFactures();
            await SelectionnerCompteContrat(numeroCompteContrat);
            await CliquerPaiementMultiple();
            await CliquerSuivant();
            await Selectionner6Mois();
            await CliquerSuivant();
            await AccepterConditions();
            await EnvoyerPlanDePaiement();
            await RetournerAuResume();
        }

        #endregion
    }
}
