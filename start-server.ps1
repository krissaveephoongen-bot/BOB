# Windows: starts the local Nakama 3.22.0 + Postgres 16 (Docker Desktop must be installed and running).
# Run from PowerShell:  powershell -ExecutionPolicy Bypass -File .\start-server.ps1
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot 'server')
docker info *> $null
if ($LASTEXITCODE -ne 0) { throw 'Docker is not running. Start Docker Desktop, wait until it says "Engine running", then run this again.' }
if (Get-Command node -ErrorAction SilentlyContinue) { node build-module.js } else { Write-Host 'node not found: using the modules/index.js already in the folder' }
docker compose up -d
for ($i = 0; $i -lt 40; $i++) {
  try { if ((Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 http://127.0.0.1:7350/healthcheck).StatusCode -eq 200) { break } } catch {}
  Start-Sleep -Milliseconds 1500
}
Write-Host 'Nakama API http://127.0.0.1:7350  console http://127.0.0.1:7351 (admin / password, development only)'
