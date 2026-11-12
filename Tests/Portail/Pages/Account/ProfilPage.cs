using Microsoft.Playwright;

namespace TestPlayrights.PortailV2Copilot.Pages
{
    /// <summary>
    /// Lecture des coordonnées affichées dans la page profil du Portail.
    /// Aucune valeur n'est modifiée ni sauvegardée sur le Portail.
    /// </summary>
    public sealed class ProfilPage
    {
        private readonly IPage _page;
        private readonly string _langue;

        private ILocator LienModifierCorrespondance =>
            _page.Locator("gfc-loading-overlay")
                .Filter(new()
                {
                    HasText = ResourceHelper.GetStringPortail("Corespondance", _langue)
                })
                .Locator("a");

        private ILocator ChampEmail =>
            _page.GetByRole(AriaRole.Textbox, new()
            {
                Name = ResourceHelper.GetStringPortail("textbox_email", _langue)
            });

        private ILocator ChampPrenom =>
            _page.GetByRole(AriaRole.Textbox, new()
            {
                Name = ResourceHelper.GetStringPortail("name", _langue)
            });

        private ILocator ChampNom =>
            _page.GetByRole(AriaRole.Textbox, new()
            {
                Name = ResourceHelper.GetStringPortail("lastname", _langue)
            });

        private ILocator ChampTelephone =>
            _page.GetByRole(AriaRole.Textbox, new()
            {
                Name = ResourceHelper.GetStringPortail("mobilePhone", _langue)
            });

        private ILocator ChampCodePostal =>
            _page.GetByRole(AriaRole.Combobox, new()
            {
                Name = ResourceHelper.GetStringPortail("NPA", _langue)
            });

        private ILocator ChampRue =>
            _page.GetByRole(AriaRole.Combobox, new()
            {
                Name = ResourceHelper.GetStringPortail("rue", _langue)
            });

        private ILocator ChampNumeroRue =>
            _page.GetByRole(AriaRole.Searchbox, new()
            {
                Name = ResourceHelper.GetStringPortail("number", _langue)
            });

        public ProfilPage(IPage page, string langue)
        {
            _page = page;
            _langue = langue;
        }

        public async Task<bool> OuvrirCorrespondanceSiDisponibleAsync()
        {
            try
            {
                await LienModifierCorrespondance.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5_000
                });
            }
            catch (TimeoutException)
            {
                return false;
            }

            await LienModifierCorrespondance.ClickAsync();
            await ChampTelephone.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            return true;
        }

        public async Task<Dictionary<string, string>> LireCoordonneesAsync()
        {
            var coordonnees = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            await AjouterValeurSiVisibleAsync(coordonnees, "Prenom", ChampPrenom);
            await AjouterValeurSiVisibleAsync(coordonnees, "Nom", ChampNom);
            await AjouterValeurSiVisibleAsync(coordonnees, "EmailCorrespondance", ChampEmail);
            await AjouterValeurSiVisibleAsync(coordonnees, "Mobile", ChampTelephone);
            await AjouterValeurSiVisibleAsync(coordonnees, "CodePostal", ChampCodePostal);
            await AjouterValeurSiVisibleAsync(coordonnees, "Rue", ChampRue);
            await AjouterValeurSiVisibleAsync(coordonnees, "NumRue", ChampNumeroRue);

            return coordonnees;
        }

        private static async Task AjouterValeurSiVisibleAsync(
            IDictionary<string, string> valeurs,
            string colonne,
            ILocator champ)
        {
            if (await champ.CountAsync() == 0 || !await champ.First.IsVisibleAsync())
            {
                return;
            }

            var valeur = (await champ.First.InputValueAsync()).Trim();
            if (!string.IsNullOrWhiteSpace(valeur))
            {
                valeurs[colonne] = valeur;
            }
        }
    }
}
