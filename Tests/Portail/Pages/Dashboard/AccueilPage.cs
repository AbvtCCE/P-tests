using Microsoft.Playwright;

namespace TestPlayrights.PortailV2Copilot.Pages
{
    /// <summary>
    /// Vérifications de la page d'accueil affichée après connexion au Portail.
    /// </summary>
    public sealed class AccueilPage
    {
        private readonly IPage _page;

        private ILocator BoutonFermerPopup(string langue) =>
            _page.GetByRole(AriaRole.Button, new()
            {
                Name = ResourceHelper.GetStringPortail("close", langue)
            });

        public AccueilPage(IPage page)
        {
            _page = page;
        }

        public async Task<bool> DetecterPopupFactureEmailAsync()
        {
            var popupFacture = _page.Locator("mygre-pdf-push");
            try
            {
                await popupFacture.First.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 2_500
                });
            }
            catch (TimeoutException)
            {
            }

            return await popupFacture.CountAsync() > 0 && await popupFacture.First.IsVisibleAsync();
        }

        public async Task FermerPopupInformationSiPresentAsync()
        {
            foreach (var langue in new[] { "FR", "DE" })
            {
                var boutonFermer = BoutonFermerPopup(langue);
                if (await boutonFermer.IsVisibleAsync())
                {
                    await boutonFermer.ClickAsync();
                    return;
                }
            }
        }

        public async Task VerifierConnexionReussieAsync()
        {
            await _page.Locator("[role='tab']").First.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30_000
            });
        }

        public async Task OuvrirProfilAsync(string langue)
        {
            await _page.GetByText(ResourceHelper.GetStringPortail("MyData", langue)).First.ClickAsync();
        }

        public async Task<string> LireNomCompletAsync(string langue)
        {
            var messageBienvenue = _page.GetByText(ResourceHelper.GetStringPortail("Welcome", langue)).First;
            await messageBienvenue.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            return (await messageBienvenue.InnerTextAsync()).Trim();
        }
    }
}
