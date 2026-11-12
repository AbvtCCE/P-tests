using NUnit.Framework;

namespace TestPlayrights
{
    [TestFixture]
    [Category("IVY_TOOLS")]
    [Explicit("Outil de maintenance : reprend puis termine les interactions IVY encore ouvertes.")]
    public class NettoyerInteractionsOuvertesIvy : TestBaseIvy
    {
        [Test]
        public async Task FermerToutesLesInteractionsOuvertes()
        {
            testName = "Fermer toutes les interactions IVY ouvertes";
            await Setup("chromium", false, "FR");

            var interactionsFermees = await TestTools.LoginIvyQual(page!, "FR");
            TestContext.WriteLine($"Interactions IVY terminées : {interactionsFermees}");
        }
    }
}
