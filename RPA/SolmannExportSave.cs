using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    
    [TestFixture]
    [Category("RPA")]
    public class SolmannExportSave
    {
        //Configuration SAP CRM ---
        private const string UrlSapCrm =
            "https://psa.din.cce24x7.net/sap(bD1kZSZjPTEwMCZkPW1pbg==)/bc/bsp/sap/crm_ui_start/default.htm?sap-client=100&sap-language=EN";
        private const int MaxPages = 500;
        private const int DelaiAvantCaptureMs = 2500;
        private const int DelaiApresScrollMs = 700;
        private const int DelaiApresCaptureMs = 800;
        private const double FractionScroll = 0.5;

        // Nom logique du test (utilisé pour nommer le dossier de sortie)
        private const string TestName = "SolmannExport";

        // varaibles playwrights
        private IPlaywright? playwright;
        private IBrowser? browser;
        private IBrowserContext? context;
        private IPage? page;

        // session chrome (pour la capture MHTML via CDP)
        private ICDPSession? _cdp;

        #region Cycle de vie (Setup / TearDown autonomes)

        
        [SetUp]
        ///Initialisation des éléments playwrights, navigateur et page
        public async Task Setup()
        {

            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[] { "--start-maximized" }
            });

            // Mode mobile 390x844 capture complète de la page en deux prises.
            var iphone12 = playwright.Devices["iPhone 12"];
            context = await browser.NewContextAsync(new BrowserNewContextOptions(iphone12)
            {
                IgnoreHTTPSErrors = true
            });

            page = await context.NewPageAsync();
        }

        
        [TearDown]
        public async Task TearDown()
        {
            await TestEvidence.CaptureAsync(page);
            if (context != null) await context.CloseAsync();
            if (browser != null) await browser.CloseAsync();
            playwright?.Dispose();
        }

      
        /// <summary>
        /// Dossier de sortie autonome, créé à côté de l'exécutable :
        ///   {BaseDirectory}/REPORTS/yyyyMMdd/SolmannExport
        /// </summary>
        private static string DossierResultats()
        {
            string dossier = Path.Combine(
                AppContext.BaseDirectory,
                "REPORTS",
                DateTime.Now.ToString("yyyyMMdd"),
                TestName);
            Directory.CreateDirectory(dossier);
            return dossier;
        }

        #endregion

        [Test]
        public async Task SolmannExport()
        {
            // --- Navigation vers la page SAP CRM ---
            await page!.GotoAsync(UrlSapCrm, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            // Crée le dossier où seront stockées les captures et les documents PDF/ZIP
            string dossierExecution = Path.Combine(
                DossierResultats(),
                $"Execution_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(dossierExecution);
            //Ouvre la liste des changes
            await OuvrirVueChangeEnCours();

            //Init des variable de paginations
            int pageCourante = 1;
            int compteur = 0;
            //Boucle principale
            while (pageCourante <= MaxPages)
            {
                int nbLignes = await CompterLignesAsync();
                if (nbLignes == 0) break;

                // Vérifie si il va y avoir une page suivante
                bool pageSuivanteExiste = await ForwardDisponibleAsync();
                    // Pour chaque ligne de la page
                for (int ligne = 0; ligne < nbLignes; ligne++)
                {
                    // Lien ID de la ligne (colonne 1)
                    var lienChange = WorkArea()
                        .GetByRole(AriaRole.Link, new() { Name = "Change ID" })
                        .Nth(ligne);

                    //Enregistrer le numéro du change pour le  nom du dossier
                    string numeroChange = (await lienChange.InnerTextAsync()).Trim();

                    // Un dossier par change
                    string dossierChange = Path.Combine(
                        dossierExecution,
                        Nettoyer(string.IsNullOrEmpty(numeroChange) ? $"p{pageCourante:D2}_l{ligne + 1:D2}" : numeroChange));
                    Directory.CreateDirectory(dossierChange);


                    await lienChange.ClickAsync();
                    await page.WaitForTimeoutAsync(DelaiAvantCaptureMs);

                    //1 ère capute
                    await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = Path.Combine(dossierChange, "1_haut.png"),
                        FullPage = true
                    });

                    //  Scroll
                    await ScrollDetailAsync(FractionScroll);
                    await page.WaitForTimeoutAsync(DelaiApresScrollMs);
                    //2 eme capture
                    await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = Path.Combine(dossierChange, "2_bas.png"),
                        FullPage = true
                    });

                    compteur++;



                    //Enregistre la page en mhtml
                    await SauvegarderMhtmlAsync(dossierChange, "page.mhtml");

                    //remonte pour pas avoir de problèem sur la prochaine executions
                    await RemonterDetailAsync();

                    //télécharge les documents pdf, zip et word
                    await TelechargerDocumentsAsync(dossierChange);

                    await page.WaitForTimeoutAsync(DelaiApresCaptureMs);

                    await RetournerALaListe();
                }
                //tant que la page suivante existe
                if (!pageSuivanteExiste) break;

                // Page suivante
                await WorkArea()
                    .GetByRole(AriaRole.Link, new() { Name = "Forward", Exact = true })
                    .ClickAsync();
                await AttendreListeStableAsync();
                pageCourante++;
            }

        }

        #region Fonctions de navigation SAP CRM


        private IFrameLocator WorkArea() =>
            page!.Locator("iframe[name=\"CRMApplicationFrame\"]").ContentFrame
                 .Locator("#WorkAreaFrame1").ContentFrame;

        private ILocator LiensChangeId() =>
            WorkArea().GetByRole(AriaRole.Link, new() { Name = "Change ID" });

        /// <summary>
        /// Ouvre la vue "change en cours"
        /// </summary>
        private async Task OuvrirVueChangeEnCours()
        {
            await WorkArea()
                .GetByRole(AriaRole.Link, new() { Name = "Vue des change en cours" })
                .ClickAsync();
            await AttendreListeStableAsync();
        }

        /// <summary>
        /// Revenir a la liste des changes en fin de boucle
        /// </summary>
        private async Task RetournerALaListe()
        {
            await WorkArea().Locator("#BackButton").First.ClickAsync();
            await LiensChangeId().First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30000
            });
        }

        /// <summary>
        /// Fait defiler la page de detail
        /// </summary>
        private async Task ScrollDetailAsync(double fraction)
        {
            var box = await page!.Locator("iframe[name=\"CRMApplicationFrame\"]").BoundingBoxAsync();
            if (box != null)
            {
                await page.Mouse.MoveAsync(
                    //repérage hors zones pour scroll pas dans les petits menus
                    (float)(box.X + box.Width / 2),
                    (float)(box.Y + box.Height * 0.35));
            }
            //Copilot qui m'a pondu ça
            // en gros le calcul de la hauteur de scroll a faire
            double delta = 800;
            var frame = page.Frames.FirstOrDefault(f => f.Name == "WorkAreaFrame1");
            if (frame != null)
            {
                try
                {
                    delta = await frame.EvaluateAsync<double>(@"(frac) => {
                        const h = Math.max(document.body.scrollHeight, document.documentElement.scrollHeight);
                        const vh = window.innerHeight || document.documentElement.clientHeight || 800;
                        return Math.max(vh * 0.9, (h - vh) * frac);
                    }", fraction);
                }
                catch
                {

                }
            }

            await page.Mouse.WheelAsync(0, (float)delta);
        }


        /// Remonte la page de detail tout en haut
        private async Task RemonterDetailAsync()
        {
            var box = await page!.Locator("iframe[name=\"CRMApplicationFrame\"]").BoundingBoxAsync();
            if (box != null)
            {
                await page.Mouse.MoveAsync(
                    (float)(box.X + box.Width / 2),
                    (float)(box.Y + box.Height * 0.35));
            }

            await page.Mouse.WheelAsync(0, -100000);
        }


        /// Telecharge, dans le dossier du change, les documents de la liste dont le type
        /// est PDF ou ZIP et Word (les autres types URL, sont ignores).
        private async Task TelechargerDocumentsAsync(string dossierChange)
        {
            // Liens "Name" de la liste de documents (colonne Name)
            var liens = await WorkArea()
                .Locator("a[id*='doclist_table'][id$='kw_relative_url']")
                .AllAsync();
            if (liens.Count == 0) return;

            foreach (var lien in liens)
            {
                string id = await lien.GetAttributeAsync("id") ?? "";
                if (id.Length == 0) continue;

                // Type du document
                string typeId = id.Replace(".kw_relative_url", ".type");
                string type = "";
                try { type = (await WorkArea().Locator($"[id=\"{typeId}\"]").InnerTextAsync()).Trim(); }
                catch {}

                bool estPdf = type.Contains("pdf", StringComparison.OrdinalIgnoreCase);
                bool estZip = type.Contains("zip", StringComparison.OrdinalIgnoreCase);
                bool estWord = type.Contains("Word Document", StringComparison.OrdinalIgnoreCase);
                if (!estPdf && !estZip && !estWord) continue;

                string nomDoc = (await lien.InnerTextAsync()).Trim();
                string extParDefaut = estPdf ? ".pdf" : ".zip";

                try
                {

                    var download = await page!.RunAndWaitForDownloadAsync(async () =>
                    {
                        await lien.ClickAsync();
                    }, new PageRunAndWaitForDownloadOptions { Timeout = 15000 });

                    string nomFichier = string.IsNullOrWhiteSpace(download.SuggestedFilename)
                        ? Nettoyer(nomDoc) + extParDefaut
                        : download.SuggestedFilename;

                    await download.SaveAsAsync(Path.Combine(dossierChange, Nettoyer(nomFichier)));
                    Console.WriteLine($"[SolmannExport]      doc telecharge : {nomFichier}");
                }
                catch (Exception exDl)
                {
                    // Cas 2 : le document s'est ouvert dans une fenetre
                    bool ok = await RecupererDepuisPopupAsync(dossierChange, nomDoc, extParDefaut);
                    await FermerPopupsAsync();
                    if (!ok)
                        Console.WriteLine($"[SolmannExport]      doc NON telecharge ({nomDoc} / {type}) : {exDl.Message}");
                }
            }
        }


        /// Recupere un document ouvert dans un autre onglet
        private async Task<bool> RecupererDepuisPopupAsync(string dossierChange, string nomDoc, string extParDefaut)
        {
            foreach (var pop in page!.Context.Pages.Where(p => p != page).ToList())
            {
                try
                {
                    await pop.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
                        new PageWaitForLoadStateOptions { Timeout = 8000 });

                    string url = pop.Url;
                    if (string.IsNullOrEmpty(url) || !url.StartsWith("http")) continue;

                    var resp = await page.Context.APIRequest.GetAsync(url);
                    if (!resp.Ok) continue;

                    var bytes = await resp.BodyAsync();
                    await File.WriteAllBytesAsync(
                        Path.Combine(dossierChange, Nettoyer(nomDoc) + extParDefaut), bytes);
                    Console.WriteLine($"[SolmannExport]      doc (via fenetre) : {nomDoc}{extParDefaut}");
                    return true;
                }
                catch {  }
            }
            return false;
        }

        /// Ferme les fenetres secondaires
         private async Task FermerPopupsAsync()
        {
            foreach (var p in page!.Context.Pages.Where(p => p != page).ToList())
            {
                try { await p.CloseAsync(); } catch { /* deja fermee */ }
            }
        }

        /// Nombre de lignes
        private async Task<int> CompterLignesAsync()
        {
            return await LiensChangeId().CountAsync();
        }


        private async Task<bool> ForwardDisponibleAsync()
        {
            return await WorkArea()
                .GetByRole(AriaRole.Link, new() { Name = "Forward", Exact = true })
                .CountAsync() > 0;
        }


        /// Attend que la liste soit chargee ET que le nombre de lignes se stabilise
        private async Task<int> AttendreListeStableAsync()
        {
            var liens = LiensChangeId();
            await liens.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30000
            });

            int precedent = -1;
            int stable = 0;
            for (int i = 0; i < 40; i++) // borne ~20s
            {
                int c = await liens.CountAsync();
                if (c == precedent && c > 0)
                {
                    if (++stable >= 2) return c; // inchange sur 2 cycles => stabilise
                }
                else
                {
                    stable = 0;
                    precedent = c;
                }
                await page!.WaitForTimeoutAsync(500);
            }
            return precedent;
        }

        /// Conseil copilot
        private async Task<ICDPSession> CdpAsync()
        {
            if (_cdp == null) _cdp = await context!.NewCDPSessionAsync(page!);
            return _cdp;
        }


        /// Sauvegarde la page courante au format MHTML
        private async Task SauvegarderMhtmlAsync(string dossierChange, string nomFichier)
        {
            try
            {
                var cdp = await CdpAsync();
                var res = await cdp.SendAsync("Page.captureSnapshot", new Dictionary<string, object>
                {
                    ["format"] = "mhtml"
                });

                if (res.HasValue && res.Value.TryGetProperty("data", out var data))
                {
                    await File.WriteAllTextAsync(Path.Combine(dossierChange, nomFichier), data.GetString());
                    Console.WriteLine($"[SolmannExport]      MHTML : {nomFichier}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SolmannExport]      MHTML KO : {ex.Message}");
            }
        }

        // Vérifie que c'est valide dans un nom de fichier
        private static string Nettoyer(string valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) return "sansId";
            foreach (var c in Path.GetInvalidFileNameChars())
                valeur = valeur.Replace(c, '_');
            return valeur.Trim();
        }

        #endregion
    }
}
