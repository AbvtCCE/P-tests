using System.Text;
using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.Pages.Portail;

namespace TestPlayrights
{
    /// <summary>
    /// Test de synchronisation des donnees du Portail (my-q.groupe-e.ch) avec le CSV Partenaires.
    ///
    /// Pour CHAQUE ligne traitee :
    ///   1. Tente de se connecter (session navigateur isolee : nouveau contexte par partenaire).
    ///   2. Si la connexion echoue (toujours sur la page de connexion) -> InscritSurLePortail = False.
    ///   3. Si la connexion reussit -> InscritSurLePortail = True, puis lecture du nom affiche
    ///      (en-tete "Bienvenue {prenom} {nom}!") et mise a jour de Prenom / Nom de la ligne CSV,
    ///      chacun stocke INDEPENDAMMENT (le "!" final est retire).
    ///
    /// Deux tests :
    ///   - SynchroniserInscriptionPortail : balaye TOUT le CSV.
    ///   - SynchroniserInscrits           : ne parcourt que les InscritSurLePortail=true.
    ///
    /// Le CSV est mis a jour de maniere chirurgicale (seules les cellules concernees changent,
    /// l'en-tete et les autres colonnes sont preservees a l'identique).
    ///
    /// Execution : Chrome desktop FR (test de donnees, pas multi-navigateur).
    /// </summary>
    [TestFixture]
    [Category("Portail_Sync")]
    public class Portail_SyncInscription : TestBasePortail
    {
        #region Configuration

        /// <summary>Langue utilisee pour lire l'en-tete de bienvenue.</summary>
        private const string LangueSync = "FR";

        /// <summary>Rapport partage (memes schema que le smoke).</summary>
        protected override bool IsSharedReport() => true;

        // Compteurs de synthese (cumulatifs sur la fixture)
        private int _lignesTraitees;
        private int _connectes;
        private int _nonConnectes;
        private int _erreurs;

        #endregion

        #region Setup / Teardown

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var reportsBase = Environment.GetEnvironmentVariable("REPORTS_PATH") ?? Serializer.pathToReportFolder;
            var workflowName = Environment.GetEnvironmentVariable("WORKFLOW_NAME") ?? "";
            var dossier = Path.Combine(reportsBase, "Sync_Inscription_Portail");

            Report = new ReportBuilder(dossier, "Sync_Inscription_Portail", workflowName);
            _testCount = 0;
            _passedCount = 0;
            _failedCount = 0;
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown()
        {
            if (Report == null) return;

            string resultat = $"Lignes traitees: {_lignesTraitees} | Connectes: {_connectes} | " +
                              $"Non connectes: {_nonConnectes} | Erreurs: {_erreurs}";
            Report.SetTestResult(_erreurs == 0, resultat);

            var pw = await Playwright.CreateAsync();
            var tempBrowser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            try
            {
                await Report.BuildReportAsPdf(tempBrowser, keepHtml: false);
            }
            finally
            {
                await tempBrowser.CloseAsync();
                pw.Dispose();
            }
        }

        #endregion

        #region Tests

        /// <summary>
        /// Balaye TOUT le CSV : tente la connexion de chaque partenaire et met a jour
        /// InscritSurLePortail (+ Prenom/Nom si connecte).
        /// </summary>
        [Test]
        public async Task SynchroniserInscriptionPortail()
        {
            Interlocked.Increment(ref _testCount);

            testName = "Sync Inscription Portail";
            await Setup("chromium", false, LangueSync);
            string label = GetTestLabel();
            LogGitHubActions(label);

            await ExecuterSynchronisation("Synchronisation - Tout le CSV", LangueSync, inscritsSeulement: false);

            LogGitHubActionsSuccess(label);
            Interlocked.Increment(ref _passedCount);
        }

        /// <summary>
        /// Ne parcourt QUE les partenaires deja InscritSurLePortail=true et resynchronise leurs infos.
        /// Met en pause sur l'inspecteur Playwright apres chaque connexion (en local) pour explorer
        /// la page et reperer des verifications supplementaires a ajouter.
        /// </summary>
        [Test]
        public async Task SynchroniserInscrits()
        {
            Interlocked.Increment(ref _testCount);

            testName = "Sync Inscrits";
            await Setup("chromium", false, LangueSync);
            string label = GetTestLabel();
            LogGitHubActions(label);

            await ExecuterSynchronisation("Synchronisation - Inscrits seulement", LangueSync,
                inscritsSeulement: true, pause: true);

            LogGitHubActionsSuccess(label);
            Interlocked.Increment(ref _passedCount);
        }

        #endregion

        #region Coeur du balayage

        /// <summary>
        /// Coeur commun aux deux tests.
        /// </summary>
        /// <param name="titre">Titre affiche dans le rapport.</param>
        /// <param name="lg">Langue.</param>
        /// <param name="inscritsSeulement">Ne traiter que les lignes InscritSurLePortail=true.</param>
        /// <param name="pause">Met en pause sur l'inspecteur Playwright apres chaque connexion (local).</param>
        private async Task ExecuterSynchronisation(string titre, string lg, bool inscritsSeulement, bool pause = false)
        {
            int locTraitees = 0, locConnectes = 0, locNonConnectes = 0, locErreurs = 0;

            string path = Serializer.PathToPartenaireCsv;
            var lines = File.ReadAllLines(path).ToList();
            if (lines.Count < 2)
            {
                Assert.Fail($"CSV vide ou introuvable: {path}");
                return;
            }

            var header = lines[0].Split(';').Select(h => h.Trim()).ToList();
            int idxEmail = header.IndexOf("Email");
            int idxNum = header.IndexOf("NumeroPartenaire");
            int idxInscrit = header.IndexOf("InscritSurLePortail");

            if (idxEmail < 0 || idxInscrit < 0)
            {
                Assert.Fail("Colonnes 'Email' et/ou 'InscritSurLePortail' absentes de l'en-tete du CSV.");
                return;
            }

            lock (_reportLock)
            {
                Report!.AddTextBlock(titre,
                    $"Fichier: {path}<br/>" +
                    $"Filtre: {(inscritsSeulement ? "InscritSurLePortail=true uniquement" : "toutes les lignes")}<br/>" +
                    $"Navigateur: Chrome Desktop {lg}",
                    "info");
            }

            for (int i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var cells = lines[i].Split(';').ToList();
                // Normaliser la longueur sur l'en-tete pour pouvoir indexer chaque colonne.
                while (cells.Count < header.Count) cells.Add("");

                string email = idxEmail < cells.Count ? cells[idxEmail].Trim() : "";
                string numero = (idxNum >= 0 && idxNum < cells.Count) ? cells[idxNum].Trim() : "?";
                if (string.IsNullOrWhiteSpace(email)) continue;

                // Filtre : ne garder que les inscrits si demande.
                if (inscritsSeulement && !EstVrai(cells[idxInscrit])) continue;

                locTraitees++;
                _lignesTraitees++;
                Console.WriteLine($"[{i}/{lines.Count - 1}] Partenaire {numero} <{email}>");

                IBrowserContext? ctx = null;
                try
                {
                    // Session isolee : un nouveau contexte = cookies/tokens vierges.
                    ctx = await TestTools.CreateContext(browser!, false);
                    var p = await ctx.NewPageAsync();

                    bool inscrit = await TenterConnexion(p, email);
                    cells[idxInscrit] = inscrit ? "True" : "False";

                    if (inscrit)
                    {
                        locConnectes++;
                        _connectes++;
                        Console.WriteLine("  -> Connecte");

                        if (pause) await PausePourInspection(p);

                        var infos = await LireInfosDepuisPortail(p, lg);
                        AppliquerInfos(cells, header, infos);
                    }
                    else
                    {
                        locNonConnectes++;
                        _nonConnectes++;
                        Console.WriteLine("  -> Toujours sur la page de connexion : InscritSurLePortail = False");
                    }
                }
                catch (Exception ex)
                {
                    locErreurs++;
                    _erreurs++;
                    cells[idxInscrit] = "False";
                    Console.WriteLine($"  -> Erreur ({numero}): {ex.Message}");
                }
                finally
                {
                    if (ctx != null) await ctx.CloseAsync();
                }

                // Reecriture incrementale : la progression est sauvee meme en cas d'arret.
                lines[i] = string.Join(";", cells);
                File.WriteAllLines(path, lines, new UTF8Encoding(false));
            }

            // --- Synthese ---
            string synthese = $"Lignes traitees: {locTraitees}<br/>Connectes (True): {locConnectes}<br/>" +
                              $"Non connectes (False): {locNonConnectes}<br/>Erreurs: {locErreurs}";
            lock (_reportLock)
            {
                Report!.AddTextBlock($"{titre} - Synthese", synthese, locErreurs == 0 ? "success" : "error");
            }
            Console.WriteLine($"=== {synthese.Replace("<br/>", " | ")} ===");
        }

        #endregion

        #region Connexion

        /// <summary>
        /// Tente de connecter un email sur le portail. Retourne true si la connexion a abouti
        /// (le champ de connexion n'est plus affiche), false si on reste sur la page de connexion.
        /// </summary>
        private async Task<bool> TenterConnexion(IPage p, string email)
        {
            await p.GotoAsync(Serializer.GetUrlPortailQual());

            var champUser = p.Locator("input[name='username']");
            try { await champUser.WaitForAsync(new() { Timeout = 15000 }); }
            catch { /* Formulaire absent : l'etat connecte est verifie plus bas. */ }

            if (await champUser.IsVisibleAsync())
            {
                await p.FillAsync("input[name='username']", email);
                await p.FillAsync("input[name='password']", Serializer.mdp);
                await p.ClickAsync("button[type='submit']");

                try { await p.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 20000 }); }
                catch { /* timeout reseau : on evalue l'etat ci-dessous */ }

                await p.WaitForTimeoutAsync(1500);
                await TestTools.PopUpPortal(p);
            }

            // Connecte si le champ de connexion a disparu.
            bool encoreSurLogin = await champUser.IsVisibleAsync();
            return !encoreSurLogin;
        }

        /// <summary>
        /// Detecte le popup "Vos factures d'electricite par e-mail" (mygre-pdf-push) qui peut
        /// apparaitre apres connexion. Retourne true s'il est present, puis le ferme SANS
        /// modifier le reglage (touche Echap ; la navigation suivante le dissipe de toute facon).
        /// </summary>
        private async Task<bool> GererPopupFactureEmail(IPage p)
        {
            try
            {
                var popup = p.Locator("mygre-pdf-push");
                try { await popup.First.WaitForAsync(new() { Timeout = 2500 }); }
                catch { /* pas de popup dans le delai */ }

                if (await popup.CountAsync() == 0 || !await popup.First.IsVisibleAsync())
                    return false;

                // Fermeture non destructive (ne change pas la preference de facturation)
                try { await p.Keyboard.PressAsync("Escape"); await p.WaitForTimeoutAsync(400); }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [warn] Popup facture e-mail: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Met en pause sur l'inspecteur Playwright (en local uniquement) pour permettre d'explorer
        /// la page connectee et reperer les verifications supplementaires a ajouter.
        /// Ignore en mode headless / CI (sinon le test resterait bloque).
        /// </summary>
        private async Task PausePourInspection(IPage p)
        {
            if (TestConfig.IsHeadless)
            {
                Console.WriteLine("  [PAUSE] Ignoree (mode headless / CI).");
                return;
            }

            Console.WriteLine("  [PAUSE] Inspecteur Playwright ouvert : explorez la page, puis cliquez sur 'Resume'.");
            await p.PauseAsync();
        }

        #endregion

        #region Lecture des informations affichees

        /// <summary>
        /// Lit le nom affiche apres connexion via l'en-tete "Bienvenue {prenom} {nom}!" et le
        /// decoupe en Prenom (1er mot) et Nom (mots suivants), en retirant un eventuel "!".
        /// Stocke Prenom et Nom INDEPENDAMMENT.
        /// </summary>
        private async Task<Dictionary<string, string>> LireInfosDepuisPortail(IPage p, string lg)
        {
            var infos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var dashboard = new DashboardPage(p);

            // Forcer la langue pour lire l'en-tete attendu.
            try
            {
                await TestTools.SelectLanguage(p, false, lg);
                await p.WaitForTimeoutAsync(800);
            }
            catch (Exception ex) { Console.WriteLine($"  [warn] Selection langue: {ex.Message}"); }

            try
            {
                string bienvenue = await dashboard.LireTexteBienvenue(lg); // ex. "Bienvenue Egidus Zimmermann!"
                string mot = ResourceHelper.GetStringPortail("Welcome", lg);
                string nomComplet = bienvenue
                    .Replace(mot, "", StringComparison.OrdinalIgnoreCase)
                    .Replace("!", "")
                    .Trim();

                Console.WriteLine($"  [nom affiche] \"{nomComplet}\"");

                var parties = nomComplet.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parties.Length >= 2)
                {
                    infos["Prenom"] = parties[0];
                    infos["Nom"] = string.Join(" ", parties.Skip(1));
                }
                else if (parties.Length == 1)
                {
                    infos["Prenom"] = parties[0];
                }
            }
            catch (Exception ex) { Console.WriteLine($"  [warn] En-tete bienvenue illisible: {ex.Message}"); }

            return infos;
        }

        /// <summary>
        /// Applique les informations lues aux cellules de la ligne CSV (seules les colonnes
        /// presentes dans l'en-tete et avec une valeur non vide sont modifiees).
        /// </summary>
        private static void AppliquerInfos(List<string> cells, List<string> header, Dictionary<string, string> infos)
        {
            foreach (var kv in infos)
            {
                int idx = header.IndexOf(kv.Key);
                if (idx < 0 || idx >= cells.Count) continue;
                string valeur = Sanitize(kv.Value);
                if (string.IsNullOrWhiteSpace(valeur)) continue;
                if (cells[idx] != valeur)
                {
                    Console.WriteLine($"  [maj] {kv.Key}: \"{cells[idx]}\" -> \"{valeur}\"");
                    cells[idx] = valeur;
                }
            }
        }

        /// <summary>Vrai si la cellule represente une valeur booleenne vraie ("True"/"1").</summary>
        private static bool EstVrai(string val)
        {
            string v = (val ?? "").Trim();
            return v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
        }

        /// <summary>Nettoie une valeur pour l'ecriture CSV (pas de ';' ni de saut de ligne).</summary>
        private static string Sanitize(string val) =>
            (val ?? "").Replace(";", ",").Replace("\r", " ").Replace("\n", " ").Trim();

        #endregion

        #region Synchronisation des donnees compteurs (multi-lignes)

        /// <summary>Colonnes ajoutees pour les donnees compteur/produit/facture.</summary>
        private static readonly string[] _colonnesCompteur =
        {
            "CompteurIntelligent", "PortP1Actif", "CompteurActif", "FacturePayee", "BoutonsPaiementDisponibles"
        };

        /// <summary>
        /// Pour chaque partenaire InscritSurLePortail=true : connexion, puis lecture des onglets
        /// Produits & contrats, Consommation et Factures. Le CSV est reconstruit avec UNE LIGNE
        /// PAR COMPTEUR (intitule, n° compteur, smart, P1, garantie d'origine, actif/inactif,
        /// facture en suspens/payee, boutons de paiement). Dedup par (partenaire, compteur).
        /// </summary>
        [Test]
        public async Task SynchroniserDonneesCompteurs()
        {
            Interlocked.Increment(ref _testCount);

            testName = "Sync Donnees Compteurs";
            await Setup("chromium", false, LangueSync);
            string lg = LangueSync;
            string label = GetTestLabel();
            LogGitHubActions(label);

            string path = Serializer.PathToPartenaireCsv;
            var lignesBrutes = File.ReadAllLines(path).ToList();
            if (lignesBrutes.Count < 2) { Assert.Fail($"CSV vide ou introuvable: {path}"); return; }

            // En-tete + ajout des nouvelles colonnes si absentes
            var header = lignesBrutes[0].Split(';').Select(h => h.Trim()).ToList();
            foreach (var col in _colonnesCompteur)
                if (!header.Contains(col)) header.Add(col);

            int idxNum = header.IndexOf("NumeroPartenaire");
            int idxEmail = header.IndexOf("Email");
            int idxInscrit = header.IndexOf("InscritSurLePortail");
            if (idxNum < 0 || idxEmail < 0 || idxInscrit < 0)
            { Assert.Fail("Colonnes 'NumeroPartenaire'/'Email'/'InscritSurLePortail' absentes."); return; }

            // Lignes -> cellules, normalisees sur la longueur de l'entete
            var rows = new List<List<string>>();
            for (int i = 1; i < lignesBrutes.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lignesBrutes[i])) continue;
                var cells = lignesBrutes[i].Split(';').ToList();
                while (cells.Count < header.Count) cells.Add("");
                rows.Add(cells);
            }

            // Partenaires inscrits uniques (dans l'ordre d'apparition)
            var partenaires = new List<string>();
            var vus = new HashSet<string>();
            foreach (var c in rows)
            {
                if (!EstVrai(c[idxInscrit])) continue;
                string num = c[idxNum].Trim();
                if (num.Length == 0 || !vus.Add(num)) continue;
                partenaires.Add(num);
            }

            lock (_reportLock)
            {
                Report!.AddTextBlock("Sync Donnees Compteurs",
                    $"Fichier: {path}<br/>Partenaires inscrits a traiter: {partenaires.Count}", "info");
            }

            int traites = 0, lignesGenerees = 0;

            foreach (var numero in partenaires)
            {
                var gabarit = rows.First(r => r[idxNum].Trim() == numero);
                string email = gabarit[idxEmail].Trim();
                if (email.Length == 0) continue;

                _lignesTraitees++;
                Console.WriteLine($"Partenaire {numero} <{email}>");

                IBrowserContext? ctx = null;
                try
                {
                    ctx = await TestTools.CreateContext(browser!, false);
                    var p = await ctx.NewPageAsync();
                    await TestTools.ActiverSuiviEtapes(p);

                    bool inscrit = await TenterConnexion(p, email);
                    if (!inscrit)
                    {
                        _nonConnectes++;
                        foreach (var r in rows.Where(r => r[idxNum].Trim() == numero))
                            r[idxInscrit] = "False";
                        Console.WriteLine("  -> Non connecte : InscritSurLePortail = False");
                    }
                    else
                    {
                        _connectes++;

                        // Popup "factures par e-mail" : on note sa presence puis on la ferme
                        bool popupFactureEmail = await GererPopupFactureEmail(p);

                        try { await TestTools.SelectLanguage(p, false, lg); await p.WaitForTimeoutAsync(600); }
                        catch (Exception ex) { Console.WriteLine($"  [warn] Selection langue: {ex.Message}"); }

                        var produits = await LireProduits(p);
                        var conso = await LireConso(p);
                        var factures = await LireFactures(p, lg);
                        Console.WriteLine($"  -> Connecte | popupEmail:{popupFactureEmail} produits:{produits.Count} conso:{conso.Count} factures:{factures.Count}");

                        var nouvelles = ConstruireLignesCompteur(gabarit, header, produits, conso, factures, popupFactureEmail);

                        int pos = rows.FindIndex(r => r[idxNum].Trim() == numero);
                        rows.RemoveAll(r => r[idxNum].Trim() == numero);
                        pos = pos < 0 ? rows.Count : Math.Min(pos, rows.Count);
                        rows.InsertRange(pos, nouvelles);

                        traites++;
                        lignesGenerees += nouvelles.Count;
                    }
                }
                catch (Exception ex)
                {
                    _erreurs++;
                    Console.WriteLine($"  -> Erreur ({numero}): {ex.Message}");
                }
                finally
                {
                    if (ctx != null) await ctx.CloseAsync();
                }

                // Reecriture incrementale (la progression est sauvee meme en cas d'arret)
                var sortie = new List<string> { string.Join(";", header) };
                sortie.AddRange(rows.Select(r => string.Join(";", r)));
                File.WriteAllLines(path, sortie, new UTF8Encoding(false));
            }

            string synthese = $"Partenaires traites: {traites}<br/>Lignes compteurs generees: {lignesGenerees}";
            lock (_reportLock)
            {
                Report!.AddTextBlock("Sync Donnees Compteurs - Synthese", synthese, "success");
            }
            Console.WriteLine($"=== {synthese.Replace("<br/>", " | ")} ===");

            LogGitHubActionsSuccess(label);
            Interlocked.Increment(ref _passedCount);
        }

        #endregion

        #region Lecture des onglets (compteurs)

        private async Task<List<ProduitContrat>> LireProduits(IPage p)
        {
            try
            {
                var page = new ProduitsContratsPage(p);
                await page.Naviguer();
                return await page.ExtraireProduits();
            }
            catch (Exception ex) { Console.WriteLine($"  [warn] Produits & contrats: {ex.Message}"); return new List<ProduitContrat>(); }
        }

        private async Task<List<CompteurConso>> LireConso(IPage p)
        {
            try
            {
                var page = new ConsommationPage(p);
                await page.Naviguer();
                return await page.ExtraireCompteurs();
            }
            catch (Exception ex) { Console.WriteLine($"  [warn] Consommation: {ex.Message}"); return new List<CompteurConso>(); }
        }

        private async Task<List<FactureCompte>> LireFactures(IPage p, string lg)
        {
            try
            {
                var page = new FacturesPage(p);
                await page.Naviguer();
                return await page.ExtraireFactures(lg);
            }
            catch (Exception ex) { Console.WriteLine($"  [warn] Factures: {ex.Message}"); return new List<FactureCompte>(); }
        }

        /// <summary>
        /// Construit les lignes CSV (une par compteur) pour un partenaire, en partant
        /// de son gabarit (ligne existante) pour preserver les colonnes non lues.
        /// </summary>
        private static List<List<string>> ConstruireLignesCompteur(
            List<string> gabarit, List<string> header,
            List<ProduitContrat> produits, List<CompteurConso> conso, List<FactureCompte> factures,
            bool popupFactureEmail)
        {
            var resultat = new List<List<string>>();

            var consoParCompteur = new Dictionary<string, CompteurConso>();
            foreach (var c in conso)
                if (!string.IsNullOrEmpty(c.NumeroCompteur)) consoParCompteur[c.NumeroCompteur!] = c;

            // Aucun compteur lisible -> on conserve une seule ligne (gabarit), marquee inscrite
            if (produits.Count == 0 && conso.Count == 0)
            {
                var seule = new List<string>(gabarit);
                Set(seule, header, "InscritSurLePortail", "True");
                Set(seule, header, "AvertissementFactureParMail", Bool(popupFactureEmail));
                resultat.Add(seule);
                return resultat;
            }

            // Source primaire : Produits & contrats ; a defaut, Consommation
            IEnumerable<ProduitContrat> primaires = produits.Count > 0
                ? produits
                : conso.Select(c => new ProduitContrat
                {
                    Intitule = c.Intitule,
                    NumeroCompteur = c.NumeroCompteur,
                    Actif = c.Actif,
                    Rue = c.Rue,
                    NumRue = c.NumRue,
                    CodePostal = c.CodePostal,
                    Ville = c.Ville
                });

            foreach (var pc in primaires)
            {
                var ligne = new List<string>(gabarit);

                Set(ligne, header, "LieuDeConsommation", pc.Intitule);
                Set(ligne, header, "numeroCompteur", pc.NumeroCompteur ?? "");
                Set(ligne, header, "Rue", pc.Rue);
                Set(ligne, header, "NumRue", pc.NumRue);
                Set(ligne, header, "CodePostal", pc.CodePostal);
                Set(ligne, header, "CompteurIntelligent", Bool(pc.CompteurIntelligent));
                Set(ligne, header, "PortP1Actif", Bool(pc.PortP1Actif));
                Set(ligne, header, "GarantieOrigine", Bool(pc.GarantieOrigineActive));
                Set(ligne, header, "InscritSurLePortail", "True");
                Set(ligne, header, "AvertissementFactureParMail", Bool(popupFactureEmail));

                // Actif/inactif depuis Consommation (sinon depuis Produits)
                bool? actif = pc.Actif;
                if (!string.IsNullOrEmpty(pc.NumeroCompteur)
                    && consoParCompteur.TryGetValue(pc.NumeroCompteur!, out var cc))
                    actif = cc.Actif;
                Set(ligne, header, "CompteurActif", Bool(actif));

                // Factures rapprochees par adresse (lieu)
                string cle = PortailParsing.CleLieu(pc.Rue, pc.NumRue, pc.CodePostal);
                var liees = factures
                    .Where(f => PortailParsing.CleLieu(f.Rue, f.NumRue, f.CodePostal) == cle)
                    .ToList();
                if (liees.Count > 0)
                {
                    bool enSuspens = liees.Any(f => f.EnSuspens);
                    bool boutons = liees.Any(f => f.BoutonsPaiementDisponibles);
                    Set(ligne, header, "CompteContrat", liees[0].CompteContrat ?? Get(ligne, header, "CompteContrat"));
                    Set(ligne, header, "FactureOuverte", Bool(enSuspens));
                    Set(ligne, header, "FacturePayee", Bool(!enSuspens));
                    Set(ligne, header, "BoutonsPaiementDisponibles", Bool(boutons));
                }

                resultat.Add(ligne);
            }

            return resultat;
        }

        private static void Set(List<string> ligne, List<string> header, string nom, string? valeur)
        {
            int i = header.IndexOf(nom);
            if (i < 0 || i >= ligne.Count) return;
            ligne[i] = Sanitize(valeur ?? "");
        }

        private static string Get(List<string> ligne, List<string> header, string nom)
        {
            int i = header.IndexOf(nom);
            return (i >= 0 && i < ligne.Count) ? ligne[i] : "";
        }

        private static string Bool(bool? v) => v.HasValue ? (v.Value ? "True" : "False") : "";

        #endregion
    }
}
