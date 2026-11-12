using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Données d'un compteur/produit lues dans l'onglet "Produits & contrats".
    /// </summary>
    public class ProduitContrat
    {
        public string Intitule { get; set; } = "";
        public string Details { get; set; } = "";
        public string? NumeroCompteur { get; set; }
        public bool? CompteurIntelligent { get; set; }
        public bool? PortP1Actif { get; set; }
        public bool? GarantieOrigineActive { get; set; }
        public bool Actif { get; set; } = true;
        public string Rue { get; set; } = "";
        public string NumRue { get; set; } = "";
        public string CodePostal { get; set; } = "";
        public string Ville { get; set; } = "";
    }

    /// <summary>
    /// Page "Produits & contrats" (URL /contract).
    /// Un panneau dépliable par lieu de consommation ; le corps (numéro de compteur,
    /// smart meter, port P1, garantie d'origine) est chargé à l'ouverture du panneau.
    /// </summary>
    public class ProduitsContratsPage
    {
        private readonly IPage _page;

        public ProduitsContratsPage(IPage page)
        {
            _page = page;
        }

        private string Url => Serializer.GetUrlPortailQual().TrimEnd('/') + "/contract";

        private ILocator Panneaux =>
            _page.Locator("mygre-contract-premise-list mat-expansion-panel");

        /// <summary>
        /// Navigue vers l'onglet Produits & contrats et attend la liste.
        /// </summary>
        public async Task Naviguer()
        {
            await _page.GotoAsync(Url);
            await Panneaux.First.WaitForAsync(new() { Timeout = 15000 });
        }

        /// <summary>
        /// Extrait un <see cref="ProduitContrat"/> par lieu de consommation.
        /// </summary>
        public async Task<List<ProduitContrat>> ExtraireProduits()
        {
            var resultats = new List<ProduitContrat>();
            var panneaux = await Panneaux.AllAsync();

            foreach (var panneau in panneaux)
            {
                var pc = new ProduitContrat();

                // Intitulé du produit (titre du panneau)
                try { pc.Intitule = (await panneau.Locator("mat-panel-title").First.InnerTextAsync()).Trim(); }
                catch { }

                // Actif / inactif (classe du panneau)
                try { pc.Actif = !((await panneau.GetAttributeAsync("class")) ?? "").Contains("inactive"); }
                catch { }

                // Adresse
                try
                {
                    var description = (await panneau.Locator("mat-panel-description").First.InnerTextAsync()).Trim();
                    var adresse = PortailParsing.ParseAdresse(description);
                    pc.Rue = adresse.rue;
                    pc.NumRue = adresse.num;
                    pc.CodePostal = adresse.cp;
                    pc.Ville = adresse.ville;
                }
                catch { }

                // Déplier le panneau pour charger le corps (lazy)
                var entete = panneau.Locator("mat-expansion-panel-header").First;
                if ((await entete.GetAttributeAsync("aria-expanded")) != "true")
                {
                    await entete.Locator(".mat-expansion-indicator").ClickAsync();
                }
                await Assertions.Expect(entete).ToHaveAttributeAsync("aria-expanded", "true",
                    new() { Timeout = 8000 });
                await panneau.Locator(".mat-expansion-panel-body").First.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 8000
                });

                pc.Details = (await panneau.Locator(".mat-expansion-panel-body").First.InnerTextAsync()).Trim();
                string corps = "";
                try { corps = await panneau.InnerTextAsync(); }
                catch { }

                // Numéro de compteur ("Numéro:  567578")
                var mNum = Regex.Match(corps, @"Num[ée]ro\s*:?\s*(\d+)");
                if (mNum.Success) pc.NumeroCompteur = mNum.Groups[1].Value;

                // Compteur intelligent ("Compteur intelligent: Oui")
                var mSmart = Regex.Match(corps, @"Compteur intelligent\s*:?\s*(Oui|Non)", RegexOptions.IgnoreCase);
                if (mSmart.Success)
                    pc.CompteurIntelligent = mSmart.Groups[1].Value.Equals("Oui", StringComparison.OrdinalIgnoreCase);

                // Port P1 ("P1 inactif" / "P1 actif")
                var mP1 = Regex.Match(corps, @"P1\s+(inactif|actif)", RegexOptions.IgnoreCase);
                if (mP1.Success)
                    pc.PortP1Actif = mP1.Groups[1].Value.Equals("actif", StringComparison.OrdinalIgnoreCase);
                else if (corps.Contains("Activer port P1", StringComparison.OrdinalIgnoreCase))
                    pc.PortP1Actif = false;

                // Garanties d'origines ("Garanties d'origines désactivé" / "activé")
                var mGo = Regex.Match(corps, @"Garanties? d'origines?\s+(d[eé]sactiv[ée]+|activ[ée]+)", RegexOptions.IgnoreCase);
                if (mGo.Success)
                    pc.GarantieOrigineActive = !mGo.Groups[1].Value.StartsWith("d", StringComparison.OrdinalIgnoreCase);

                resultats.Add(pc);
            }

            return resultats;
        }
    }
}
