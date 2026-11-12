using Microsoft.Playwright;

namespace TestPlayrights.PortailV2Copilot.Pages
{
    /// <summary>
    /// Interactions avec la page de connexion du Portail.
    /// </summary>
    public sealed class ConnexionPage
    {
        private readonly IPage _page;
        private readonly string _langue;

        private ILocator ChampEmail => _page.Locator("input[name='username']");
        private ILocator BoutonSuivant =>
            _page.GetByRole(AriaRole.Button, new()
            {
                Name = ResourceHelper.GetStringPortail("next", _langue)
            });
        private ILocator ChampMotDePasse => _page.Locator("input[name='password']");
        private ILocator BoutonConnexion => _page.Locator("button[type='submit']");

        public ConnexionPage(IPage page, string langue)
        {
            _page = page;
            _langue = langue;
        }

        public async Task NaviguerAsync()
        {
            await _page.GotoAsync(Serializer.GetUrlPortailQual());
            await TestTools.SelectLanguageWithoutBeingConnected(_page, _langue);
            await ChampEmail.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }

        public async Task SeConnecterAsync(string email, string motDePasse)
        {
            await ChampEmail.FillAsync(email);
            await BoutonSuivant.ClickAsync();
            await ChampMotDePasse.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await ChampMotDePasse.FillAsync(motDePasse);
            await BoutonConnexion.ClickAsync();
        }
    }
}
