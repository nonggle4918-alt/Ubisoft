# Ubisoft Tower Defense — Project Context

## Overview
2D top-down tower defense game built in Unity 6.3 (6000.3.19f1) with URP, 2D mode, orthographic camera (Size: 5, Position: (3.5, 3.5, -10)). The player places ally pieces on an 8×8 grid, enemies walk a clockwise perimeter path, and ally pieces auto-attack via projectile.

---

## Phase Status

### ✅ Phase 1 (Core & Pieces) — Done
### ✅ Phase 2 (Enemy & Combat) — Done
### ✅ Phase 3 (UI) — Done
### 🔲 Phase 4 (Polish) — Not started

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/GameManager.cs
│   ├── Grid/GridManager.cs, GridCell.cs
│   ├── Pieces/Piece.cs, PieceData.cs, PieceManager.cs
│   ├── Enemies/Enemy.cs, EnemyManager.cs
│   ├── Combat/CombatManager.cs, Projectile.cs
│   ├── UI/UIManager.cs
│   └── Pieces/AllyPawnData.asset, AllyQueenData.asset, EnemyPawnData.asset, EnemyQueenData.asset
├── Prefabs/
│   ├── Tile/WhiteTile.prefab, GreenTile.prefab
│   ├── Piece.prefab
│   ├── Enemy.prefab
│   └── Projectile.prefab
├── Sprites/
│   ├── Board/spr_tileWhite.png (128×128, PPU=128), spr_tileGreen.png (128×128, PPU=128)
│   ├── wht_Pawn.png, wht_Queen.png (ally piece sprites)
│   ├── blk_Pawn.png, blk_Queen.png (enemy piece sprites)
│   └── Board/Title.png
├── Resources/
│   └── FX/HitEffect.prefab, DeathEffect.prefab
└── Scenes/Scene01.unity
```

---

## Script Architecture

### GameManager (`Core/GameManager.cs`)
- Singleton (`Instance`)
- State machine: `Ready → WaveInProgress → GameOver/Victory`
- Properties: `Gold` (start 100), `Lives` (start 20), `CurrentWave` (start 1)
- Events: `OnGoldChanged`, `OnLivesChanged`, `OnWaveChanged`, `OnStateChanged`
- Methods: `SpendGold()`, `AddGold()`, `LoseLife()`, `StartWave()`, `EndWave()`, `Restart()`
- Auto-starts first wave after 10s (`firstWaveDelay`), subsequent waves 5s (`betweenWaveDelay`) after previous ends
- GameOver when `Lives <= 0`

### GridManager (`Grid/GridManager.cs`)
- Generates 8×8 chessboard-pattern grid from `WhiteTile.prefab` / `GreenTile.prefab`
- Tiles placed at integer positions (0,0)–(7,7), each exactly 1.0×1.0 world units
- `GetCell(x, y)` with bounds check
- `GetEmptyCell()` — scans grid row-major for first `IsEmpty` cell

### GridCell (`Grid/GridCell.cs`)
- Properties: `X`, `Y`, `CurrentPiece`, `IsEmpty`
- Methods: `Initialize(x, y)`, `SetPiece(piece)`, `RemovePiece()`

### PieceData (`Pieces/PieceData.cs`)
- ScriptableObject, `[CreateAssetMenu(menuName = "Ubisoft/PieceData")]`
- Fields: `pieceName`, `team` (Ally/Enemy), `sprite`, `cost`, `maxHP`, `attackDamage`, `attackRange`, `attackCooldown`, `projectileSpeed`

### Piece (`Pieces/Piece.cs`)
- Holds `PieceData` reference, applies sprite/HP at Start
- `Team → data.team`, `IsDead → CurrentHP <= 0`
- `SetData(data)` — reconfigures runtime
- `TakeDamage(damage)`, `Die()` (Destroy)
- Ally pieces auto-register with `CombatManager`; unregister on `OnDestroy`

### PieceManager (`Pieces/PieceManager.cs`)
- References: `gridManager`, `piecePrefab`, `allyPieceData`
- `SpawnPiece()` — checks gold, finds empty cell, instantiates piece, deducts gold; refunds if board full
- `GetCurrentPieceData()` — returns current `allyPieceData` (used by UIManager for button text)

### Enemy (`Enemies/Enemy.cs`)
- Properties: `CurrentHP`, `IsDead`
- Static event `OnAnyEnemyRemoved` — triggers EnemyManager wave-end check
- `SetData(data)`, `SetWaypoints(List<Vector3>)`
- `Update()` — moves toward current waypoint at `moveSpeed` (2); when distance < 0.05, advances to next waypoint
- `TakeDamage(damage)`, `Die()` → AddGold(10) + fire event + SpawnDeathEffect + Destroy
- `ReachEnd()` → LoseLife(1) + fire event + Destroy

### EnemyManager (`Enemies/EnemyManager.cs`)
- References: `enemyPrefab`, `enemyPawnData`, `enemyQueenData`, `spawnInterval` (1.5s)
- **Waypoints** (36 points, clockwise perimeter): start at (-1,-1) → right along y=-1 to (8,-1) → up x=8 to (8,7) → left y=8 to (-1,8) → down x=-1 to (-1,0) → back to (-1,-1)
- Listens to `GameManager.OnStateChanged` → starts `SpawnWave()` coroutine when `WaveInProgress`
- Wave composition: Wave N → `Pawn × (2+N)` + `Queen × max(0, N-2)`
- Tracks `enemiesAlive`; calls `EndWave()` when all dead

### CombatManager (`Combat/CombatManager.cs`)
- References: `projectilePrefab`
- Maintains `List<Piece> allyPieces` + `Dictionary<Piece, float> lastAttackTime` (cooldown tracking)
- `RegisterPiece(piece)`, `UnregisterPiece(piece)`
- `Update()` — iterates ally pieces (reverse), removes dead/null, calls `TryAttack()`
- `TryAttack()` — checks cooldown, finds nearest enemy in range via `FindObjectsByType<Enemy>`, fires projectile
- `FireProjectile()` — instantiates `projectilePrefab`, calls `projectile.Initialize(target, damage, speed)`

### Projectile (`Combat/Projectile.cs`)
- Properties: `target`, `damage`, `speed`
- `Update()` — moves toward target at `speed`; destroys if target null; deals damage on proximity (< 0.2)
- `SpawnHitEffect()` — loads `Resources/FX/HitEffect` and instantiates at impact position

### UIManager (`UI/UIManager.cs`)
- References TMP texts for gold, lives, wave, countdown; Game Over / Victory panels
- Subscribes to `GameManager.OnGoldChanged`, `OnLivesChanged`, `OnWaveChanged`, `OnStateChanged`
- `Start()` — initializes text values, hides panels; calls `UpdateBuyButtonText()`
- `Update()` — shows countdown text only when `State == Ready`
- `OnStateChanged()` — shows GameOverPanel on `GameOver`, VictoryPanel on `Victory`
- `WireRestartButton()` — finds RestartBtn in GameOverPanel and wires onClick to `GameManager.Restart()`

---

## PieceData Assets

| Asset | Team | Cost | HP | ATK | Range | Cooldown | Speed |
|---|---|---|---|---|---|---|---|
| AllyPawnData | Ally | 50 | 50 | 10 | 2 | 1.0 | 5 |
| AllyQueenData | Ally | 100 | 80 | 25 | 3 | 1.5 | 6 |
| EnemyPawnData | Enemy | — | 30 | 5 | 1 | 1.0 | — |
| EnemyQueenData | Enemy | — | 60 | 15 | 1.5 | 1.2 | — |

---

## Scene Setup (Scene01.unity, 6 root GameObjects)

| GameObject | Components | Key References |
|---|---|---|
| **Main Camera** | Camera (Ortho, Size=5, Pos=3.5,3.5,-10) | — |
| **GridManager** | GridManager | WhiteTile.prefab, GreenTile.prefab |
| **PieceManager** | PieceManager | GridManager (ref), Piece.prefab, AllyPawnData |
| **Canvas** | Canvas, CanvasScaler, GraphicRaycaster, UIManager | GoldText, LivesText, WaveText, CountdownText, GameOverPanel, VictoryPanel |
| ├─ EventSystem | EventSystem | — |
| ├─ BuyPieceButton | Button (Text TMP child) | OnClick → PieceManager.SpawnPiece, btn text="Buy Pawn (50G)" |
| ├─ GoldText | TextMeshProUGUI | "Gold: 100" |
| ├─ LivesText | TextMeshProUGUI | "Lives: 20" |
| ├─ WaveText | TextMeshProUGUI | "Wave: 1" |
| ├─ CountdownText | TextMeshProUGUI | "Wave N starts soon..." (hidden during wave) |
| ├─ GameOverPanel | Panel (Image) + Title + RestartBtn | Hidden by default |
| └─ VictoryPanel | Panel (Image) + Title | Hidden by default |
| **EnemyManager** | EnemyManager | Enemy.prefab, EnemyPawnData, EnemyQueenData |
| **CombatManager** | CombatManager | Projectile.prefab |

---

## Known Issues & Fixes Applied

1. **GridCell missing on tile prefabs** → Added GridCell component to both WhiteTile/GreenTile prefabs via script-execute
2. **Tile sprite PPU mismatch** → Changed PPU from 100→128 on both tile sprites so tiles are exactly 1.0×1.0 world units
3. **Restart button** → GameManager.Restart() resets gold/lives/wave, clears enemies/pieces/grid; UIManager.WireRestartButton() wires RestartBtn onClick
4. **Visual effects** → HitEffect (yellow burst, 0.3s) and DeathEffect (orange burst, 0.5s) via ParticleSystem, loaded from Resources/FX/

## Next Steps (Phase 4 — Polish)
- Sound effects / music
- Balancing & tuning
