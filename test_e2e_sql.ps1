# Splendor E2E Test - SQL Server Read Models
$baseUrl = "http://localhost:5200"
$ErrorActionPreference = "Stop"

Write-Host "`n=== Splendor E2E Test ===" -ForegroundColor Magenta
Write-Host "Testing: Marten -> SQL Server`n" -ForegroundColor Cyan

# 1. Create Game
Write-Host "[1/6] Creating Game..."
try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/games" -Method Post -Body "{}" -ContentType "application/json"
    $gameId = $createResponse.id
    Write-Host "  [OK] Game Created: $gameId" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Failed to create game: $_" -ForegroundColor Red
    exit 1
}

# 2. Join Alice
Write-Host "[2/6] Joining Alice..."
$p1Id = [Guid]::NewGuid()
$joinP1Body = @{ GameId = $gameId; PlayerId = $p1Id; Name = "Alice" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$baseUrl/games/$gameId/players" -Method Post -Body $joinP1Body -ContentType "application/json"
    Write-Host "  [OK] Alice joined" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Failed to join Alice" -ForegroundColor Red
    exit 1
}

# 3. Join Bob
Write-Host "[3/6] Joining Bob..."
$p2Id = [Guid]::NewGuid()
$joinP2Body = @{ GameId = $gameId; PlayerId = $p2Id; Name = "Bob" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$baseUrl/games/$gameId/players" -Method Post -Body $joinP2Body -ContentType "application/json"
    Write-Host "  [OK] Bob joined" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Failed to join Bob" -ForegroundColor Red
    exit 1
}

# 4. Start Game
Write-Host "[4/6] Starting Game..."
try {
    Invoke-RestMethod -Uri "$baseUrl/games/$gameId/start" -Method Post -ContentType "application/json"
    Write-Host "  [OK] Game Started" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Failed to start game" -ForegroundColor Red
    exit 1
}

# 5. Verify
Write-Host "[5/6] Verifying Read Model..."
try {
    $gameState = Invoke-RestMethod -Uri "$baseUrl/games/$gameId" -Method Get
    Write-Host "  [OK] Status: $($gameState.status)" -ForegroundColor Green
    Write-Host "  [OK] Players: $($gameState.players.Count)" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Failed to verify" -ForegroundColor Red
    exit 1
}

# 6. Take Gems
Write-Host "[6/6] Alice takes gems..."
$takeGemsBody = @{
    GameId = $gameId; PlayerId = $p1Id;
    Diamond = 1; Sapphire = 1; Ruby = 1;
    Emerald = 0; Onyx = 0; Gold = 0
} | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$baseUrl/games/$gameId/actions/take-gems" -Method Post -Body $takeGemsBody -ContentType "application/json"
    Write-Host "  [OK] Gems taken" -ForegroundColor Green
    
    $upd = Invoke-RestMethod -Uri "$baseUrl/games/$gameId" -Method Get
    $alice = $upd.players | Where-Object { $_.id -eq $p1Id }
    Write-Host "  [OK] Alice gems: D=$($alice.gems.diamond) S=$($alice.gems.sapphire) R=$($alice.gems.ruby)" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Failed to take gems" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== [SUCCESS] All Tests Passed ===" -ForegroundColor Green
