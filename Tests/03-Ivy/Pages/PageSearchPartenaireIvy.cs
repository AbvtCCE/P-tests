using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TestPlayrights
{
    public class PageSearchPartenaireIvy 
  {
      private readonly IPage Page;

        public PageSearchPartenaireIvy(IPage page) => Page = page;

        // =========================
        // Locators
        // =========================

        private ILocator NumeroPartenaireInput(string lg) =>
            Page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("Npart", lg) });

        // (Les autres champs sont commentés dans ton snippet — on les laisse prêts si besoin)
        private ILocator NumeroCompteCompteurInput(string lg) =>
            Page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("numCompteCompteur", lg) });
        private ILocator NumeroFactureInput(string lg) =>
            Page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("numFact", lg) });
        private ILocator NumeroLCInput(string lg) =>
            Page.GetByRole(AriaRole.Textbox, new() { Name = ResourceHelper.GetStringIvy("numLc", lg) });
        private ILocator NumeroCompteurInput =>
            Page.GetByRole(AriaRole.Textbox, new() { Name = "Numéro de compteur" });
        private ILocator PointDeConsommationInput =>
            Page.GetByRole(AriaRole.Textbox, new() { Name = "Point de consommation" });

        private ILocator SearchButton(string lg) =>
            Page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("search", lg) }).Last;

        private ILocator SelectionPersonButton(string lg) =>
            Page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("SelectionPerson", lg) });

        private ILocator ChoosePartenaireButton(string lg) =>
            Page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("ChoosePartenaire", lg) });

        private ILocator EndButtons(string lg) =>
            Page.GetByRole(AriaRole.Button, new() { Name = ResourceHelper.GetStringIvy("End", lg) });

        // =========================
        // Méthodes d’action unitaires
        // =========================

        public async Task EnterNumeroPartenaireAsync(int? numPartenaire, string lg)
        {
            await NumeroPartenaireInput(lg).FillAsync(numPartenaire?.ToString() ?? string.Empty);
        }

        public async Task ClickSearchAsync(string lg)
        {
            await SearchButton(lg).ClickAsync();
        }

        public async Task ClickSelectionPersonAsync(string lg)
        {
            await SelectionPersonButton(lg).ClickAsync();
        }

        /// <summary>
        /// Clique sur "Choisir partenaire", attend l’ouverture du popup, puis le ferme.
        /// </summary>
        public async Task ChoosePartenaireInPopupAsync(string lg)
        {
            var popup = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await ChoosePartenaireButton(lg).ClickAsync();
            });

            if (popup is not null && !popup.IsClosed)
                await popup.CloseAsync();
        }

        /// <summary>
        /// Si plusieurs boutons "Terminer" sont présents, clique sur le dernier (comme dans ton code).
        /// </summary>
        public async Task ClickEndIfNeededAsync(string lg)
        {
            var all = await EndButtons(lg).AllAsync();
            if (all.Count > 1)
                await EndButtons(lg).Last.ClickAsync();
        }

        // =========================
        // Méthode composée (rejoue IvySearch)
        // =========================

        /// <summary>
        /// Rejoue exactement le flux IvySearch(...) avec uniquement le N° partenaire.
        /// </summary>
        public async Task SearchByPartenaireAsync(int? numPartenaire, string lg)
        {
            // Log équivalent
            System.Console.WriteLine($"le numéro du partenaire est:{numPartenaire}");

            await EnterNumeroPartenaireAsync(numPartenaire, lg);

            // Les autres champs étaient commentés dans ton snippet. 
            // (Décommente et appelle si nécessaire.)
            // await NumeroCompteCompteurInput(lg).FillAsync(numCC?.ToString() ?? "");
            // await NumeroFactureInput(lg).FillAsync(numFact?.ToString() ?? "");
            // await NumeroLCInput(lg).FillAsync(numLC?.ToString() ?? "");
            // await NumeroCompteurInput.FillAsync(numCompteur?.ToString() ?? "");
            // await PointDeConsommationInput.FillAsync(pointConsomation?.ToString() ?? "");

            await ClickSearchAsync(lg);
            await ClickSelectionPersonAsync(lg);
            await ChoosePartenaireInPopupAsync(lg);
            await ClickEndIfNeededAsync(lg);
        }
    }



  }
