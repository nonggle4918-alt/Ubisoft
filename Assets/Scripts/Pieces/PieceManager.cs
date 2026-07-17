using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PieceManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Piece piecePrefab;
    [SerializeField] private int pullCost = 50;
    [SerializeField] private List<PieceData> allyPiecePool = new List<PieceData>();

    public event Action<PieceData> OnPiecePulled;

    private List<PieceData> gachaPool = new List<PieceData>();
    private int totalWeight;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            PullPiece();
    }

    private void Start()
    {
        ApplyCharacterDatabase();

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

        if (!GameManager.Instance.SpendGold(pullCost))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }

        PieceData selected = WeightedRandom();
        if (selected == null)
        {
            GameManager.Instance.AddGold(pullCost);
            return;
        }

        GridCell cell = gridManager.GetEmptyCell();
        if (cell == null)
        {
            Debug.Log("빈 칸이 없습니다.");
            GameManager.Instance.AddGold(pullCost);
            return;
        }

        int tier = GameManager.Instance.Database.RollTier();

        PieceData runtimeData = ScriptableObject.CreateInstance<PieceData>();
        runtimeData.pieceName = selected.pieceName;
        runtimeData.team = selected.team;
        runtimeData.attackType = selected.attackType;
        runtimeData.maxHP = selected.maxHP;
        runtimeData.attackDamage = selected.attackDamage;
        runtimeData.attackRange = selected.attackRange;
        runtimeData.attackCooldown = selected.attackCooldown;
        runtimeData.cost = selected.cost;
        runtimeData.projectileSpeed = selected.projectileSpeed;
        runtimeData.visualScale = selected.visualScale;
        runtimeData.bonusMaxHpPercent = selected.bonusMaxHpPercent;
        runtimeData.bonusDamageCapPercent = selected.bonusDamageCapPercent;
        runtimeData.extraRange = selected.extraRange;
        runtimeData.chargeDuration = selected.chargeDuration;
        runtimeData.maxChargeMultiplier = selected.maxChargeMultiplier;
        runtimeData.homingDuration = selected.homingDuration;
        runtimeData.splashRadius = selected.splashRadius;
        runtimeData.slowPercent = selected.slowPercent;
        runtimeData.buffRange = selected.buffRange;
        runtimeData.buffAttackPercent = selected.buffAttackPercent;
        runtimeData.movementSpeed = selected.movementSpeed;
        runtimeData.goldReward = selected.goldReward;
        runtimeData.projectileCount = selected.projectileCount;
        runtimeData.sprite = selected.sprite;

        runtimeData.tier = tier;
        ApplyTierToData(runtimeData, tier);

        Piece piece = Instantiate(piecePrefab, cell.transform.position, Quaternion.identity);
        piece.SetData(runtimeData);
        piece.CurrentCell = cell;
        cell.SetPiece(piece);

        OnPiecePulled?.Invoke(runtimeData);
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
        string resourceId = $"Char_{pieceName}_{tier}";
        return GameManager.Instance.Database.GetSprite(resourceId);
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
}
