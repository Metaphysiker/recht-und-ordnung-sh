#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

$Server = "deploy@84.234.19.192"
$RemotePath = "/home/deploy/recht-und-ordnung-sh"

Write-Host "=== Pre-Deployment Checks ===" -ForegroundColor Cyan

# Check for uncommitted changes
$gitStatus = git diff-index --quiet HEAD -- 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: You have uncommitted changes:" -ForegroundColor Yellow
    git status --short
    $reply = Read-Host "Continue anyway? (y/N)"
    if ($reply -notmatch '^[Yy]$') {
        Write-Host "Deployment cancelled." -ForegroundColor Red
        exit 0
    }
}

# Get current deployment info
$currentBranch  = git rev-parse --abbrev-ref HEAD
$commitHash     = git rev-parse HEAD
$commitShort    = git rev-parse --short HEAD
$commitMessage  = git log -1 --pretty=%B

Write-Host ""
Write-Host "Current branch: $currentBranch"
Write-Host "Commit: $commitShort - $commitMessage"

# Check what's currently deployed
Write-Host ""
Write-Host "Checking current deployment on server..." -ForegroundColor Cyan
$currentDeployment = ssh $Server "cat $RemotePath/deployment.txt 2>/dev/null || echo 'No deployment info found'"
if ($currentDeployment -ne 'No deployment info found') {
    Write-Host "Currently deployed:"
    Write-Host $currentDeployment
} else {
    Write-Host "No previous deployment info found on server"
}

# Create deployment tracking
$timestamp       = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$deploymentBranch = "deployment/$timestamp"
$deploymentTag    = "deploy-$timestamp"

Write-Host ""
Write-Host "Creating deployment tracking:" -ForegroundColor Cyan
Write-Host "  Branch: $deploymentBranch"
Write-Host "  Tag:    $deploymentTag"

git branch $deploymentBranch
git tag -a $deploymentTag -m "Deployment at $timestamp from $currentBranch ($commitShort)"

# Create deployment info file
$deployedBy   = $env:USERNAME
$deployedFrom = $env:COMPUTERNAME
@"
Deployment Date: $timestamp
Branch: $currentBranch
Commit: $commitHash
Commit Short: $commitShort
Message: $commitMessage
Deployed By: $deployedBy
Deployed From: $deployedFrom
"@ | Set-Content -Encoding UTF8 deployment.txt

Write-Host ""
Write-Host "Deployment info created"

# Build
Write-Host ""
Write-Host "Building Docker images..." -ForegroundColor Cyan
docker compose --file docker-compose.build.yml build
if ($LASTEXITCODE -ne 0) { throw "Docker build failed" }

# Transfer webapi image
Write-Host ""
Write-Host "Transferring webapi image..." -ForegroundColor Cyan
docker save recht-und-ordnung-sh-production-webapi -o webapi-image.tar
if ($LASTEXITCODE -ne 0) { throw "docker save webapi failed" }
scp webapi-image.tar "${Server}:/tmp/"
ssh $Server "docker load -i /tmp/webapi-image.tar && rm /tmp/webapi-image.tar"
Remove-Item webapi-image.tar

# Transfer blazor image
Write-Host ""
Write-Host "Transferring blazor image..." -ForegroundColor Cyan
docker save recht-und-ordnung-sh-production-blazor -o blazor-image.tar
if ($LASTEXITCODE -ne 0) { throw "docker save blazor failed" }
scp blazor-image.tar "${Server}:/tmp/"
ssh $Server "docker load -i /tmp/blazor-image.tar && rm /tmp/blazor-image.tar"
Remove-Item blazor-image.tar

# Copy config files
Write-Host ""
Write-Host "Copying config files..." -ForegroundColor Cyan
scp docker-compose.remote.yml "${Server}:${RemotePath}"
scp .env                       "${Server}:${RemotePath}"
scp appsettings.Production.json "${Server}:${RemotePath}"
scp deployment.txt             "${Server}:${RemotePath}"
scp nginx-frontend.conf        "${Server}:${RemotePath}"
scp nginx-api.conf             "${Server}:${RemotePath}"
scp nginx-setup.sh             "${Server}:${RemotePath}"

# Restart containers
Write-Host ""
Write-Host "Restarting containers on server..." -ForegroundColor Cyan
ssh $Server "cd $RemotePath && docker compose --file docker-compose.remote.yml down && docker compose --file docker-compose.remote.yml up -d && docker system prune -f"

Write-Host ""
Write-Host "=== Deployment completed successfully! ===" -ForegroundColor Green
Write-Host "Deployment tracked in:"
Write-Host "  Branch: $deploymentBranch"
Write-Host "  Tag:    $deploymentTag"
Write-Host "  Commit: $commitShort"

# Cleanup
Write-Host ""
Write-Host "Cleaning up..." -ForegroundColor Cyan
Remove-Item deployment.txt -ErrorAction SilentlyContinue
git checkout $currentBranch

Write-Host ""
Write-Host "To check what's deployed on server, run:"
Write-Host "  ssh $Server cat $RemotePath/deployment.txt"
Write-Host ""
Write-Host "To view deployment history:"
Write-Host "  git branch --list 'deployment/*'"
Write-Host "  git tag --list 'deploy-*'"
