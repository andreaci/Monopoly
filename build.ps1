[CmdletBinding()]
param(
    [string]$OutputDir = 'published',
    [string]$BasePath = '/',
    [switch]$SkipNpmInstall
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$frontendDir = Join-Path $repoRoot 'frontend'
$serverProject = Join-Path $repoRoot 'backend/Monopoly.Server/Monopoly.Server.csproj'
$publishDir = if ([IO.Path]::IsPathRooted($OutputDir)) { [IO.Path]::GetFullPath($OutputDir) } else { [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDir)) }
if ($publishDir.TrimEnd('\', '/') -eq $repoRoot.TrimEnd('\', '/')) { throw 'OutputDir must not be the repository root.' }

# Invoke npm beside Node directly, avoiding stale npm shims in the user's PATH.
$nodePath = (Get-Command node -ErrorAction Stop).Source
$npmCli = Join-Path (Split-Path $nodePath) 'node_modules/npm/bin/npm-cli.js'
function Invoke-Npm([string[]]$NpmArguments) {
    if (Test-Path -LiteralPath $npmCli) { & $nodePath $npmCli @NpmArguments }
    else { & npm @NpmArguments }
    if ($LASTEXITCODE -ne 0) { throw "npm failed with exit code $LASTEXITCODE" }
}

$normalizedBase = '/' + $BasePath.Trim('/')
if ($normalizedBase -ne '/') { $normalizedBase += '/' }
$previousBase = $env:VITE_BASE_PATH
$env:VITE_BASE_PATH = $normalizedBase
Push-Location $frontendDir
try {
    if (-not $SkipNpmInstall) { Invoke-Npm @('ci', '--no-audit', '--no-fund') }
    Invoke-Npm @('run', 'build')
} finally { Pop-Location; $env:VITE_BASE_PATH = $previousBase }

& dotnet publish $serverProject -c Release -o $publishDir --nologo -p:AllowMissingPrunePackageData=true
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
$webRoot = Join-Path $publishDir 'wwwroot'
New-Item -ItemType Directory -Path $webRoot -Force | Out-Null
Copy-Item -Path (Join-Path $frontendDir 'dist/*') -Destination $webRoot -Recurse -Force
@{ APP_BASE_PATH = $normalizedBase.TrimEnd('/') } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $publishDir 'appsettings.json') -Encoding utf8
Write-Host "Published frontend and backend to $publishDir"
Write-Host 'Start with:'
Write-Host "  dotnet `"$(Join-Path $publishDir 'Monopoly.Server.dll')`" --contentRoot `"$publishDir`""
Write-Host "Then open http://localhost:5080$normalizedBase on the server PC."
