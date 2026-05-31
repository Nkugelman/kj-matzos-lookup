# Publish Confluence docs from docs/.
#
#   .\docs\publish-confluence.ps1              # customer proposal + mockups
#   .\docs\publish-confluence.ps1 -Developer # internal developer spec
#   .\docs\publish-confluence.ps1 -DryRun
#
# Setup: copy docs\.env.example → docs\.env and set CONFLUENCE_API_TOKEN

param(
    [switch]$Developer,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Import-DotEnvFile {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#")) { return }
        $eq = $line.IndexOf("=")
        if ($eq -lt 1) { return }
        $name = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim().Trim('"').Trim("'")
        Set-Item -Path "Env:$name" -Value $value -Force
    }
}

function Get-ConfluencePageAttachments {
    param(
        [string]$PageId,
        [string]$Domain,
        [string]$BasicAuth
    )
    $url = "https://$Domain/wiki/rest/api/content/$PageId/child/attachment?limit=100&expand=version"
    $json = curl.exe -sS -H "Authorization: Basic $BasicAuth" $url
    if ($LASTEXITCODE -ne 0) { throw "Failed to list attachments" }
    ($json | ConvertFrom-Json).results
}

function Remove-ConfluenceAttachments {
    param(
        [string]$Domain,
        [string]$BasicAuth,
        [object[]]$Attachments
    )
    foreach ($att in $Attachments) {
        $id = $att.id
        $title = $att.title
        Write-Host "  Remove: $title ($id)" -ForegroundColor DarkGray
        curl.exe -sS -X DELETE `
            -H "Authorization: Basic $BasicAuth" `
            "https://$Domain/wiki/rest/api/content/$id" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Failed to delete attachment $title" }
    }
}

function Upload-ConfluenceAttachments {
    param(
        [string]$PageId,
        [string]$Domain,
        [string]$BasicAuth,
        [string[]]$Files
    )
    $uploadUrl = "https://$Domain/wiki/rest/api/content/$PageId/child/attachment"
    foreach ($file in $Files) {
        if (-not (Test-Path $file)) {
            Write-Warning "Skipping missing attachment: $file"
            continue
        }
        $name = Split-Path $file -Leaf
        $localSize = (Get-Item $file).Length
        Write-Host "  Upload: $name ($localSize bytes)" -ForegroundColor DarkGray
        $response = curl.exe -sS -X POST `
            -H "Authorization: Basic $BasicAuth" `
            -H "X-Atlassian-Token: no-check" `
            -F "file=@$file;filename=$name" `
            $uploadUrl
        if ($LASTEXITCODE -ne 0) { throw "Failed to upload $name" }
        $uploaded = ($response | ConvertFrom-Json).results[0]
        $remoteSize = $uploaded.extensions.fileSize
        if ($remoteSize -ne $localSize) {
            throw "Upload size mismatch for ${name}: local=$localSize remote=$remoteSize"
        }
    }
}

function Find-ConfluencePageByTitle {
    param(
        [string]$Domain,
        [string]$BasicAuth,
        [string]$Space,
        [string]$Title
    )
    $cql = [uri]::EscapeDataString("space=$Space and title=`"$Title`" and type=page")
    $url = "https://$Domain/wiki/rest/api/content/search?cql=$cql&limit=1"
    $json = curl.exe -sS -H "Authorization: Basic $BasicAuth" $url
    if ($LASTEXITCODE -ne 0) { throw "Failed to search Confluence pages" }
    $result = ($json | ConvertFrom-Json).results
    if ($result -and $result.Count -gt 0) { return $result[0].id }
    return $null
}

function New-ConfluencePage {
    param(
        [string]$Domain,
        [string]$BasicAuth,
        [string]$Space,
        [string]$Title,
        [string]$ParentPageId
    )
    $payload = @{
        type  = "page"
        title = $Title
        space = @{ key = $Space }
        body  = @{
            storage = @{
                value          = "<p>Placeholder - run publish script to sync markdown.</p>"
                representation = "storage"
            }
        }
    }
    if ($ParentPageId) {
        $payload.ancestors = @(@{ id = $ParentPageId })
    }
    $jsonPath = Join-Path $env:TEMP "confluence-new-page.json"
    $payload | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonPath -Encoding utf8
    $response = curl.exe -sS -X POST `
        -H "Authorization: Basic $BasicAuth" `
        -H "Content-Type: application/json" `
        --data-binary "@$jsonPath" `
        "https://$Domain/wiki/rest/api/content"
    Remove-Item $jsonPath -Force -ErrorAction SilentlyContinue
    if ($LASTEXITCODE -ne 0) { throw "Failed to create Confluence page" }
    ($response | ConvertFrom-Json).id
}

function Set-MarkdownPageId {
    param(
        [string]$FilePath,
        [string]$PageId
    )
    $content = Get-Content $FilePath -Raw
    if ($content -match '(?m)^confluence-page-id:\s*\d+\s*$') {
        $content = $content -replace '(?m)^confluence-page-id:\s*\d+\s*$', "confluence-page-id: $PageId"
    }
    else {
        $content = $content -replace '(?m)^(confluence-space:.*)$', "`$1`nconfluence-page-id: $PageId"
    }
    Set-Content -Path $FilePath -Value $content.TrimEnd() -Encoding utf8 -NoNewline
    Add-Content -Path $FilePath -Value "`n" -Encoding utf8
}

$repoRoot = Split-Path $PSScriptRoot -Parent
Import-DotEnvFile (Join-Path $PSScriptRoot ".env")
Import-DotEnvFile (Join-Path $repoRoot ".env")

$domain = if ($env:CONFLUENCE_DOMAIN) { $env:CONFLUENCE_DOMAIN } else { "rdtsystems.atlassian.net" }
$space = if ($env:CONFLUENCE_SPACE) { $env:CONFLUENCE_SPACE } else { "KJMATZ" }
$email = if ($env:CONFLUENCE_EMAIL) { $env:CONFLUENCE_EMAIL } else { "naftoli@rdtsystems.com" }
$token = $env:CONFLUENCE_API_TOKEN

if (-not $token) {
    Write-Host "Missing Confluence API token." -ForegroundColor Red
    Write-Host ""
    Write-Host "  Copy docs\.env.example to docs\.env"
    Write-Host "  Set CONFLUENCE_API_TOKEN=your-token"
    Write-Host ""
    exit 1
}

if ($Developer) {
    $mdFileName = "KJ-Matzos-Confluence-Developer.md"
    $defaultPageId = $env:CONFLUENCE_DEVELOPER_PAGE_ID
    $pageTitle = "KJ Matzos - Developer Spec (Internal)"
    $parentPageId = "164167681"
    $uploadMockups = $false
}
else {
    $mdFileName = "KJ-Matzos-Confluence.md"
    $defaultPageId = "164167681"
    $pageTitle = $null
    $parentPageId = $null
    $uploadMockups = $true
}

$mdFile = Join-Path $PSScriptRoot $mdFileName
if (-not (Test-Path $mdFile)) {
    throw "Not found: $mdFile"
}

$basicAuth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${email}:${token}"))
$pageId = $defaultPageId

if ($Developer -and -not $pageId) {
    $pageId = Find-ConfluencePageByTitle -Domain $domain -BasicAuth $basicAuth -Space $space -Title $pageTitle
    if (-not $pageId) {
        Write-Host "Creating Confluence page: $pageTitle" -ForegroundColor Cyan
        $pageId = New-ConfluencePage -Domain $domain -BasicAuth $basicAuth -Space $space -Title $pageTitle -ParentPageId $parentPageId
        Set-MarkdownPageId -FilePath $mdFile -PageId $pageId
        Write-Host "Created page id $pageId - add CONFLUENCE_DEVELOPER_PAGE_ID=$pageId to docs\.env" -ForegroundColor Yellow
    }
}

if ($Developer) {
    $frontmatter = Get-Content $mdFile -Raw
    if ($frontmatter -notmatch 'confluence-page-id:\s*(\d+)') {
        Set-MarkdownPageId -FilePath $mdFile -PageId $pageId
    }
    elseif ($Matches[1] -ne $pageId) {
        $pageId = $Matches[1]
    }
}

$mockupFiles = @(
    (Join-Path $PSScriptRoot "mockups\item_sale_1.png"),
    (Join-Path $PSScriptRoot "mockups\item_sale_2.png"),
    (Join-Path $PSScriptRoot "mockups\item_sale_3.png")
)

Push-Location $PSScriptRoot
try {
    $npxArgs = @(
        "--yes", "markfluence",
        "--domain", $domain,
        "--space", $space,
        "--user", $email,
        "--token", $token,
        "-v",
        $mdFileName
    )
    if ($DryRun) { $npxArgs = @("--yes", "markfluence", "--dry-run") + $npxArgs[2..($npxArgs.Length - 1)] }

    Write-Host "Target: https://$domain/wiki/spaces/$space/pages/$pageId" -ForegroundColor Cyan
    & npx @npxArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not $DryRun -and $uploadMockups) {
        Write-Host "Replacing mockup attachments..." -ForegroundColor Cyan
        $existing = Get-ConfluencePageAttachments -PageId $pageId -Domain $domain -BasicAuth $basicAuth
        $mockupNames = $mockupFiles | ForEach-Object { Split-Path $_ -Leaf }
        $toRemove = $existing | Where-Object {
            $_.title -like "item_sale*" -or $_.title -in $mockupNames
        }
        if ($toRemove) {
            Remove-ConfluenceAttachments -Domain $domain -BasicAuth $basicAuth -Attachments $toRemove
        }
        Upload-ConfluenceAttachments -PageId $pageId -Domain $domain -BasicAuth $basicAuth -Files $mockupFiles
    }

    Write-Host "Published: https://$domain/wiki/spaces/$space/pages/$pageId" -ForegroundColor Green
}
finally {
    Pop-Location
}
