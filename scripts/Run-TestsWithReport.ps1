param(
    [string]$Filter,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$projectPath = Join-Path $repositoryRoot "TestPlayrights.csproj"
$executionsDirectory = Join-Path $repositoryRoot "REPORTS\Executions"
$runId = Get-Date -Format "yyyyMMdd-HHmmssfff"
$runDirectory = Join-Path $executionsDirectory $runId
$evidenceDirectory = Join-Path $runDirectory "Evidence"
$testResultsDirectory = Join-Path $runDirectory "TestResults"
$legacyReportsDirectory = Join-Path $runDirectory "LegacyReports"

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Projet .NET introuvable : $projectPath"
}

New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $testResultsDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $legacyReportsDirectory -Force | Out-Null

$cleanupScript = Join-Path $PSScriptRoot "Remove-OldTestReports.ps1"
& $cleanupScript

$previousEvidenceDirectory = $env:TEST_EVIDENCE_DIRECTORY
$previousReportsPath = $env:REPORTS_PATH
$env:TEST_EVIDENCE_DIRECTORY = $evidenceDirectory
$env:REPORTS_PATH = $legacyReportsDirectory

try {
    $testArguments = @(
        "test",
        $projectPath,
        "--configuration", $Configuration,
        "--logger", "trx;LogFileName=results.trx",
        "--results-directory", $testResultsDirectory
    )
    if ($NoBuild) {
        $testArguments += "--no-build"
    }
    if (-not [string]::IsNullOrWhiteSpace($Filter)) {
        $testArguments += @("--filter", $Filter)
    }

    Write-Output "Résultats de cette exécution : $runDirectory"
    & dotnet @testArguments
    $testExitCode = $LASTEXITCODE

    $trxPath = Join-Path $testResultsDirectory "results.trx"
    if (Test-Path -LiteralPath $trxPath -PathType Leaf) {
        [xml]$trx = Get-Content -LiteralPath $trxPath -Raw
        $testResults = @($trx.SelectNodes("//*[local-name()='UnitTestResult']"))
        $evidenceItems = @(
            Get-ChildItem -LiteralPath $evidenceDirectory -Filter "*.json" -File |
                ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json }
        )

        $passedCount = @($testResults | Where-Object { $_.GetAttribute("outcome") -eq "Passed" }).Count
        $failedCount = @($testResults | Where-Object { $_.GetAttribute("outcome") -eq "Failed" }).Count
        $otherCount = $testResults.Count - $passedCount - $failedCount
        $html = [System.Text.StringBuilder]::new()
        [void]$html.AppendLine("<!doctype html><html lang='fr'><head><meta charset='utf-8'>")
        [void]$html.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>")
        [void]$html.AppendLine("<title>Rapport d'exécution des tests</title><style>")
        [void]$html.AppendLine("body{font:15px Segoe UI,Arial,sans-serif;max-width:1200px;margin:2rem auto;padding:0 1rem;color:#222}h1{margin-bottom:.3rem}.summary{display:flex;gap:1rem;margin:1.5rem 0}.card{padding:1rem;border:1px solid #ccc;border-radius:6px}.passed{color:#176b32}.failed{color:#a32222}details{border:1px solid #ccc;border-radius:5px;margin:1rem 0;padding:.8rem}summary{cursor:pointer;font-weight:600}.evidence img{max-width:100%;max-height:800px;border:1px solid #bbb;margin:.5rem 0}.error{white-space:pre-wrap;background:#fff3f3;padding:.8rem;overflow-wrap:anywhere}.finding{border-left:4px solid #b22;padding:.5rem 1rem;margin:.75rem 0;background:#fffafa}.finding h3{margin:.25rem 0}.page-url{font-size:1.05rem;font-weight:600;background:#eef5ff;padding:.75rem}code{overflow-wrap:anywhere}")
        [void]$html.AppendLine("</style></head><body>")
        [void]$html.AppendLine("<h1>Rapport d'exécution des tests</h1>")
        [void]$html.AppendLine("<p>Exécution : $( [System.Net.WebUtility]::HtmlEncode($runId) )</p>")
        [void]$html.AppendLine("<section class='summary'><div class='card'>Total : $($testResults.Count)</div><div class='card passed'>Réussis : $passedCount</div><div class='card failed'>Échoués : $failedCount</div><div class='card'>Autres : $otherCount</div></section>")

        foreach ($result in $testResults) {
            $testName = $result.GetAttribute("testName")
            $outcome = $result.GetAttribute("outcome")
            $duration = $result.GetAttribute("duration")
            $encodedName = [System.Net.WebUtility]::HtmlEncode($testName)
            $encodedOutcome = [System.Net.WebUtility]::HtmlEncode($outcome)
            $encodedDuration = [System.Net.WebUtility]::HtmlEncode($duration)
            $statusClass = if ($outcome -eq "Passed") { "passed" } elseif ($outcome -eq "Failed") { "failed" } else { "" }

            [void]$html.AppendLine("<details><summary class='$statusClass'>$encodedOutcome — $encodedName <span>($encodedDuration)</span></summary>")
            $resultMessage = $result.SelectSingleNode("./*[local-name()='Output']/*[local-name()='ErrorInfo']/*[local-name()='Message']")
            $stackTrace = $result.SelectSingleNode("./*[local-name()='Output']/*[local-name()='ErrorInfo']/*[local-name()='StackTrace']")
            $standardOutput = $result.SelectSingleNode("./*[local-name()='Output']/*[local-name()='StdOut']")
            $messageText = if ($resultMessage) { $resultMessage.InnerText } else { "" }
            $pageMatch = [regex]::Match($messageText, '(https?://[^\s<]+)')
            if ($pageMatch.Success) {
                $pageUrl = $pageMatch.Groups[1].Value.TrimEnd('.', ',', ')')
                $pageUrlHtml = [System.Net.WebUtility]::HtmlEncode($pageUrl)
                [void]$html.AppendLine("<p class='page-url'>Page concernée : <a href='$pageUrlHtml'>$pageUrlHtml</a></p>")
            }

            $findingMatches = [regex]::Matches(
                $messageText,
                '(?m)^\s*([a-z0-9][a-z0-9-]+)\s+\((critical|serious|moderate|minor)\)\s*:\s*(.+)$')
            for ($findingIndex = 0; $findingIndex -lt $findingMatches.Count; $findingIndex++) {
                $finding = $findingMatches[$findingIndex]
                $ruleId = $finding.Groups[1].Value
                $impact = $finding.Groups[2].Value
                $description = $finding.Groups[3].Value.Trim()
                $segmentEnd = if ($findingIndex + 1 -lt $findingMatches.Count) {
                    $findingMatches[$findingIndex + 1].Index
                } else {
                    $messageText.Length
                }
                $findingDetails = $messageText.Substring(
                    $finding.Index,
                    $segmentEnd - $finding.Index)
                $ruleHtml = [System.Net.WebUtility]::HtmlEncode($ruleId)
                $impactHtml = [System.Net.WebUtility]::HtmlEncode($impact)
                $descriptionHtml = [System.Net.WebUtility]::HtmlEncode($description)
                [void]$html.AppendLine("<section class='finding'><h3>$impactHtml — $ruleHtml</h3><p>$descriptionHtml</p>")

                $targetMatch = [regex]::Match(
                    $findingDetails,
                    "(?m)^\s*Cible sur cette page\s*:\s*(.+)$")
                if (-not $targetMatch.Success) {
                    $targetMatch = [regex]::Match($findingDetails, '(?m)^\s*Cible\s*:\s*(.+)$')
                }
                if ($targetMatch.Success) {
                    $targetHtml = [System.Net.WebUtility]::HtmlEncode($targetMatch.Groups[1].Value.Trim())
                    [void]$html.AppendLine("<p>Élément concerné sur cette page : <code>$targetHtml</code></p>")
                }

                $ruleHelpMatch = [regex]::Match($findingDetails, '(?m)^\s*Règle axe\s*:\s*(.+)$')
                if ($ruleHelpMatch.Success) {
                    $ruleHelp = [System.Net.WebUtility]::HtmlEncode($ruleHelpMatch.Groups[1].Value.Trim())
                    [void]$html.AppendLine("<p>Ce que vérifie la règle : $ruleHelp</p>")
                }
                $ruleUrlMatch = [regex]::Match($findingDetails, '(?m)^\s*Détails de la règle\s*:\s*(https?://\S+)')
                if ($ruleUrlMatch.Success) {
                    $ruleUrl = $ruleUrlMatch.Groups[1].Value.Trim()
                    $ruleUrlHtml = [System.Net.WebUtility]::HtmlEncode($ruleUrl)
                    [void]$html.AppendLine("<p><a href='$ruleUrlHtml'>Explication complète de cette règle</a></p>")
                }
                $axeChecks = [regex]::Matches($findingDetails, '(?m)^\s*Vérification axe\s*:\s*(.+)$')
                foreach ($axeCheck in $axeChecks) {
                    $checkText = [System.Net.WebUtility]::HtmlEncode($axeCheck.Groups[1].Value.Trim())
                    [void]$html.AppendLine("<p>Constat axe-core : $checkText</p>")
                }

                $contextCaptures = @(
                    Get-ChildItem -LiteralPath $evidenceDirectory -Filter "*.png" -File |
                        Where-Object {
                            $_.Name.StartsWith("$testName`_", [System.StringComparison]::OrdinalIgnoreCase) -and
                            $_.Name -match "_violation_$([regex]::Escape($ruleId))_\d+_context\.png$"
                        }
                )
                foreach ($capture in $contextCaptures) {
                    $imagePath = "Evidence/$($capture.Name)"
                    $imagePathHtml = [System.Net.WebUtility]::HtmlEncode($imagePath)
                    $imageNameHtml = [System.Net.WebUtility]::HtmlEncode($capture.Name)
                    [void]$html.AppendLine("<p>Contexte complet — cible encadrée en rouge :</p><a href='$imagePathHtml'><img src='$imagePathHtml' alt='Page complète avec l’élément signalé encadré'></a><p><code>$imageNameHtml</code></p>")
                }
                [void]$html.AppendLine("</section>")
            }

            if ($resultMessage -or $stackTrace) {
                $stackText = if ($stackTrace) { $stackTrace.InnerText } else { "" }
                $failureDetails = [System.Net.WebUtility]::HtmlEncode("$messageText`n$stackText")
                [void]$html.AppendLine("<details><summary>Détails techniques complets</summary><pre class='error'>$failureDetails</pre></details>")
            }
            if ($standardOutput -and -not [string]::IsNullOrWhiteSpace($standardOutput.InnerText)) {
                $outputText = [System.Net.WebUtility]::HtmlEncode($standardOutput.InnerText)
                [void]$html.AppendLine("<details><summary>Sortie du test</summary><pre>$outputText</pre></details>")
            }

            $matchingEvidence = @($evidenceItems | Where-Object {
                $_.TestName -eq $testName -or
                $_.FullName -eq $testName -or
                $testName.EndsWith(".$($_.TestName)", [System.StringComparison]::OrdinalIgnoreCase)
            })
            foreach ($evidence in $matchingEvidence) {
                $safeTestName = $evidence.TestName -replace '[<>:"/\\|?*]', '_'
                $safeTestName = $safeTestName.Replace(' ', '_')
                $capturePrefix = "${safeTestName}_"
                $testCaptures = @(
                    Get-ChildItem -LiteralPath $evidenceDirectory -Filter "*.png" -File |
                        Where-Object { $_.Name.StartsWith($capturePrefix, [System.StringComparison]::OrdinalIgnoreCase) }
                )
                foreach ($capture in $testCaptures) {
                    if ($capture.Name -match "_violation_.+_\d+_context\.png$") {
                        continue
                    }
                    $imagePath = "Evidence/$($capture.Name)"
                    $imagePathHtml = [System.Net.WebUtility]::HtmlEncode($imagePath)
                    $imageNameHtml = [System.Net.WebUtility]::HtmlEncode($capture.Name)
                    $captureLabel = if ($capture.Name -match "_page\.png$") {
                        "Capture complète de la page contrôlée"
                    } else {
                        "Capture de fin de test"
                    }
                    $captureLabelHtml = [System.Net.WebUtility]::HtmlEncode($captureLabel)
                    [void]$html.AppendLine("<div class='evidence'><p>$captureLabelHtml — <code>$imageNameHtml</code></p><a href='$imagePathHtml'><img src='$imagePathHtml' alt='$captureLabelHtml'></a></div>")
                }

                if ($testCaptures.Count -eq 0 -and $evidence.CaptureError) {
                    $captureError = [System.Net.WebUtility]::HtmlEncode($evidence.CaptureError)
                    [void]$html.AppendLine("<p>La capture a échoué : $captureError</p>")
                } elseif ($testCaptures.Count -eq 0) {
                    [void]$html.AppendLine("<p>Capture indisponible : la page du navigateur n'était pas ouverte au terme du test.</p>")
                }

                if ($evidence.PageUrl) {
                    $pageUrlHtml = [System.Net.WebUtility]::HtmlEncode($evidence.PageUrl)
                    [void]$html.AppendLine("<p>Page : <a href='$pageUrlHtml'>$pageUrlHtml</a></p>")
                }
            }
            [void]$html.AppendLine("</details>")
        }

        $unmatchedEvidence = @($evidenceItems | Where-Object {
            $evidence = $_
            -not ($testResults | Where-Object {
                $testName = $_.GetAttribute("testName")
                $evidence.TestName -eq $testName -or
                $evidence.FullName -eq $testName -or
                $testName.EndsWith(".$($evidence.TestName)", [System.StringComparison]::OrdinalIgnoreCase)
            })
        })
        if ($unmatchedEvidence.Count -gt 0) {
            [void]$html.AppendLine("<h2>Preuves non associées à un résultat TRX</h2>")
            foreach ($evidence in $unmatchedEvidence) {
                $evidenceName = [System.Net.WebUtility]::HtmlEncode($evidence.FullName)
                [void]$html.AppendLine("<p>$evidenceName</p>")
                if ($evidence.Screenshot) {
                    $imagePath = [System.Net.WebUtility]::HtmlEncode("Evidence/$($evidence.Screenshot)")
                    [void]$html.AppendLine("<a href='$imagePath'><img class='evidence' src='$imagePath' alt='Capture du test'></a>")
                }
            }
        }

        $legacyPdfs = @(Get-ChildItem -LiteralPath $legacyReportsDirectory -Filter "*.pdf" -File -Recurse)
        if ($legacyPdfs.Count -gt 0) {
            [void]$html.AppendLine("<h2>Rapports PDF historiques</h2><ul>")
            foreach ($pdf in $legacyPdfs) {
                $relativePdf = [System.IO.Path]::GetRelativePath($runDirectory, $pdf.FullName).Replace("\", "/")
                $encodedPdf = [System.Net.WebUtility]::HtmlEncode($relativePdf)
                $encodedPdfName = [System.Net.WebUtility]::HtmlEncode($pdf.Name)
                [void]$html.AppendLine("<li><a href='$encodedPdf'>$encodedPdfName</a></li>")
            }
            [void]$html.AppendLine("</ul>")
        }

        [void]$html.AppendLine("</body></html>")
        $reportPath = Join-Path $runDirectory "index.html"
        [System.IO.File]::WriteAllText($reportPath, $html.ToString(), [System.Text.UTF8Encoding]::new($false))
        Write-Output "Rapport HTML créé : $reportPath"
    } else {
        Write-Warning "Aucun résultat TRX n'a été créé; le rapport HTML n'a pas pu être construit."
        if ($testExitCode -eq 0) {
            $testExitCode = 1
        }
    }
}
finally {
    $env:TEST_EVIDENCE_DIRECTORY = $previousEvidenceDirectory
    $env:REPORTS_PATH = $previousReportsPath
}

exit $testExitCode
