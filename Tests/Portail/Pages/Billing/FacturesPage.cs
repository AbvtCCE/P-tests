using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// État de facturation d'un compte de contrat (onglet "Factures").
    /// </summary>
    public class FactureCompte
    {
        public string? CompteContrat { get; set; }
        public bool EnSuspens { get; set; }
        public bool BoutonsPaiementDisponibles { get; set; }
        public string Rue { get; set; } = "";
        public string NumRue { get; set; } = "";
        public string CodePostal { get; set; } = "";
        public string Ville { get; set; } = "";
    }

    /// <summary>
    /// Page "Factures" (URL /invoice) — lecture seule de l'état des comptes de contrat.
    /// Pour chaque compte : numéro, présence de facture en suspens (badge d'en-tête),
    /// et disponibilité des boutons de paiement (attribut disabled).
    /// </summary>
    public class FacturesPage
    {
        private readonly IPage _page;

        public FacturesPage(IPage page)
        {
            _page = page;
        }

        private string Url => Serializer.GetUrlPortailQual().TrimEnd('/') + "/invoice";

        private ILocator Panneaux =>
            _page.Locator("mygre-contract-account-list mat-expansion-panel");

        /// <summary>
        /// Navigue vers l'onglet Factures et attend la liste.
        /// </summary>
        public async Task Naviguer()
        {
            await _page.GotoAsync(Url);
            await Panneaux.First.WaitForAsync(new() { Timeout = 15000 });
        }

        /// <summary>
        /// Extrait l'état de facturation par compte de contrat.
        /// </summary>
        public async Task<List<FactureCompte>> ExtraireFactures(string langue)
        {
            var resultats = new List<FactureCompte>();
            string enSuspens = ResourceHelper.GetStringPortail("OpenBill", langue);   // "En suspens"
            string payerMaintenant = ResourceHelper.GetStringPortail("PayNow", langue); // "Payer maintenant"

            var panneaux = await Panneaux.AllAsync();

            foreach (var panneau in panneaux)
            {
                var fc = new FactureCompte();

                string entete = "";
                try { entete = (await panneau.Locator("mat-expansion-panel-header").First.InnerTextAsync()).Trim(); }
                catch { }

                // Numéro de compte de contrat
                var m = Regex.Match(entete, @"Compte de contrat\s+(\d+)");
                if (m.Success) fc.CompteContrat = m.Groups[1].Value;

                // Facture en suspens (badge dans l'en-tête)
                fc.EnSuspens = entete.Contains(enSuspens, StringComparison.OrdinalIgnoreCase);

                // Adresse
                try
                {
                    var desc = (await panneau.Locator("mat-panel-description").First.InnerTextAsync()).Trim();
                    var a = PortailParsing.ParseAdresse(desc);
                    fc.Rue = a.rue; fc.NumRue = a.num; fc.CodePostal = a.cp; fc.Ville = a.ville;
                }
                catch { }

                // Boutons de paiement disponibles (le bouton "Payer maintenant" n'est pas désactivé)
                try
                {
                    var bouton = panneau.GetByRole(AriaRole.Button, new() { Name = payerMaintenant }).First;
                    if (await bouton.CountAsync() > 0)
                        fc.BoutonsPaiementDisponibles = await bouton.IsEnabledAsync();
                }
                catch { }

                if (!string.IsNullOrEmpty(fc.CompteContrat))
                    resultats.Add(fc);
            }

            return resultats;
        }
    }
}
