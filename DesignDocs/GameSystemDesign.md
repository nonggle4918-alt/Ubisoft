# 🏰 타워 디펜스 게임 시스템 설계 (Ubisoft)

## 1. 개요

2D 탑다운 뷰의 타워 디펜스 게임. 체스말을 모티브로 한 아군/적군 기물이 8x8 보드 위에서 대치.

---

## 2. 게임 흐름 (사용자 시점)

```
게임 시작 → 초기 골드 지급 → 아군 기물 배치 (Buy) 
    → 적 웨이브 스폰 → 적이 보드 외곽 동선 따라 이동
        → 아군 기물이 투사체 발사 → 적 처치 (골드 획득)
            → 웨이브 종료 → 다음 웨이브 (라운드 증가)
                → 게임 오버 (체력 0) 또는 승리
```

---

## 3. 시스템별 상세 설계

### 3.1. Core (GameManager)

**파일:** `Assets/Scripts/Core/GameManager.cs`

**역할:** 게임 전반 상태 관리 (싱글톤)

| 요소 | 설명 |
|---|---|
| **골드** | 초기값: 100. 적 처치 시 골드 획득 (+10~50). 구매 시 차감. |
| **생명 (Lives)** | 초기값: 20. 적이 보드 끝까지 도달하면 차감. 0이면 게임 오버. |
| **웨이브** | 현재 웨이브 번호 (1부터 시작). 웨이브마다 적 구성/수 증가. |
| **게임 상태** | `Ready`(배치) / `WaveInProgress` / `GameOver` / `Victory` |

**UI 표시:** 화면 상단 HUD
```
┌──────────────────────────────────────┐
│  ❤️ 20    💰 100    Wave 1/10    [Start] │
└──────────────────────────────────────┘
```

---

### 3.2. Grid (그리드)

**파일:** `Assets/Scripts/Grid/GridManager.cs` (기존)
**파일:** `Assets/Scripts/Grid/GridCell.cs` (기존)

**현재 구현:** 8x8 격자, 타일 생성, 빈 칸 찾기 ✅

**추가 필요 사항:**

| 요소 | 설명 |
|---|---|
| **타일 강조** | 배치 가능한 타일 시각적 표시 (초록색 테두리 등) |
| **타일 hover 효과** | 마우스 오버 시 타일 하이라이트 |
| **타일 클릭** | 기물 정보 표시 (향후) |

**사용자에게 보이는 모습:**
```
    0   1   2   3   4   5   6   7
  ┌───┬───┬───┬───┬───┬───┬───┬───┐
0 │ ■ │ □ │ ■ │ □ │ ■ │ □ │ ■ │ □ │  ← 체커보드 패턴 타일
  ├───┼───┼───┼───┼───┼───┼───┼───┤
1 │ □ │ ■ │ □ │ ■ │ □ │ ■ │ □ │ ■ │
  ├───┼───┼───┼───┼───┼───┼───┼───┤
2 │ ■ │ □ │ ♙ │ □ │ ■ │ □ │ ■ │ □ │  ← ♙ = 아군 기물 배치됨
  ├───┼───┼───┼───┼───┼───┼───┼───┤
... (8x8)
```

---

### 3.3. Piece (기물 시스템)

**파일:** `Assets/Scripts/Pieces/Piece.cs` (기존 - 확장 필요)
**파일:** `Assets/Scripts/Pieces/PieceManager.cs` (기존 - 확장 필요)
**파일:** `Assets/Scripts/Pieces/PieceData.cs` (신규 - ScriptableObject)

#### 3.3.1. 기물 데이터 (PieceData SO)

```
PieceData (ScriptableObject)
├── pieceName: string ("Pawn", "Queen" 등)
├── team: enum { Ally, Enemy }
├── sprite: Sprite (아군=흰색, 적군=검정색)
├── cost: int (구매 가격, 아군만 해당)
├── hp: int (체력)
├── attackDamage: float (공격력)
├── attackRange: float (사정거리)
├── attackCooldown: float (공격 쿨타임, 초)
└── projectileSpeed: float (투사체 속도)
```

#### 3.3.2. Piece 컴포넌트

```
Piece
├── data: PieceData (참조)
├── currentHP: int
├── isPlaced: bool
└── 목표 Enemy 참조
```

#### 3.3.3. 사용자에게 보이는 모습

**구매 전:**
```
[Buy - 50G] ← 화면 하단 버튼
  → 클릭 시: 골드 차감 → 가장 가까운 빈 타일에 Pawn 자동 배치
```

**배치 후 보드:**
```
┌─────────────────────┐
│  (0,0)  (1,0)  ... │
│  ┌─────┐           │
│  │ ♙   │  ← 흰색 Pawn  │
│  │ HP   │           │
│  └─────┘           │
│  (0,1)             │
└─────────────────────┘
```

**기물 클릭 시 정보창:**
```
┌─ Piece Info ──────┐
│  ♙ Pawn           │
│  HP: 50/50        │
│  ATK: 10          │
│  Range: 2.0       │
│  Team: Ally       │
└───────────────────┘
```

---

### 3.4. Enemy (적 시스템) - 신규

**파일:** `Assets/Scripts/Enemies/Enemy.cs`
**파일:** `Assets/Scripts/Enemies/EnemyManager.cs`

#### 3.4.1. Enemy 컴포넌트

```
Enemy
├── data: PieceData (적군용 데이터)
├── currentHP: int
├── moveSpeed: float
├── pathIndex: int (동선 경로 인덱스)
└── agent (네비게이션 또는 단순 Waypoint 이동)
```

#### 3.4.2. EnemyManager (웨이브 관리)

| 요소 | 설명 |
|---|---|
| **웨이브 간격** | 30초 (웨이브 종료 후 카운트다운) |
| **웨이브 구성** | Wave 1: Pawn x3 → Wave 5: Pawn x5 + Queen x2 → 점진적 난이도 상승 |
| **스폰 위치** | 보드 좌상단 외곽 (0, -1) 또는 동선 시작점 |
| **스폰 간격** | 각 적 1~2초 간격 생성 |

#### 3.4.3. 적 이동 동선

적은 보드 외곽을 **시계방향**으로 사각형 이동

```
  Start→ [0,-1]→[1,-1]→ ... →[7,-1]  (상단 외곽)
                                       ↓
  (-1,0)←(-1,1)← ... ←(-1,7)   (좌측 외곽) [8,7]
   ↑                                     │
  [0,8]→[1,8]→ ... →[7,8]       (하단 외곽)
   ↓
  [8,7]→[8,6]→ ... →[8,0]       (우측 외곽)
   ↓
  [7,-1] 방향으로 계속 (루프)
```

적이 지정된 생명점(예: 다시 시작점 도달 시)에 도달하면 플레이어 생명 차감 후 제거.

#### 3.4.4. 사용자에게 보이는 모습

```
Wave 1 시작! → 좌상단에서 검정 Pawn 3마리 등장
  → 보드 외곽을 따라 시계방향 이동
  → 아군 기물 사정거리 내 진입 시 아군이 투사체 발사 시작
```

---

### 3.5. Combat (전투 시스템) - 신규

**파일:** `Assets/Scripts/Combat/CombatManager.cs`
**파일:** `Assets/Scripts/Combat/Projectile.cs`

#### 3.5.1. CombatManager

| 요소 | 설명 |
|---|---|
| **탐색** | Piece가 자신의 사정거리 내 Enemy 탐색 (매 프레임 or 코루틴) |
| **목표 선택** | 가장 가까운 적 우선 |
| **공격** | 쿨타임마다 Projectile 생성 후 발사 |

#### 3.5.2. Projectile

```
Projectile
├── speed: float
├── damage: float
├── target: Enemy
└── 발사 위치 → 목표 방향으로 이동, 충돌 시 데미지 + 파괴
```

#### 3.5.3. 사용자에게 보이는 모습

```
  ♙ (아군)  ────●────→  ♟ (적군)
              투사체    HP: 50 → 40 (데미지 -10)
```

---

### 3.6. UI 시스템 - 신규

**파일:** `Assets/Scripts/UI/UIManager.cs`
**파일:** `Assets/Scripts/UI/BuyPanel.cs`
**파일:** `Assets/Scripts/UI/HUD.cs`

| 요소 | 위치 | 설명 |
|---|---|---|
| **HUD** | 상단 | 골드, 생명, 웨이브, 라운드 시작 버튼 |
| **BuyPanel** | 하단 | Buy 버튼 + 구매할 기물 선택 (향후) |
| **기물 정보창** | 화면 일부 | 클릭한 기물 스탯 표시 (향후) |

초기 단계: 현재 Buy 버튼 하나로 Pawn만 구매.

#### 사용자에게 보이는 모습 (초안)

```
┌──────────────────────────────────────────┐
│  ❤️ Lives: 20    💰 Gold: 100     Wave 1 │ [Start Wave]
├──────────────────────────────────────────┤
│                                          │
│     ■  □  ■  □  ■  □  ■  □              │
│     □  ■  □  ■  □  ■  □  ■              │
│     ■  □  ♙  □  ■  □  ■  □    ← 8x8 보드 │
│     □  ■  □  ■  □  ■  □  ■              │
│     ...                                   │
│                                          │
├──────────────────────────────────────────┤
│           [ Buy Pawn - 50G ]            │ ← 하단 버튼
└──────────────────────────────────────────┘
```

---

## 4. 구현 우선순위 (Phase)

### Phase 1 - Core & Piece (현재)
- [x] Grid 8x8 생성
- [x] PieceManager 기본 (Buy → Spawn)
- [ ] GameManager (골드/생명/웨이브 상태)
- [ ] PieceData ScriptableObject (팀/스탯)
- [ ] Piece 확장 (HP 적용, 기물 타입별 구매)

### Phase 2 - Enemy & Combat
- [ ] Enemy 스크립트
- [ ] EnemyManager (웨이브, 스폰)
- [ ] 적 이동 동선 (Waypoint)
- [ ] 기본 전투 (사정거리 내 목표 탐색, 투사체)

### Phase 3 - UI & Polish
- [ ] HUD (골드/생명/웨이브 표시)
- [ ] 웨이브 시스템 연동
- [ ] 기물 정보창
- [ ] 게임오버/승리 조건

---

## 5. 파일 구조 (최종)

```
Assets/Scripts/
├── Core/
│   └── GameManager.cs
├── Grid/
│   ├── GridManager.cs
│   └── GridCell.cs
├── Pieces/
│   ├── PieceManager.cs
│   ├── Piece.cs
│   └── PieceData.cs (ScriptableObject)
├── Enemies/
│   ├── EnemyManager.cs
│   └── Enemy.cs
├── Combat/
│   ├── CombatManager.cs
│   └── Projectile.cs
└── UI/
    ├── UIManager.cs
    ├── HUD.cs
    └── BuyPanel.cs
```

---

> **참고:** 모든 수치(골드, 체력, 공격력, 웨이브 구성 등)는 밸런스 조정이 필요하며, 구현 후 테스트를 통해 튜닝 예정.
