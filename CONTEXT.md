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
│   └── Pieces/AllyPawnData.asset, AllyQueenData.asset, AllyKnightData.asset,
│            AllyBishopData.asset, AllyRookData.asset, AllyKingData.asset,
│            EnemyPawnData.asset, EnemyQueenData.asset
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
- Fields: `pieceName`, `team` (Ally/Enemy), `attackType` (Projectile/Direct/DiagonalProjectile/Laser/Homing/Splash), `sprite`, `cost`, `maxHP`, `attackDamage`, `attackRange`, `attackCooldown`, `projectileSpeed`
- Special ability fields: `bonusMaxHpPercent`, `bonusDamageCapPercent` (Knight), `projectileCount`, `extraRange` (Bishop/Rook/Queen), `chargeDuration`, `maxChargeMultiplier` (Rook), `homingDuration` (Queen), `splashRadius`, `slowPercent`, `buffRange`, `buffAttackPercent` (King)
- Gacha field: `gachaWeight`

### Piece (`Pieces/Piece.cs`)
- Holds `PieceData` reference, applies sprite/HP at Start
- `Team → data.team`, `IsDead → CurrentHP <= 0`
- `AttackBuff` — multiplicative damage buff (stackable, used by King aura)
- `ResetAttackBuff()`, `AddAttackBuff(multiplier)`, `GetAttackDamage()` — returns `data.attackDamage * AttackBuff`
- `SetData(data)` — reconfigures runtime
- `TakeDamage(damage)`, `Die()` (Destroy)
- Ally pieces auto-register with `CombatManager`; unregister on `OnDestroy`

### PieceManager (`Pieces/PieceManager.cs`)
- References: `gridManager`, `piecePrefab`, `pullCost` (50)
- `Start()` — collects all `Team.Ally` PieceData assets with `gachaWeight > 0` into gacha pool
- `PullPiece()` — spends gold, weighted random selection (Knight/Bishop/Rook:30000, Queen/King:1000), places on empty cell
- `OnPiecePulled` event — notifies UI with the pulled PieceData

### Enemy (`Enemies/Enemy.cs`)
- Properties: `CurrentHP`, `IsDead`
- Static event `OnAnyEnemyRemoved` — triggers EnemyManager wave-end check
- `SetData(data)`, `SetWaypoints(List<Vector3>)`
- `Update()` — moves toward current waypoint at `moveSpeed` (2) with slow support; when distance < 0.05, advances to next waypoint
- `TakeDamage(damage)`, `ApplySlow(multiplier, duration)` — stackable slow
- `Die()` → AddGold(10) + fire event + SpawnDeathEffect + Destroy
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
- `Update()` — resets attack buffs, applies King auras, then iterates ally pieces (reverse) calling `TryAttack()`
- `TryAttack()` — routes to handler based on `AttackType`:
  - **Direct** (Knight): instant damage with bonus %maxHP damage
  - **Projectile** (Pawn): fires standard homing projectile
  - **DiagonalProjectile** (Bishop): fires 4 diagonal line projectiles
  - **Laser** (Rook): charges for duration, then deals AoE in 4 directions with multiplier
  - **Homing** (Queen): fires homing projectile with limited tracking time
  - **Splash** (King): AoE damage + slow to enemies, buffs adjacent allies

### Projectile (`Combat/Projectile.cs`)
- Three modes: `Standard` (track target), `Line` (fixed direction), `Homing` (track then line after timeout)
- `Initialize(target, damage, speed)` — standard homing
- `InitializeLine(dir, damage, speed, maxDist)` — line projectile (Bishop)
- `InitializeHoming(target, damage, speed, homingTime)` — homing with timeout (Queen)
- `Update()` — delegates to mode-specific update
- `SpawnHitEffect()` — loads `Resources/FX/HitEffect` and instantiates at impact position

### UIManager (`UI/UIManager.cs`)
- References TMP texts for gold, lives, wave, countdown, buy button, pull result; Game Over / Victory panels
- Subscribes to `GameManager.OnGoldChanged`, `OnLivesChanged`, `OnWaveChanged`, `OnStateChanged`
- Subscribes to `PieceManager.OnPiecePulled` — shows pulled piece name for 2s
- `Start()` — initializes text values, hides panels; calls `UpdateBuyButtonText()`
- `Update()` — shows countdown text only when `State == Ready`
- `OnStateChanged()` — shows GameOverPanel on `GameOver`, VictoryPanel on `Victory`
- `WireRestartButton()` — finds RestartBtn in GameOverPanel and wires onClick to `GameManager.Restart()`

---

## PieceData Assets

| Asset | Team | Cost | HP | ATK | Range | Cooldown | Speed | AttackType |
|---|---|---|---|---|---|---|---|---|
| AllyPawnData | Ally | 50 | 50 | 10 | 2 | 1.0 | 5 | Projectile |
| AllyKnightData | Ally | 50 | 40 | 5 | 2 | 0.67 | — | Direct |
| AllyBishopData | Ally | 50 | 30 | 3 | 3 | 0.56 | 8 | DiagonalProjectile |
| AllyRookData | Ally | 50 | 60 | 4 | 4 | 1.0 | — | Laser |
| AllyQueenData | Ally | 100 | 80 | 5 | 6 | 0.5 | 6 | Homing |
| AllyKingData | Ally | 200 | 100 | 4 | 4 | 0.67 | — | Splash |
| EnemyPawnData | Enemy | — | 30 | 5 | 1 | 1.0 | — | Projectile |
| EnemyQueenData | Enemy | — | 60 | 15 | 1.5 | 1.2 | — | Projectile |

---

## Scene Setup (Scene01.unity, 6 root GameObjects)

| GameObject | Components | Key References |
|---|---|---|
| **Main Camera** | Camera (Ortho, Size=5, Pos=3.5,3.5,-10) | — |
| **GridManager** | GridManager | WhiteTile.prefab, GreenTile.prefab |
| **PieceManager** | PieceManager | GridManager (ref), Piece.prefab, AllyPawnData |
| **Canvas** | Canvas, CanvasScaler, GraphicRaycaster, UIManager | GoldText, LivesText, WaveText, CountdownText, GameOverPanel, VictoryPanel |
| ├─ EventSystem | EventSystem | — |
| ├─ BuyPieceButton | Button (Text TMP child) | OnClick → PieceManager.PullPiece, btn text="Pull (50G)" |
| ├─ GoldText | TextMeshProUGUI | "Gold: 100" |
| ├─ LivesText | TextMeshProUGUI | "Lives: 20" |
| ├─ WaveText | TextMeshProUGUI | "Wave: 1" |
| ├─ CountdownText | TextMeshProUGUI | "Wave N starts soon..." (hidden during wave) |
| ├─ PullResultText | TextMeshProUGUI | Shows pulled piece name (hidden by default) |
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
- 적군 기물 다양화 (EnemyManager에 Knight/Bishop/Rook/King 대응)
