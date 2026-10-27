using System.Text.RegularExpressions;

namespace TestPlayrights.Pages.Portail
{
    /// <summary>
    /// Utilitaires de parsing partagés par les page objects du portail
    /// (adresses, clé de rapprochement par lieu).
    /// </summary>
    public static class PortailParsing
    {
        /// <summary>
        /// Découpe une adresse brute en (rue, numéro, NPA, ville).
        /// Gère les formats "Rue 6\n1588 Ville" (Produits/Conso) et
        /// "Rue 6, 1588 Ville" (Factures). Les espaces insécables sont normalisés.
        /// </summary>
        public static (string rue, string num, string cp, string ville) ParseAdresse(string? brut)
        {
            var segments = (brut ?? "")
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Regex.Replace(s, @"\s+", " ").Trim())
                .Where(s => s.Length > 0)
                .ToList();

            string rue = "", num = "", cp = "", ville = "";

            if (segments.Count > 0)
            {
                // "Route de Champmartin 6" -> rue + numéro final
                var m = Regex.Match(segments[0], @"^(.*?)\s+(\d+\s*[a-zA-Z]?)$");
                if (m.Success)
                {
                    rue = m.Groups[1].Value.Trim();
                    num = Regex.Replace(m.Groups[2].Value, @"\s+", "");
                }
                else
                {
                    rue = segments[0];
                }
            }

            if (segments.Count > 1)
            {
                // "1588 Cudrefin" -> NPA + ville
                var m = Regex.Match(segments[1], @"^(\d{4,5})\s+(.*)$");
                if (m.Success)
                {
                    cp = m.Groups[1].Value;
                    ville = m.Groups[2].Value.Trim();
                }
                else
                {
                    ville = segments[1];
                }
            }

            return (rue, num, cp, ville);
        }

        /// <summary>
        /// Clé normalisée d'un lieu (rue + numéro + NPA) pour rapprocher
        /// les compteurs (Produits/Conso) avec les comptes de contrat (Factures).
        /// </summary>
        public static string CleLieu(string? rue, string? num, string? cp)
        {
            string brut = $"{rue} {num} {cp}".ToLowerInvariant();
            return Regex.Replace(brut, @"\s+", " ").Trim();
        }
    }
}
