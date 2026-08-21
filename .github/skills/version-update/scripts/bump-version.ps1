param(
    [Parameter(Mandatory = $false)]
    [string]$ToVersion
)

$propsPath = "Directory.Build.props"

if (-not (Test-Path -Path $propsPath)) {
    throw "Could not find $propsPath in the current working directory."
}

$propsXml = [xml](Get-Content -Path $propsPath -Raw)
$currentNode = $propsXml.SelectSingleNode("//Version")

if ($null -eq $currentNode -or [string]::IsNullOrWhiteSpace($currentNode.InnerText)) {
    throw "Could not read <Version> from $propsPath."
}

$oldNumeric = $currentNode.InnerText.Trim().TrimStart('v', 'V')

if ([string]::IsNullOrWhiteSpace($oldNumeric)) {
    throw "Current version in $propsPath is empty."
}

if ([string]::IsNullOrWhiteSpace($ToVersion)) {
    $parts = $oldNumeric -split '\.'

    if ($parts.Count -lt 1) {
        throw "Current version '$oldNumeric' is not in a supported dotted format."
    }

    $lastIndex = $parts.Count - 1
    $lastPart = 0

    if (-not [int]::TryParse($parts[$lastIndex], [ref]$lastPart)) {
        throw "Cannot auto-increment version '$oldNumeric' because last group '$($parts[$lastIndex])' is not numeric."
    }

    $parts[$lastIndex] = ($lastPart + 1).ToString()
    $newNumeric = [string]::Join('.', $parts)
}
else {
    $newNumeric = $ToVersion.Trim().TrimStart('v', 'V')
}

if ([string]::IsNullOrWhiteSpace($newNumeric)) {
    throw "Target version is empty."
}

if ($oldNumeric -eq $newNumeric) {
    throw "Current version and target version are the same ($oldNumeric)."
}

$escapedOld = [Regex]::Escape($oldNumeric)
$searchPattern = "v$escapedOld|$escapedOld"

$files = git grep -l -E $searchPattern -- . ":(exclude).vs/**" ":(exclude)**/bin/**" ":(exclude)**/obj/**"

if (-not $files) {
    Write-Host "No tracked files found containing v$oldNumeric or $oldNumeric."
    exit 0
}

$updated = @()

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file)
    $next = $content.Replace("v$oldNumeric", "v$newNumeric").Replace($oldNumeric, $newNumeric)

    if ($next -ne $content) {
        [System.IO.File]::WriteAllText($file, $next)
        $updated += $file
    }
}

Write-Host "Source version: v$oldNumeric"
Write-Host "Target version: v$newNumeric"
Write-Host "Updated files:" 
$updated | ForEach-Object { Write-Host "- $_" }

Write-Host "`nOld version residual check:"
$residual = git grep -n -E $searchPattern -- . ":(exclude).vs/**" ":(exclude)**/bin/**" ":(exclude)**/obj/**"

if ($LASTEXITCODE -eq 1) {
    Write-Host "No residual matches found."
    exit 0
}

if ($LASTEXITCODE -ne 0) {
    throw "Residual check failed with exit code $LASTEXITCODE."
}

$residual
