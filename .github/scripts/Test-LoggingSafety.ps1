[CmdletBinding()]
param(
    [Parameter()]
    [string]$RepositoryRoot = "."
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedRepositoryRoot = Resolve-Path -Path $RepositoryRoot
$targetDirectories = @(
    (Join-Path -Path $resolvedRepositoryRoot -ChildPath "marginalia-service/src/Api"),
    (Join-Path -Path $resolvedRepositoryRoot -ChildPath "marginalia-service/src/Infrastructure")
)

$logCallPattern = [regex]'Log(?:Information|Warning|Error|Debug|Trace)\s*\((?<args>[\s\S]*?)\);'
$sensitivePlaceholderPattern = [regex]'\{(?:AccessCode|Content|FileName|Guidance|Prompt|Text|Title|Transcript)\}'
$sensitiveArgumentPattern = [regex]'(?:\bprovidedCode\b|\baccessCode\b|\bsourceFilePath\b|\bresultFilePath\b|\bjob\.ResultFilePath\b|\bfile\.FileName\b|\bdocument\.Filename\b|\brequest\.Title\b(?!\.Length)|\beffectiveUserInstructions\b|\beffectiveToneGuidance\b)'

$violations = New-Object System.Collections.Generic.List[object]

foreach ($targetDirectory in $targetDirectories) {
    if (-not (Test-Path -Path $targetDirectory -PathType Container)) {
        continue
    }

    $files = Get-ChildItem -Path $targetDirectory -Filter '*.cs' -Recurse -File
    foreach ($file in $files) {
        $content = Get-Content -Path $file.FullName -Raw
        if ([string]::IsNullOrWhiteSpace($content)) {
            continue
        }

        $matches = $logCallPattern.Matches($content)
        foreach ($match in $matches) {
            $invocationText = $match.Value
            if (-not ($sensitivePlaceholderPattern.IsMatch($invocationText) -or $sensitiveArgumentPattern.IsMatch($invocationText))) {
                continue
            }

            $lineNumber = (($content.Substring(0, $match.Index) -split "`n").Count)
            $trimmedInvocation = $invocationText.Trim() -replace "`r", ' '
            if ($trimmedInvocation.Length -gt 240) {
                $trimmedInvocation = "$($trimmedInvocation.Substring(0, 240))..."
            }

            $relativePath = [System.IO.Path]::GetRelativePath($resolvedRepositoryRoot.Path, $file.FullName)
            $violations.Add([PSCustomObject]@{
                    Path       = $relativePath
                    Line       = $lineNumber
                    Invocation = $trimmedInvocation
                })
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Error "Logging safety validation failed. Found $($violations.Count) potentially unsafe log statements."
    foreach ($violation in $violations) {
        Write-Error " - $($violation.Path):$($violation.Line) :: $($violation.Invocation)"
    }

    exit 1
}

Write-Host "Logging safety validation passed. No unsafe log statements found in API/Infrastructure logging calls."
