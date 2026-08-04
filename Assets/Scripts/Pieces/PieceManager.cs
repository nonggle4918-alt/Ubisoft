using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PieceManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Piece piecePrefab;
    [SerializeField] private int pullCost = 50;
    [SerializeField] private int pullCostPerBoss = 25;
    [SerializeField] private List<PieceData> allyPiecePool = new List<PieceData>();

    public event Action<PieceData> OnPiecePulled;

    private static readonly HashSet<string> SpecialPieceNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pawn", "queen", "king" };
    private static readonly Color SpecialPieceEffectColor = new Color(1f, 0.85f, 0.3f);

    private List<PieceData> gachaPool = new List<PieceData>();
    private int totalWeight;
    private int[] bossStages;

    // Every boss left behind makes the next piece more expensive.
    public int CurrentPullCost => pullCost + pullCostPerBoss * ClearedBossCount();

    // Sum of gachaWeight across the gacha pool — used by the merchant to price a specific
    // piece relative to how rare pulling it actually is.
    public int TotalGachaWeight => totalWeight;

    private int ClearedBossCount()
    {
        if (bossStages == null || GameManager.Instance == null) return 0;

        int wave = GameManager.Instance.CurrentWave;
        int count = 0;
        foreach (int stage in bossStages)
        {
            if (stage < wave) count++;
        }
        return count;
    }

    // Derived from the database rather than hard-coded so it tracks stage.csv/spawn.csv.
    private void CacheBossStages()
    {
        GameDatabase database = GameManager.Instance?.Database;
        if (database == null) { bossStages = new int[0]; return; }

        var stages = new List<int>();
        foreach (StageRecord stage in database.Stages.rows)
        {
            foreach (SpawnRecord spawn in database.GetSpawns(stage.spawnGroupId))
            {
                EnemyRecord enemy = database.GetEnemy(spawn.enemyId);
                if (enemy != null && enemy.IsBoss)
                {
                    stages.Add(stage.stageNumber);
                    break;
                }
            }
        }
        bossStages = stages.ToArray();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            PullPiece();
    }

    private void Start()
    {
        ApplyCharacterDatabase();
        CacheBossStages();

        foreach (var pd in allyPiecePool)
        {
            if (pd != null && pd.team == Team.Ally && pd.gachaWeight > 0)
            {
                gachaPool.Add(pd);
                totalWeight += pd.gachaWeight;
            }
        }
    }

    public void PullPiece()
    {
        if (GameManager.Instance == null) return;

        // Snapshot the price so a refund cannot differ from what was charged.
        int cost = CurrentPullCost;

        if (!GameManager.Instance.SpendGold(cost))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }

        PieceData selected = WeightedRandom();
        if (selected == null)
        {
            GameManager.Instance.RefundGold(cost);
            return;
        }

        int tier = GameManager.Instance.Database.RollTier();
        PieceData runtimeData = BuildRuntimeData(selected, tier);

        if (!TryPlaceRuntimePiece(runtimeData, out string failReason))
        {
            Debug.Log(failReason);
            GameManager.Instance.RefundGold(cost);
        }
    }

    // Copies a template piece's stats into a fresh runtime instance and applies the given
    // tier's stat overrides. Shared by the gacha pull above and merchant purchases.
    public PieceData BuildRuntimeData(PieceData source, int tier)
    {
        PieceData runtimeData = ScriptableObject.CreateInstance<PieceData>();
        runtimeData.pieceName = source.pieceName;
        runtimeData.team = source.team;
        runtimeData.attackType = source.attackType;
        runtimeData.maxHP = source.maxHP;
        runtimeData.attackDamage = source.attackDamage;
        runtimeData.attackRange = source.attackRange;
        runtimeData.attackCooldown = source.attackCooldown;
        runtimeData.cost = source.cost;
        runtimeData.projectileSpeed = source.projectileSpeed;
        runtimeData.visualScale = source.visualScale;
        runtimeData.bonusMaxHpPercent = source.bonusMaxHpPercent;
        runtimeData.bonusDamageCapPercent = source.bonusDamageCapPercent;
        runtimeData.extraRange = source.extraRange;
        runtimeData.chargeDuration = source.chargeDuration;
        runtimeData.maxChargeMultiplier = source.maxChargeMultiplier;
        runtimeData.homingDuration = source.homingDuration;
        runtimeData.splashRadius = source.splashRadius;
        runtimeData.slowPercent = source.slowPercent;
        runtimeData.buffRange = source.buffRange;
        runtimeData.buffAttackPercent = source.buffAttackPercent;
        runtimeData.movementSpeed = source.movementSpeed;
        runtimeData.goldReward = source.goldReward;
        runtimeData.projectileCount = source.projectileCount;
        runtimeData.sprite = source.sprite;
        runtimeData.isSpecialPiece = source.isSpecialPiece;
        runtimeData.coolUpgradeEfficiency = source.coolUpgradeEfficiency;

        runtimeData.tier = tier;
        ApplyTierToData(runtimeData, tier);
        return runtimeData;
    }

    // Places an already-built PieceData onto the first empty grid cell. Shared by the
    // gacha pull above and merchant purchases; the caller owns refunding gold on failure.
    public bool TryPlaceRuntimePiece(PieceData data, out string failReason)
    {
        GridCell cell = gridManager.GetEmptyCell();
        if (cell == null)
        {
            failReason = "빈 칸이 없습니다.";
            return false;
        }

        Piece piece = Instantiate(piecePrefab, cell.transform.position, Quaternion.identity);
        piece.SetData(data);
        piece.CurrentCell = cell;
        cell.SetPiece(piece);

        SFXManager.Instance?.PlayUnitPurchased();
        SFXManager.Instance?.PlayTierReward(data.tier);
        TrySpawnRarityEffect(data, piece.transform.position);
        OnPiecePulled?.Invoke(data);

        failReason = null;
        return true;
    }

    // Lightning strike marking a piece's arrival. Fires for every pull so the feedback is
    // always there, with the bolt tinted by what was pulled: gold for special pieces
    // (pawn/queen/king) and the tier color for bishop/knight/rook. Hero pieces get their own
    // strike from Piece's promotion routine instead.
    private static void TrySpawnRarityEffect(PieceData data, Vector3 position)
    {
        if (data == null) return;

        bool isSpecial = SpecialPieceNames.Contains(data.pieceName);
        Color color = isSpecial ? SpecialPieceEffectColor : PieceData.TierColor(data.tier);
        LightningStrikeEffect.Spawn(position, color);
    }

    private void ApplyTierToData(PieceData data, int tier)
    {
        int pieceId = GetPieceIdByName(data.pieceName);
        if (pieceId <= 0) return;

        TierStatRecord stat = GameManager.Instance.Database.GetTierStat(pieceId, tier);
        if (stat == null) return;

        if (stat.attackDamage > 0) data.attackDamage = Mathf.RoundToInt(stat.attackDamage);
        if (stat.attackRange > 0) data.attackRange = stat.attackRange;
        if (stat.attackCooldown > 0) data.attackCooldown = stat.attackCooldown;
        if (stat.cost > 0) data.cost = stat.cost;

        if (tier > 1)
        {
            Sprite tierSprite = GetTierSprite(data.pieceName, tier);
            if (tierSprite != null)
                data.sprite = tierSprite;
        }
    }

    private int GetPieceIdByName(string pieceName)
    {
        if (string.IsNullOrEmpty(pieceName)) return 0;
        string lower = pieceName.ToLower();
        switch (lower)
        {
            case "bishop": return 10001;
            case "knight": return 10002;
            case "rook": return 10003;
            case "pawn": return 10111;
            case "queen": return 10112;
            case "king": return 10113;
            default: return 0;
        }
    }

    private Sprite GetTierSprite(string pieceName, int tier)
    {
        Sprite databaseSprite = GameManager.Instance.Database.GetSprite($"Char_{pieceName}_{tier}");
        if (databaseSprite != null) return databaseSprite;

        // asset.csv only lists the base pieces, so tier art is resolved from its file naming
        // convention: Resources/Sprites/White/Char_White_<Piece>_<tier>.
        return Resources.Load<Sprite>($"Sprites/White/Char_White_{pieceName}_{tier}");
    }

    private void ApplyCharacterDatabase()
    {
        GameDatabase database = GameManager.Instance?.Database;
        if (database == null) return;

        foreach (PieceData pieceData in allyPiecePool)
        {
            if (pieceData == null || pieceData.team != Team.Ally) continue;

            CharacterRecord record = database.Characters.rows.Find(row =>
                string.Equals(row.name, pieceData.pieceName, System.StringComparison.OrdinalIgnoreCase));
            if (record == null) continue;

            pieceData.attackDamage = record.attackDamage;
            pieceData.attackRange = record.attackRange;
            pieceData.attackCooldown = record.attackCooldown;
            pieceData.cost = record.cost;

            Sprite sprite = database.GetSprite(record.imageResourceId);
            if (sprite != null)
                pieceData.sprite = sprite;
        }
    }

    private PieceData WeightedRandom()
    {
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var pd in gachaPool)
        {
            cumulative += pd.gachaWeight;
            if (roll < cumulative)
                return pd;
        }
        return gachaPool.Count > 0 ? gachaPool[0] : null;
    }

    // Looks up a template by name from the same pool the gacha draws from — used by the
    // merchant to build a specific offer (e.g. "a Bishop") rather than rolling one.
    public PieceData GetTemplateByName(string pieceName)
    {
        foreach (var pd in gachaPool)
        {
            if (pd != null && string.Equals(pd.pieceName, pieceName, StringComparison.OrdinalIgnoreCase))
                return pd;
        }
        return null;
    }
}
