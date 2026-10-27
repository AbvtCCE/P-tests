using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    /// <summary>
    /// Tests utilitaires pour construire les Page Objects.
    /// Chaque test ouvre l'application cible (Ivy ou Portail), effectue le login,
    /// puis met en pause via page.PauseAsync() pour permettre l'inspection
    /// des locators via le Playwright Inspector.
    ///
    /// Usage :
    ///   - Lancer un seul test depuis l'explorateur de tests.
    ///   - Le navigateur reste ouvert et l'Inspector permet de capturer les locators.
    /// </summary>
    [TestFixture]
    [Category("ConstructionsPages")]
    public class ConstructionsPages : TestBase
    {
        [Test]
        public async Task ConstructionIvy()
        {
            testName = "ConstructionIvy";
            await Setup("chromium", false, "FR");

            var p = DataLoader.FindClientWithCriteria(
                tousLesPartenaires, new Dictionary<string, string?>())!;

            await TestTools.LoginQual(
                page!, Serializer.EmailIvy, Serializer.MdpIvy, true, langue!);
            await TestTools.IvySearch(page!, p.NumeroPartenaire, langue!);

            // Verifications du header centralisees dans le Page Object
            var header = new ComponentsHeaderDashIvy(page!);
            await header.VerifierToutLeHeaderAsync(p, langue!);

            await page!.PauseAsync();
        }

        [Test]
        public async Task ConstructionPortail()
        {
            testName = "ConstructionPortail";
            await Setup("chromium", false, "FR");

            var partenaire = DataLoader.FindClientWithCriteria(
                tousLesPartenaires, new Dictionary<string, string?>())!;

            await TestTools.LogWithoutCriteria(page!, partenaire, Report!, testName!);

            await page!.PauseAsync();
        }
    }
}
