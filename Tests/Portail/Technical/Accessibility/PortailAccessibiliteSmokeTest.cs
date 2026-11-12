using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using TestPlayrights.PortailV2Copilot.Pages;

namespace TestPlayrights.PortailV2Copilot.Smoke
{
    [TestFixture]
    [Category("PORTAIL_ACCESSIBILITY")]
    public sealed class PortailAccessibiliteSmokeTest
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        [SetUp]
        public async Task SetUpAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = TestConfig.IsHeadless
            });
            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            _page = await _context.NewPageAsync();
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await TestEvidence.CaptureAsync(_page);
            try
            {
                if (_context != null)
                {
                    await _context.CloseAsync();
                }
            }
            finally
            {
                try
                {
                    if (_browser != null)
                    {
                        await _browser.CloseAsync();
                    }
                }
                finally
                {
                    _playwright?.Dispose();
                }
            }
        }

        [TestCaseSource(nameof(GenererLangues))]
        public async Task PortailMyGroupe_RespecteLesReglesAccessibiliteWCAG(string langue)
        {
            var partenaire = DataLoader.FindClientWithCriteria(
                DataLoader.GetPartenaires(Serializer.PathToPartenaireCsv),
                new Dictionary<string, string?> { ["InscritSurLePortail"] = "true" });

            Assert.That(partenaire, Is.Not.Null,
                "Aucun partenaire inscrit au Portail n'a été trouvé dans le fichier de données.");
            Assert.That(partenaire!.Email, Is.Not.Null.And.Not.Empty,
                "Le partenaire sélectionné n'a pas d'adresse email.");

            var connexionPage = new ConnexionPage(_page!, langue);
            await connexionPage.NaviguerAsync();
            await connexionPage.SeConnecterAsync(partenaire.Email, Serializer.mdp);

            var accueilPage = new AccueilPage(_page!);
            if (!await accueilPage.DetecterPopupFactureEmailAsync())
            {
                await accueilPage.FermerPopupInformationSiPresentAsync();
            }
            await accueilPage.VerifierConnexionReussieAsync();
            await _page.Locator("mat-progress-spinner[role='progressbar']").WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 30_000
            });

            var uriPortail = new Uri(_page!.Url);
            Assert.That(uriPortail.Host, Does.EndWith("my-q.groupe-e.ch"),
                $"L'audit doit porter sur le Portail my-groupe connecté, pas sur un autre site. Hôte obtenu : {uriPortail.Host}");
            var urlPageControlee = new UriBuilder(uriPortail)
            {
                Query = string.Empty,
                Fragment = string.Empty
            }.Uri.AbsoluteUri;

            var resultatAxe = await _page.RunAxe(new AxeRunOptions
            {
                RunOnly = new RunOnlyOptions
                {
                    Type = "tag",
                    Values = new List<string>
                    {
                        "wcag2a",
                        "wcag2aa",
                        "wcag21a",
                        "wcag21aa",
                        "wcag22aa"
                    }
                }
            });

            var violations = resultatAxe.Violations?.ToList() ?? new List<AxeResultItem>();
            if (violations.Count == 0)
            {
                return;
            }

            var diagnostic = await CreerPreuvesAccessibiliteAsync(_page, violations);
            Assert.That(violations, Is.Empty,
                $"Violations d'accessibilité WCAG 2.2 AA sur le Portail my-groupe : {urlPageControlee}" +
                $"{Environment.NewLine}{diagnostic}");
        }

        private static async Task<string> CreerPreuvesAccessibiliteAsync(
            IPage page,
            IReadOnlyList<AxeResultItem> violations)
        {
            var repertoirePreuves = Environment.GetEnvironmentVariable("TEST_EVIDENCE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(repertoirePreuves))
            {
                repertoirePreuves = Path.Combine(
                    TestContext.CurrentContext.WorkDirectory,
                    "TestResults",
                    "Accessibility");
            }

            Directory.CreateDirectory(repertoirePreuves);
            var nomTest = NettoyerNomFichier(TestContext.CurrentContext.Test.Name);
            var horodatage = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
            var details = new List<string>
            {
                $"Site contrôlé : application Portail my-groupe après connexion ({NettoyerUrl(page.Url)})",
                "Chaque preuve ci-dessous est une capture pleine page avec l'élément concerné encadré en rouge; " +
                "elle conserve le contexte visuel de la page. Pour confirmer un nom accessible, inspecter " +
                "la cible dans le panneau Accessibilité des outils de développement ou la parcourir avec un lecteur d'écran."
            };

            var cheminCapturePage = Path.Combine(
                repertoirePreuves,
                $"{nomTest}_{horodatage}_page.png");
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = cheminCapturePage,
                FullPage = true
            });
            TestContext.AddTestAttachment(cheminCapturePage, "Page complète du Portail my-groupe");
            details.Add($"Capture générale de la page : {Path.GetFileName(cheminCapturePage)}");

            for (var indexViolation = 0; indexViolation < violations.Count; indexViolation++)
            {
                var violation = violations[indexViolation];
                details.Add(
                    $"{Environment.NewLine}{violation.Id} ({violation.Impact}) : {ExpliquerViolation(violation.Id)}" +
                    $"{Environment.NewLine}Règle axe : {violation.Help}" +
                    $"{Environment.NewLine}Détails de la règle : {violation.HelpUrl}");

                for (var indexNoeud = 0; indexNoeud < violation.Nodes.Length; indexNoeud++)
                {
                    var noeud = violation.Nodes[indexNoeud];
                    var cible = noeud.Target.Selector;
                    details.Add(
                        $"  Cible sur cette page : {cible}" +
                        $"{Environment.NewLine}  HTML observé : {noeud.Html}");

                    var verificationsAxe = (noeud.Any ?? Array.Empty<AxeResultCheck>())
                        .Concat(noeud.All ?? Array.Empty<AxeResultCheck>())
                        .Concat(noeud.None ?? Array.Empty<AxeResultCheck>())
                        .Select(verification => verification.Message)
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .Distinct();
                    foreach (var verification in verificationsAxe)
                    {
                        details.Add($"  Vérification axe : {verification}");
                    }

                    var element = page.Locator(cible).First;
                    if (await element.CountAsync() == 0)
                    {
                        details.Add("  La cible n'est plus présente dans la page; seule la capture générale est disponible.");
                        continue;
                    }

                    var texteVisible = (await element.InnerTextAsync()).Trim();
                    var ariaLabel = await element.GetAttributeAsync("aria-label");
                    details.Add(
                        $"  Texte visible : \"{texteVisible}\"" +
                        $"{Environment.NewLine}  Nom accessible explicite (aria-label) : " +
                        (ariaLabel == null ? "absent" : $"\"{ariaLabel}\""));

                    var elementVisible = await element.IsVisibleAsync();
                    if (!elementVisible)
                    {
                        details.Add(
                            "  Élément non visible (métadonnée HTML); sa cible est nommée dans le repère rouge de la capture pleine page.");
                    }
                    else
                    {
                        await element.ScrollIntoViewIfNeededAsync();
                    }

                    var cheminCaptureContexte = Path.Combine(
                        repertoirePreuves,
                        $"{nomTest}_{horodatage}_violation_{NettoyerNomFichier(violation.Id)}_{indexNoeud + 1}_context.png");
                    var styleOriginal = await element.GetAttributeAsync("style");
                    var identifiantRepere = $"test-evidence-{Guid.NewGuid():N}";
                    try
                    {
                        if (elementVisible)
                        {
                            await element.EvaluateAsync(
                                "(element) => { element.style.setProperty('outline', '5px solid #e00000', 'important'); " +
                                "element.style.setProperty('outline-offset', '3px', 'important'); " +
                                "element.style.setProperty('box-shadow', '0 0 0 6px rgba(255,255,255,.9)', 'important'); }");
                        }
                        await page.EvaluateAsync(
                            "(data) => { const marker = document.createElement('div'); " +
                            "marker.id = data.id; marker.textContent = data.label; " +
                            "marker.style.cssText = 'position:fixed;z-index:2147483647;top:8px;left:8px;" +
                            "max-width:90vw;padding:10px 14px;background:#a40000;color:white;" +
                            "font: bold 15px Arial,sans-serif;border:3px solid white;box-shadow:0 2px 8px #333;'; " +
                            "document.body.appendChild(marker); }",
                            new
                            {
                                id = identifiantRepere,
                                label = $"ACCESSIBILITÉ : {violation.Id} — cible {cible}"
                            });
                        await page.ScreenshotAsync(new PageScreenshotOptions
                        {
                            Path = cheminCaptureContexte,
                            FullPage = true
                        });
                    }
                    finally
                    {
                        await page.EvaluateAsync(
                            "(id) => document.getElementById(id)?.remove()",
                            identifiantRepere);
                        await element.EvaluateAsync(
                            "(element, style) => { if (style === null) element.removeAttribute('style'); " +
                            "else element.setAttribute('style', style); }",
                            styleOriginal);
                    }

                    TestContext.AddTestAttachment(
                        cheminCaptureContexte,
                        $"{violation.Id} — page complète avec élément signalé encadré en rouge");
                    details.Add(
                        $"  Capture pleine page avec le sélecteur affiché en rouge et la cible encadrée : {Path.GetFileName(cheminCaptureContexte)}");
                }
            }

            var rapport = string.Join(Environment.NewLine, details);
            TestContext.WriteLine(rapport);
            return rapport;
        }

        private static string ExpliquerViolation(string identifiantRegle)
        {
            return identifiantRegle switch
            {
                "button-name" =>
                    "Le bouton n'a pas de nom accessible. Un lecteur d'écran ne peut pas annoncer clairement son action.",
                "label-content-name-mismatch" =>
                    "Le nom accessible du contrôle ne reprend pas son texte visible; " +
                    "une commande vocale peut ne pas retrouver le contrôle à partir du texte affiché.",
                _ =>
                    "axe-core a détecté un problème d'accessibilité. La règle et son explication détaillée sont indiquées ci-dessous."
            };
        }

        private static IEnumerable<TestCaseData> GenererLangues()
        {
            var langues = new List<string>();
            if (LireBooleenEnvironnement("LNG_FR", true))
            {
                langues.Add("FR");
            }

            if (LireBooleenEnvironnement("LNG_DE", true))
            {
                langues.Add("DE");
            }

            if (langues.Count == 0)
            {
                langues.Add("FR");
            }

            foreach (var langue in langues)
            {
                yield return new TestCaseData(langue)
                    .SetName($"Portail_Accessibilite_{langue}");
            }
        }

        private static bool LireBooleenEnvironnement(string nomVariable, bool valeurParDefaut)
        {
            var valeur = Environment.GetEnvironmentVariable(nomVariable);
            return string.IsNullOrEmpty(valeur)
                ? valeurParDefaut
                : valeur.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static string NettoyerNomFichier(string valeur)
        {
            var caracteresInterdits = Path.GetInvalidFileNameChars();
            var nomNettoye = new string(valeur
                .Select(caractere => caracteresInterdits.Contains(caractere) ? '_' : caractere)
                .ToArray());
            return nomNettoye.Replace(' ', '_');
        }

        private static string NettoyerUrl(string valeur)
        {
            var uri = new Uri(valeur);
            return new UriBuilder(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty
            }.Uri.AbsoluteUri;
        }
    }
}
