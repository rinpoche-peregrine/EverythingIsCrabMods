# publish.ps1 — one-shot GitHub publish for the Skip Intro mod.
# Run from the project root: PS> .\publish.ps1
# Requires: gh CLI installed and authenticated (`gh auth login`).

[CmdletBinding()]
param(
	[string]$RepoName = "EverythingIsCrabMods",
	[ValidateSet("public", "private")] [string]$Visibility = "public",
	[string]$CommitName = "Bungus"
)

$ErrorActionPreference = "Stop"

# Version comes from the manifest — single source of truth
$manifest = Get-Content "Packaging\manifest.json" -Raw | ConvertFrom-Json
$Version = $manifest.version_number
$Tag = "v$Version"
$ZipPath = "dist\Bungus-SkipIntro-$Version.zip"

# Who are we?
$ghUser = gh api user | ConvertFrom-Json
$Owner = $ghUser.login
if (-not $Owner) { throw "Couldn't determine GitHub user. Run 'gh auth login' first." }
$NoreplyEmail = "$($ghUser.id)+$Owner@users.noreply.github.com"
Write-Host "Publishing as: $Owner/$RepoName  (version $Version, $Visibility)"

# Build release zip if missing
if (-not (Test-Path $ZipPath)) {
	Write-Host "Building release zip..."
	Push-Location EverythingIsCrabPlugin
	try { dotnet build -c Release } finally { Pop-Location }
}
if (-not (Test-Path $ZipPath)) { throw "Build did not produce $ZipPath" }

# Git init + identity (set locally if not already set globally, using GitHub noreply email)
if (-not (Test-Path .git)) {
	git init -q
	git branch -M main
}
if (-not (git config user.email 2>$null)) {
	git config user.email $NoreplyEmail
	git config user.name $CommitName
	Write-Host "Set local git identity: $CommitName <$NoreplyEmail>"
}

# Stage + commit (no-op if nothing to commit)
git add .
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) { git commit -q -m "Release $Tag" }

# Suppress gh stderr noise via try/catch (gh non-zero exit -> non-terminating in PS by default,
# but we want clean console output). $LASTEXITCODE is the source of truth.
$prevEAP = $ErrorActionPreference; $ErrorActionPreference = "Continue"
gh repo view "$Owner/$RepoName" 2>&1 | Out-Null
$repoExists = ($LASTEXITCODE -eq 0)
$ErrorActionPreference = $prevEAP

if (-not $repoExists) {
	Write-Host "Creating new repo $Owner/$RepoName ..."
	gh repo create "$RepoName" --$Visibility --description "BepInEx mods for Everything is Crab by Bungus" --source=. --remote=origin --push
} else {
	Write-Host "Repo $Owner/$RepoName already exists; pushing latest."
	$hasOrigin = $(git remote get-url origin 2>$null)
	if (-not $hasOrigin) { git remote add origin "https://github.com/$Owner/$RepoName.git" }
	git push -u origin main
}

# Release: create new, or replace asset if tag exists
$ErrorActionPreference = "Continue"
gh release view $Tag 2>&1 | Out-Null
$releaseExists = ($LASTEXITCODE -eq 0)
$ErrorActionPreference = $prevEAP

if (-not $releaseExists) {
	Write-Host "Creating release $Tag ..."
	gh release create $Tag $ZipPath `
		--title "Skip Intro $Version" `
		--notes-file "Packaging\CHANGELOG.md"
} else {
	Write-Host "Release $Tag exists; re-uploading asset."
	gh release upload $Tag $ZipPath --clobber
}

Write-Host ""
Write-Host "Done. Release URL:"
Write-Host "  https://github.com/$Owner/$RepoName/releases/tag/$Tag"
