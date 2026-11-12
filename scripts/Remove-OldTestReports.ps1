param(
    [string]$ReportsDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) "REPORTS\Executions"),
    [int]$RetentionMonths = 3
)

$resolvedReportsDirectory = [System.IO.Path]::GetFullPath($ReportsDirectory)
$repositoryRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$expectedReportsDirectory = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot "REPORTS\Executions"))

if (-not $resolvedReportsDirectory.Equals(
        $expectedReportsDirectory,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Le nettoyage est limité au dossier '$expectedReportsDirectory'."
}

if ($RetentionMonths -lt 1) {
    throw "La durée de conservation doit être d'au moins un mois."
}

if (-not (Test-Path -LiteralPath $resolvedReportsDirectory -PathType Container)) {
    Write-Output "Aucun ancien rapport à nettoyer."
    exit 0
}

$cutoff = (Get-Date).AddMonths(-$RetentionMonths)
$oldReports = @(Get-ChildItem -LiteralPath $resolvedReportsDirectory -Directory |
    Where-Object { $_.LastWriteTime -lt $cutoff })

foreach ($report in $oldReports) {
    $resolvedReport = [System.IO.Path]::GetFullPath($report.FullName)
    if (-not $resolvedReport.StartsWith(
            "$resolvedReportsDirectory$([System.IO.Path]::DirectorySeparatorChar)",
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Le dossier à supprimer est en dehors du répertoire des rapports : $resolvedReport"
    }

    Remove-Item -LiteralPath $resolvedReport -Recurse -Force
    Write-Output "Ancien rapport supprimé : $resolvedReport"
}

if ($oldReports.Count -eq 0) {
    Write-Output "Aucun rapport de plus de $RetentionMonths mois."
}
