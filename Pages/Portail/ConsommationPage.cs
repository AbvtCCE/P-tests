using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Données d'un compteur lues dans l'onglet "Consommation".
    /// </summary>
    public class CompteurConso
    {
        public string? NumeroCompteur { get; set; }
        public string Intitule { get; set; } = "";
        public bool Actif { get; set; } = true;
        public string Rue { get; set; } = "";
        public string NumRue { get; set; } = "";
        public string CodePostal { get; set; } = "";
        public string Ville { get; set; } = "";
    }

    /// <summary>
    /// Page "Consommation" (URL /energy).
    /// Un panneau par compteur ; le numéro de compteur et l'état actif/inactif
    /// sont lisibles dans l'en-tête (pas besoin de déplier).
    /// </summary>
    public class ConsommationPage
    {
        private readonly IPage _page;

        public ConsommationPage(IPage page)
        {
            _page = page;
        }

        private string Url => Serializer.GetUrlPortailQual().TrimEnd('/') + "/energy";

        private ILocator Panneaux =>
            _page.Locator("mygre-energy-premise-list mat-expansion-panel");

        /// <summary>
        /// Navigue vers l'onglet Consommation et attend la liste.
        /// </summary>
        public async Task Naviguer()
        {
            await _page.GotoAsync(Url);
            await Panneaux.First.WaitForAsync(new() { Timeout = 15000 });
        }

        /// <summary>
        /// Extrait un <see cref="CompteurConso"/> par compteur affiché.
        /// </summary>
        public async Task<List<CompteurConso>> ExtraireCompteurs()
        {
            var resultats = new List<CompteurConso>();
            var panneaux = await Panneaux.AllAsync();

            foreach (var panneau in panneaux)
            {
                var cc = new CompteurConso();

                string titre = "";
                try { titre = (await panneau.Locator("mat-panel-title").First.InnerTextAsync()).Trim(); }
                catch { }

                // "Compteur 567578 - Rural + Solaire 16 kVA"
                var m = Regex.Match(titre, @"Compteur\s+(\d+)\s*-\s*(.*)$", RegexOptions.Singleline);
                if (m.Success)
                {
                    cc.NumeroCompteur = m.Groups[1].Value;
                    cc.Intitule = Regex.Replace(m.Groups[2].Value, @"\s+", " ").Trim();
                }
                else
                {
                    cc.Intitule = Regex.Replace(titre, @"\s+", " ").Trim();
                }

                // Actif / inactif (classe du panneau)
                try { cc.Actif = !((await panneau.GetAttributeAsync("class")) ?? "").Contains("inactive"); }
                catch { }

                // Adresse
                try
                {
                    var desc = (await panneau.Locator("mat-panel-description").First.InnerTextAsync()).Trim();
                    var a = PortailParsing.ParseAdresse(desc);
                    cc.Rue = a.rue; cc.NumRue = a.num; cc.CodePostal = a.cp; cc.Ville = a.ville;
                }
                catch { }

                if (!string.IsNullOrEmpty(cc.NumeroCompteur))
                    resultats.Add(cc);
            }

            return resultats;
        }
    }
}
