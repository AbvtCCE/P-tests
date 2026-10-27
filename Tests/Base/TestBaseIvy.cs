using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    /// <summary>
    /// Classe de base pour les tests IVY (ivy-qua.cce24x7.net).
    /// Hérite de TestBase et ajoute :
    /// - Login SSO IVY (Windows/Entra)
    /// - Navigation vers le dashboard partenaire
    ///
    /// Les Pages Objects IVY seront ajoutées ultérieurement
    /// (nécessite l'inspecteur Playwright pour récupérer les locators).
    ///
    /// Combinaisons testées : langue uniquement (FR/DE/EN)
    /// </summary>
    [TestFixture]
    [Category("IVY")]
    public abstract class TestBaseIvy : TestBase
    {
        #region Login IVY

        /// <summary>
        /// Connecte sur IVY via SSO et navigue vers le dashboard du partenaire.
        /// </summary>
        protected async Task LoginEtAllerAuDashboard(Partenaire partenaire)
        {
            await TestTools.GoToDashboardIvy(page!, langue!, Report!, testName!, partenaire);
        }

        /// <summary>
        /// Connecte sur IVY via SSO uniquement (sans naviguer vers un partenaire)
        /// </summary>
        protected async Task LoginIvy()
        {
            await TestTools.LoginQual(
                page!, Serializer.EmailIvy, Serializer.MdpIvy, true, langue!);
        }

        /// <summary>
        /// Recherche un partenaire dans IVY
        /// </summary>
        protected async Task RechercherPartenaire(Partenaire partenaire)
        {
            await TestTools.IvySearch(page!, partenaire.NumeroPartenaire, langue!);
        }

        /// <summary>
        /// Sélectionne un service IVY par catégorie et type
        /// </summary>
        protected async Task SelectionnerService(TestTools.Categories categorie, TestTools.Services service)
        {
            await TestTools.SelectServiceIvy(page!, categorie, service, langue!);
        }

        #endregion

        #region Génération des combinaisons de tests

        /// <summary>
        /// Génère les combinaisons de langue pour les tests IVY.
        /// IVY ne teste que les langues (pas de multi-navigateur, pas de mobile).
        /// </summary>
        public static IEnumerable<TestCaseData> GenererCombinaisonsIvy()
        {
            bool lngFr = GetEnvBool("LNG_FR", true);
            bool lngDe = GetEnvBool("LNG_DE", true);
            bool lngEn = GetEnvBool("LNG_EN", false);

            var langues = new List<string>();
            if (lngFr) langues.Add("FR");
            if (lngDe) langues.Add("DE");
            if (lngEn) langues.Add("EN");
            if (langues.Count == 0) langues.Add("FR");

            foreach (var lg in langues)
            {
                yield return new TestCaseData("chromium", false, lg)
                    .SetArgDisplayNames($"IVY_{lg}");
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
