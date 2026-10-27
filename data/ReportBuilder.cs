using System.Text;
using Microsoft.Playwright;
using NUnit.Framework.Internal;
using TestPlayrights;

public class ReportBuilder
{
    private readonly string _outputPath;
    private readonly List<string> _testBlocks;
    private readonly string _testName;
    private readonly string _workflowName;
    private string _testResultBlock = "";
    private readonly DateTime _testStartTime;
    private readonly List<string> _screenshotFiles = new();

    public ReportBuilder(string outputPath, string testName, string workflowName = "")
    {
        _outputPath = outputPath;
        if (!Directory.Exists(_outputPath))
        {
            Directory.CreateDirectory(_outputPath);
        }
        _testBlocks = new List<string>();
        _testName = testName;
        _workflowName = workflowName;
        _testStartTime = DateTime.Now;
    }

    /// <summary>
    /// Définit le résultat final du test (sera placé en début de rapport)
    /// </summary>
    public void SetTestResult(bool success, string message = "", TimeSpan? duration = null)
    {
        var resultColor = success ? "#28a745" : "#d9534f";  // Rouge professionnel
        var resultIcon = success ? "✓" : "✗";
        var resultText = success ? "SUCCÈS" : "ÉCHEC";
        
        //var actualDuration = duration ?? (DateTime.Now - _testStartTime);
        
        _testResultBlock = $@"
        <div style='background: linear-gradient(135deg, #fff9f0, #ffffff); 
                    border-left: 4px solid {resultColor}; 
                    padding: 20px; 
                    margin: 20px auto; 
                    border-radius: 4px; 
                    max-width: 800px;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
            <div style='display: flex; align-items: center; margin-bottom: 15px;'>
                <span style='font-size: 28px; margin-right: 15px; color: {resultColor}; font-weight: bold;'>{resultIcon}</span>
                <div>
                    <h2 style='margin: 0; color: {resultColor}; font-size: 22px; font-weight: 600;'>Test {resultText}</h2>
                    <p style='margin: 5px 0 0 0; color: #666; font-size: 13px;'> 
                        Exécuté le: {_testStartTime:dd/MM/yyyy HH:mm:ss}
                    </p>
                </div>
            </div>
            {(string.IsNullOrEmpty(message) ? "" : $"<p style='margin: 10px 0 0 0; color: #333; font-size: 15px;'>{message}</p>")}
        </div>";
    }

    /// <summary>
    /// Ajoute un bloc de texte formaté sans image
    /// </summary>
    public void AddTextBlock(string title, string content, string blockType = "info")
    {
        var (bgColor, borderColor, icon) = blockType.ToLower() switch
        {
            "success" => ("#f8f9fa", "#d4af37", "✓"),  // Or
            "warning" => ("#fff9f0", "#f0ad4e", "!"),   // Orange doré
            "error" => ("#fdf7f7", "#c9302c", "✗"),     // Rouge
            "info" => ("#f8f9fa", "#333333", "i"),      // Noir
            _ => ("#f8f9fa", "#666666", "•")            // Gris
        };

        var block = $@"
        <div style='background: {bgColor}; 
                    border-left: 3px solid {borderColor}; 
                    padding: 15px; 
                    margin: 15px auto; 
                    border-radius: 2px; 
                    max-width: 800px;
                    box-shadow: 0 1px 2px rgba(0,0,0,0.05);'>
            <div style='display: flex; align-items: flex-start;'>
                <span style='font-size: 16px; 
                             margin-right: 12px; 
                             color: {borderColor}; 
                             font-weight: bold; 
                             width: 20px; 
                             text-align: center;'>{icon}</span>
                <div style='flex: 1;'>
                    <h3 style='margin: 0 0 8px 0; color: #1a1a1a; font-size: 16px; font-weight: 600;'>{title}</h3>
                    <div style='color: #4a4a4a; font-size: 14px; line-height: 1.5;'>{content}</div>
                </div>
            </div>
        </div>";
        
        _testBlocks.Add(block);
    }

    /// <summary>
    /// Ajoute dynamiquement un bloc de test au rapport
    /// </summary>
    public void AddTestCase(string title, string data, string description, string expected)
    {
        var block = new StringBuilder();
        block.AppendLine(@"
        <div style='border: 1px solid #e0e0e0; 
                    padding: 20px; 
                    margin: 20px auto; 
                    border-radius: 2px; 
                    background: #ffffff;
                    max-width: 800px;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.08);'>
            <h2 style='color: #1a1a1a; margin-bottom: 18px; padding-bottom: 10px; border-bottom: 1px solid #e0e0e0; font-size: 18px; font-weight: 600;'>" + title + @"</h2>
            <div style='margin-bottom: 12px;'>
                <strong style='color: #333333; display: inline-block; width: 140px; font-size: 14px;'>Données :</strong>
                <span style='color: #666666; font-size: 14px;'>" + data + @"</span>
            </div>
            <div style='margin-bottom: 12px;'>
                <strong style='color: #333333; display: inline-block; width: 140px; font-size: 14px;'>Description :</strong>
                <span style='color: #666666; font-size: 14px;'>" + description + @"</span>
            </div>
            <div style='margin-bottom: 12px;'>
                <strong style='color: #333333; display: inline-block; width: 140px; font-size: 14px;'>Résultat attendu :</strong>
                <span style='color: #666666; font-size: 14px;'>" + expected + @"</span>
            </div>
        </div>");
        
        _testBlocks.Add(block.ToString());
    }

    /// <summary>
    /// Ajoute un step de test avec capture d'écran
    /// </summary>
    public async Task AddTestStep(string stepDescription, IPage p, string imageName = "", string donneAfter = "")
    {
        await p.WaitForTimeoutAsync(1500);
        
        
        string fileName =stepDescription.Replace(" ", "_") + ".png";
        
          /*string.IsNullOrEmpty(imageName)
            ? $"{DateTime.Now:HHmmss}_{stepDescription.Replace(" ", "_")}.png"
            : $"{DateTime.Now:HHmmss}_{imageName}.png";*/

        string screenshotPath = Path.Combine(_outputPath, fileName);
        await TestTools.TakeAScreenshot(p, screenshotPath);
        _screenshotFiles.Add(screenshotPath);

        var block = new StringBuilder();
        block.AppendLine(@"
        <div style='border: 1px solid #e0e0e0; 
                    padding: 20px; 
                    margin: 20px auto; 
                    border-radius: 2px; 
                    background: #ffffff;
                    max-width: 800px;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.08);'>
            <div style='font-weight: 600; margin-bottom: 15px; color: #1a1a1a; font-size: 15px;'>
                " + stepDescription + @"
            </div>
            <a href='./" + fileName + @"' target='_blank'>
                <img src='./" + fileName + @"'
                     style='max-width: 100%;
                            width: auto;
                            height: auto;
                            max-height: 500px;
                            object-fit: contain;
                            border: 1px solid #e0e0e0;
                            border-radius: 2px;
                            display: block;
                            margin: 0 auto;
                            box-shadow: 0 2px 6px rgba(0,0,0,0.08);'
                     alt='Capture du test'/>
            </a>");

        if (!string.IsNullOrEmpty(donneAfter))
        {
            block.AppendLine($@"
            <div style='margin-top: 18px; 
                        padding: 12px; 
                        background: #fafafa; 
                        border-left: 3px solid #d4af37; 
                        border-radius: 2px;'>
                <div style='font-weight: 600; margin-bottom: 6px; color: #1a1a1a; font-size: 14px;'>
                    Informations complémentaires :
                </div>
                <div style='color: #4a4a4a; font-size: 13px; line-height: 1.5;'>{donneAfter}</div>
            </div>");
        }

        block.AppendLine("</div>");
        _testBlocks.Add(block.ToString());
        
        await p.WaitForTimeoutAsync(1500);
    }

    /// <summary>
    /// Construit le rapport HTML final avec bannière professionnelle
    /// </summary>
    public void BuildReport()
    {
        var html = new StringBuilder();
        
        // En-tête HTML avec style professionnel noir/rouge/or
        var workflowDisplay = string.IsNullOrEmpty(_workflowName) ? "" : $@"
            <div class='workflow-name'>{_workflowName}</div>";

        var duration = DateTime.Now - _testStartTime;

        html.AppendLine(@"<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Rapport de Test - " + _testName + @"</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #8b1a1a;
            min-height: 100vh;
            padding: 0;
            margin: 0;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: #ffffff;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }
        .cover-page {
            background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);
            color: white;
            padding: 80px 40px;
            text-align: center;
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            page-break-after: always;
        }
        .cover-page .logo {
            font-size: 48px;
            margin-bottom: 30px;
            color: #d4af37;
        }
        .cover-page h1 {
            font-size: 36px;
            font-weight: 300;
            margin-bottom: 20px;
            letter-spacing: 2px;
            text-transform: uppercase;
        }
        .cover-page .test-name {
            font-size: 42px;
            font-weight: 600;
            color: #d4af37;
            margin: 20px 0;
        }
        .cover-page .workflow-name {
            font-size: 18px;
            color: #888;
            margin-bottom: 40px;
            font-style: italic;
        }
        .cover-page .meta-info {
            margin-top: 60px;
            padding: 30px;
            border: 1px solid #444;
            border-radius: 8px;
            background: rgba(255,255,255,0.05);
        }
        .cover-page .meta-info p {
            margin: 10px 0;
            font-size: 16px;
            color: #ccc;
        }
        .cover-page .meta-info strong {
            color: #d4af37;
        }
        .banner {
            background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);
            color: white;
            padding: 30px 40px;
            text-align: center;
            position: relative;
            border-bottom: 4px solid #d4af37;
        }
        .banner h1 {
            font-size: 20px;
            font-weight: 300;
            letter-spacing: 1px;
            text-transform: uppercase;
        }
        .banner .test-name {
            font-size: 24px;
            font-weight: 600;
            color: #d4af37;
            margin-top: 8px;
        }
        .content {
            padding: 30px;
            background: #ffffff;
        }
        .footer {
            background: #1a1a1a;
            color: #999;
            text-align: center;
            padding: 20px;
            font-size: 12px;
            border-top: 1px solid #d4af37;
        }
        @media print {
            .cover-page { page-break-after: always; }
        }
    </style>
</head>
<body>
    <div class='container'>
        <!-- Page de garde -->
        <div class='cover-page'>
            <div class='logo'>&#9881;</div>
            <h1>Rapport d'exécution automatisé</h1>
            <div class='test-name'>" + _testName + @"</div>" + workflowDisplay + @"
            <div class='meta-info'>
                <p><strong>Date d'exécution :</strong> " + _testStartTime.ToString("dd/MM/yyyy à HH:mm:ss") + @"</p>
                <p><strong>Durée :</strong> " + duration.ToString(@"mm\:ss") + @"</p>
                <p><strong>Environnement :</strong> CI/CD Automatisé</p>
            </div>
        </div>

        <!-- Contenu du rapport -->
        <div class='banner'>
            <h1>Détails du test</h1>
            <div class='test-name'>" + _testName + @"</div>
        </div>
        <div class='content'>");

        // Ajouter le bloc de résultat en premier s'il existe
        if (!string.IsNullOrEmpty(_testResultBlock))
        {
            html.AppendLine(_testResultBlock);
        }

        // Ajouter tous les autres blocs
        foreach (var block in _testBlocks)
        {
            html.AppendLine(block);
        }

        // Footer professionnel
        html.AppendLine(@"
        </div>
        <div class='footer'>
            Généré automatiquement le " + DateTime.Now.ToString("dd/MM/yyyy à HH:mm:ss") + @" | Tests développé par Aïssa Bovet
        </div>
    </div>
</body>
</html>");

        // Écrire le fichier
        var directory = Path.Combine(_outputPath,"index.html");

        File.WriteAllText(directory, html.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Construit le rapport HTML puis le convertit en PDF via Playwright
    /// </summary>
    /// <param name="browser">Instance du navigateur Playwright</param>
    /// <param name="keepHtml">Garder le fichier HTML en plus du PDF (défaut: false)</param>
    /// <param name="cleanupScreenshots">Supprimer les screenshots PNG après intégration dans le PDF (défaut: true)</param>
    public async Task BuildReportAsPdf(IBrowser browser, bool keepHtml = false, bool cleanupScreenshots = true)
    {
        // 1. Génère d'abord le rapport HTML
        BuildReport();

        var htmlPath = Path.Combine(_outputPath, "index.html");
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var workflowPrefix = string.IsNullOrEmpty(_workflowName) ? "" : $"{_workflowName}_";
        var pdfFileName = $"{workflowPrefix}{_testName}_{timestamp}.pdf";
        var pdfPath = Path.Combine(_outputPath, pdfFileName);

        // 2. Crée une nouvelle page pour la conversion
        var page = await browser.NewPageAsync();

        try
        {
            // 3. Charge le fichier HTML local
            await page.GotoAsync($"file://{Path.GetFullPath(htmlPath)}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // 4. Convertit en PDF
            await page.PdfAsync(new PagePdfOptions
            {
                Path = pdfPath,
                Format = "A4",
                PrintBackground = true,
                Margin = new Margin
                {
                    Top = "15px",
                    Bottom = "15px",
                    Left = "15px",
                    Right = "15px"
                }
            });

            Console.WriteLine($"[ReportBuilder] PDF généré : {pdfPath}");

            // 5. Supprime le HTML si non désiré
            if (!keepHtml && File.Exists(htmlPath))
            {
                File.Delete(htmlPath);
            }

            // 6. Nettoie les screenshots si demandé
            if (cleanupScreenshots)
            {
                CleanupScreenshots();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ReportBuilder] Erreur lors de la génération PDF : {ex.Message}");
            Console.WriteLine($"[ReportBuilder] Fallback : le rapport HTML reste disponible à : {htmlPath}");
            // En cas d'erreur, on garde le HTML comme fallback
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Surcharge avec IPage existante (évite de créer une nouvelle page)
    /// </summary>
    public async Task BuildReportAsPdf(IPage page, bool keepHtml = false, bool cleanupScreenshots = true)
    {
        // 1. Génère d'abord le rapport HTML
        BuildReport();

        var htmlPath = Path.Combine(_outputPath, "index.html");
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var workflowPrefix = string.IsNullOrEmpty(_workflowName) ? "" : $"{_workflowName}_";
        var pdfFileName = $"{workflowPrefix}{_testName}_{timestamp}.pdf";
        var pdfPath = Path.Combine(_outputPath, pdfFileName);

        try
        {
            // 2. Charge le fichier HTML local
            await page.GotoAsync($"file://{Path.GetFullPath(htmlPath)}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // 3. Convertit en PDF
            await page.PdfAsync(new PagePdfOptions
            {
                Path = pdfPath,
                Format = "A4",
                PrintBackground = true,
                Margin = new Margin
                {
                    Top = "15px",
                    Bottom = "15px",
                    Left = "15px",
                    Right = "15px"
                }
            });

            Console.WriteLine($"[ReportBuilder] PDF généré : {pdfPath}");

            // 4. Supprime le HTML si non désiré
            if (!keepHtml && File.Exists(htmlPath))
            {
                File.Delete(htmlPath);
            }

            // 5. Nettoie les screenshots si demandé
            if (cleanupScreenshots)
            {
                CleanupScreenshots();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ReportBuilder] Erreur lors de la génération PDF : {ex.Message}");
            Console.WriteLine($"[ReportBuilder] Fallback : le rapport HTML reste disponible à : {htmlPath}");
            // En cas d'erreur, on garde le HTML comme fallback
        }
    }

    /// <summary>
    /// Supprime les fichiers screenshots PNG après génération du PDF
    /// </summary>
    private void CleanupScreenshots()
    {
        foreach (var screenshotPath in _screenshotFiles)
        {
            try
            {
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportBuilder] Impossible de supprimer {screenshotPath}: {ex.Message}");
            }
        }
        _screenshotFiles.Clear();
    }
}