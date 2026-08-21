param(
    [Parameter(Mandatory = $false)]
    [string]$Version
)

if ([string]::IsNullOrWhiteSpace($Version)) {
    $propsPath = "Directory.Build.props"

    if (-not (Test-Path -Path $propsPath)) {
        throw "Could not find $propsPath in the current working directory."
    }

    $propsXml = [xml](Get-Content -Path $propsPath -Raw)
    $currentNode = $propsXml.SelectSingleNode("//Version")

    if ($null -eq $currentNode -or [string]::IsNullOrWhiteSpace($currentNode.InnerText)) {
        throw "Could not read <Version> from $propsPath."
    }

    $numericVersion = $currentNode.InnerText.Trim().TrimStart('v', 'V')
}
else {
    $numericVersion = $Version.Trim().TrimStart('v', 'V')
}

if ([string]::IsNullOrWhiteSpace($numericVersion)) {
    throw "Version cannot be empty."
}

$escaped = [Regex]::Escape($numericVersion)
$pattern = "v$escaped|$escaped"

git grep -n -E $pattern -- . ":(exclude).vs/**" ":(exclude)**/bin/**" ":(exclude)**/obj/**"
