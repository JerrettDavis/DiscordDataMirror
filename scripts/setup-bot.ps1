<#
.SYNOPSIS
    Sets up the Discord bot for DiscordDataMirror.

.DESCRIPTION
    This script:
    - Checks prerequisites (dotnet, user-secrets)
    - Prompts for the Discord bot token
    - Stores the token in .NET user secrets
    - Validates the token with Discord API
    - Generates the OAuth2 invite URL

.EXAMPLE
    .\setup-bot.ps1

.EXAMPLE
    .\setup-bot.ps1 -Token "your-token-here" -SkipValidation
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Token,

    [Parameter(Mandatory = $false)]
    [switch]$SkipValidation
)

# Configuration
$BotProjectPath = Join-Path $PSScriptRoot "..\src\DiscordDataMirror.Bot"
$PermissionsInteger = 66560  # View Channels + Read Message History
$Scopes = "bot"

# Colors for output
function Write-Success { param($Message) Write-Host "✅ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "ℹ️  $Message" -ForegroundColor Cyan }
function Write-Warn { param($Message) Write-Host "⚠️  $Message" -ForegroundColor Yellow }
function Write-Fail { param($Message) Write-Host "❌ $Message" -ForegroundColor Red }

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Blue
Write-Host "║          DiscordDataMirror Bot Setup                         ║" -ForegroundColor Blue
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Blue
Write-Host ""

# Check prerequisites
Write-Info "Checking prerequisites..."

# Check dotnet
$dotnetVersion = $null
try {
    $dotnetVersion = dotnet --version 2>$null
} catch {}

if (-not $dotnetVersion) {
    Write-Fail ".NET SDK not found. Please install .NET 10 SDK."
    Write-Host "   Download: https://dotnet.microsoft.com/download"
    exit 1
}
Write-Success ".NET SDK $dotnetVersion found"

# Check if bot project exists
if (-not (Test-Path $BotProjectPath)) {
    Write-Fail "Bot project not found at: $BotProjectPath"
    Write-Host "   Make sure you're running this from the repository root."
    exit 1
}
Write-Success "Bot project found"

# Check user-secrets initialization
$csprojPath = Join-Path $BotProjectPath "DiscordDataMirror.Bot.csproj"
$csprojContent = Get-Content $csprojPath -Raw
if ($csprojContent -notmatch "UserSecretsId") {
    Write-Info "Initializing user secrets..."
    Push-Location $BotProjectPath
    dotnet user-secrets init
    Pop-Location
}
Write-Success "User secrets configured"

# Get token
Write-Host ""
if (-not $Token) {
    Write-Host "📋 To get your bot token:" -ForegroundColor Yellow
    Write-Host "   1. Go to https://discord.com/developers/applications"
    Write-Host "   2. Select your application (or create one)"
    Write-Host "   3. Go to 'Bot' section"
    Write-Host "   4. Click 'Reset Token' and copy it"
    Write-Host ""
    
    $secureToken = Read-Host -Prompt "Enter your Discord bot token" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
    $Token = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    Write-Fail "No token provided. Exiting."
    exit 1
}

# Basic token format validation
if ($Token.Length -lt 50) {
    Write-Warn "Token seems too short. Discord tokens are typically 70+ characters."
}

# Validate token with Discord API
$clientId = $null
if (-not $SkipValidation) {
    Write-Host ""
    Write-Info "Validating token with Discord API..."
    
    try {
        $headers = @{
            "Authorization" = "Bot $Token"
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-RestMethod -Uri "https://discord.com/api/v10/users/@me" `
                                       -Headers $headers `
                                       -Method Get `
                                       -ErrorAction Stop
        
        $botUsername = $response.username
        $clientId = $response.id
        $isBot = $response.bot -eq $true
        
        if (-not $isBot) {
            Write-Warn "This token doesn't appear to be a bot token!"
        }
        
        Write-Success "Token valid! Bot: $botUsername (ID: $clientId)"
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        
        switch ($statusCode) {
            401 {
                Write-Fail "Invalid token! Discord returned 401 Unauthorized."
                Write-Host "   Please check your token and try again."
                exit 1
            }
            403 {
                Write-Fail "Token forbidden! Discord returned 403."
                Write-Host "   The token may be for a deleted bot."
                exit 1
            }
            429 {
                Write-Warn "Rate limited by Discord. Token might be valid."
            }
            default {
                Write-Warn "Could not validate token: $_"
                Write-Host "   Continuing anyway..."
            }
        }
    }
}

# Store token in user secrets
Write-Host ""
Write-Info "Storing token in user secrets..."

Push-Location $BotProjectPath
try {
    $result = dotnet user-secrets set "Discord:Token" $Token 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Token stored in user secrets"
    } else {
        Write-Fail "Failed to store token: $result"
        exit 1
    }
}
finally {
    Pop-Location
}

# Generate invite URL
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

if ($clientId) {
    $inviteUrl = "https://discord.com/oauth2/authorize?client_id=$clientId&permissions=$PermissionsInteger&scope=$Scopes"
    
    Write-Host "🔗 Bot Invite URL:" -ForegroundColor Green
    Write-Host ""
    Write-Host "   $inviteUrl" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Open this URL in your browser to add the bot to a server."
    
    # Try to copy to clipboard
    try {
        $inviteUrl | Set-Clipboard -ErrorAction Stop
        Write-Host "   (Copied to clipboard!)" -ForegroundColor DarkGray
    } catch {
        # Clipboard not available
    }
} else {
    Write-Info "To generate your invite URL, you need your Application/Client ID:"
    Write-Host ""
    Write-Host "   1. Go to https://discord.com/developers/applications"
    Write-Host "   2. Select your application"
    Write-Host "   3. Copy the 'Application ID' from General Information"
    Write-Host "   4. Replace CLIENT_ID in this URL:"
    Write-Host ""
    Write-Host "   https://discord.com/oauth2/authorize?client_id=CLIENT_ID&permissions=$PermissionsInteger&scope=$Scopes" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

# Permissions breakdown
Write-Host "📋 Bot Permissions (integer: $PermissionsInteger):" -ForegroundColor Yellow
Write-Host "   • View Channels (0x0400 = 1024)"
Write-Host "   • Read Message History (0x10000 = 65536)"
Write-Host ""

# Reminder about intents
Write-Host "⚠️  Don't forget to enable Privileged Gateway Intents!" -ForegroundColor Yellow
Write-Host "   In Discord Developer Portal → Bot → Privileged Gateway Intents:"
Write-Host "   • SERVER MEMBERS INTENT"
Write-Host "   • MESSAGE CONTENT INTENT"
Write-Host ""

Write-Host "🚀 Setup complete! Run the application with:" -ForegroundColor Green
Write-Host ""
Write-Host "   cd src\DiscordDataMirror.AppHost" -ForegroundColor Cyan
Write-Host "   dotnet run" -ForegroundColor Cyan
Write-Host ""
