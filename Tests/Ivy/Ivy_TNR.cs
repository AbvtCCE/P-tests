using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    /// <summary>
    /// Tests de Non Regression (TNR) IVY (ivy-qua.cce24x7.net).
    /// Scenarios end-to-end combinant plusieurs services IVY.
    /// TODO : a completer une fois les tests fonctionnels stables.
    /// </summary>
    [TestFixture]
    [Category("IVY_TNR")]
    public class Ivy_TNR : TestBaseIvy
    {
        #region Configuration

        protected override Dictionary<string, string?> criteresSelection { get; set; } = new();

        #endregion

        #region TNR - Emmenagement puis modification (TODO)

        /// <summary>
        /// E2E : Cree un emmenagement, puis le modifie, puis verifie.
        /// TODO : a implementer.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task EmmenagementPuisModification(string navigateur, bool mobile, string lg)
        {
            testName = "TNR Emmenagement + modification";
            descriptionTest = "Cree un emmenagement puis le modifie";
            expectedResult = "L'emmenagement est cree et modifie";
            await Setup(navigateur, mobile, lg);

            await LoginIvy();
            await DebugPause();

            // TODO: implementer le test E2E
            // 1. Creer un emmenagement
            // 2. Modifier la date
            // 3. Verifier que la modification est prise en compte
            Assert.Warn("Test TNR a implementer : emmenagement puis modification");
        }

        #endregion

        #region TNR - Changement bancaire et facturation (TODO)

        /// <summary>
        /// E2E : Change l'IBAN, puis verifie avec une copie de facture.
        /// TODO : a implementer.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsIvy))]
        public async Task ChangementBancaireEtFacturation(string navigateur, bool mobile, string lg)
        {
            testName = "TNR Changement bancaire + facturation";
            descriptionTest = "Change l'IBAN puis verifie avec facturation";
            expectedResult = "L'IBAN est modifie et la facture est correcte";
            await Setup(navigateur, mobile, lg);

            await LoginIvy();
            await DebugPause();

            // TODO: implementer le test E2E
            // 1. Changer l'IBAN
            // 2. Copier une facture
            // 3. Verifier la coherence
            Assert.Warn("Test TNR a implementer : changement bancaire et facturation");
        }

        #endregion
    }
}
