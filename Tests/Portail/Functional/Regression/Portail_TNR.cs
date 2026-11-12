using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.Pages.Portail;

namespace TestPlayrights
{
    /// <summary>
    /// Tests de Non Regression (TNR) du Portail (my-q.groupe-e.ch).
    /// Scenarios end-to-end combinant plusieurs fonctionnalites.
    /// TODO : a completer une fois les tests fonctionnels stables.
    /// </summary>
    [TestFixture]
    [Category("Portail_TNR")]
    [Explicit("Squelettes TNR incomplets; à lancer uniquement pour leur reprise.")]
    public class Portail_TNR : TestBasePortail
    {
        #region Configuration

        /// <summary>Criteres : partenaire inscrit avec facture ouverte</summary>
        protected override Dictionary<string, string?> criteresSelection { get; set; } = new()
        {
            { "InscritSurLePortail", "true" },
            { "FactureOuverte", "true" }
        };

        #endregion

        #region TNR - Consultation consommation (TODO)

        /// <summary>
        /// E2E : Consulte la consommation sur le portail.
        /// TODO : a implementer - l'ancien test dependait du CRM qui a ete supprime.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task ConsulterConsommation(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "TNR Consultation consommation";
            descriptionTest = "Verification de l'affichage de la consommation";
            expectedResult = "Les donnees de consommation s'affichent";
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LoginAvecCriteres();
            await SelectionnerLangue();

            // TODO: implementer le test de consultation de consommation
            // L'ancien test utilisait le CRM pour verification croisee
            await DebugPause();

            Assert.Warn("Test TNR a implementer : consultation consommation");
        }

        #endregion

        #region TNR - Changement donnees et produit (TODO)

        /// <summary>
        /// E2E : Change les donnees du partenaire puis le produit.
        /// TODO : a implementer.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task ChangerDonneesEtProduit(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "TNR Changement donnees et produit";
            descriptionTest = "Changement des donnees partenaire puis du produit";
            expectedResult = "Les changements sont effectues";
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LoginAvecCriteres();
            await SelectionnerLangue();

            // TODO: implementer le test E2E
            // 1. Changer le numero de telephone
            // 2. Changer le produit
            // 3. Verifier les changements
            await DebugPause();

            Assert.Warn("Test TNR a implementer : changement donnees et produit");
        }

        #endregion
    }
}
