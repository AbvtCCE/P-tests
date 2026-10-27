using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    /// <summary>
    /// Classe de base pour les tests SN Nestor (nestor.groupe-e.ch).
    /// Hérite de TestBase et ajoute :
    /// - Navigation vers le dashboard Nestor
    ///
    /// Combinaisons testées : langue uniquement (FR/DE)
    /// </summary>
    [TestFixture]
    [Category("NESTOR")]
    public abstract class TestBaseNestor : TestBase
    {
        #region Navigation Nestor

        /// <summary>
        /// Navigue vers la page d'accueil Nestor
        /// </summary>
        protected async Task OuvrirNestor()
        {
            await page!.GotoAsync(Serializer.GetUrlNestor(), new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load
            });
        }

        #endregion

        #region Génération des combinaisons de tests

        /// <summary>
        /// Génère les combinaisons de langue pour les tests Nestor.
        /// Nestor ne teste que les langues (pas de multi-navigateur, pas de mobile).
        /// </summary>
        public static IEnumerable<TestCaseData> GenererCombinaisonsNestor()
        {
            bool lngFr = GetEnvBool("LNG_FR", true);
            bool lngDe = GetEnvBool("LNG_DE", true);

            var langues = new List<string>();
            if (lngFr) langues.Add("FR");
            if (lngDe) langues.Add("DE");
            if (langues.Count == 0) langues.Add("FR");

            foreach (var lg in langues)
            {
                yield return new TestCaseData("chromium", false, lg)
                    .SetArgDisplayNames($"Nestor_{lg}");
            }
        }

        /// <summary>
        /// Lit une variable d'environnement booléenne avec valeur par défaut
        /// </summary>
        protected static bool GetEnvBool(string nomVariable, bool valeurParDefaut)
        {
            var valeur = Environment.GetEnvironmentVariable(nomVariable);
            if (string.IsNullOrEmpty(valeur)) return valeurParDefaut;
            return valeur.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
