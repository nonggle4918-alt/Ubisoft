using System;
using System.Collections.Generic;
using UnityEngine;

public enum PieceUpgradeType { Bishop, Knight, Rook }

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public event Action<PieceUpgradeType> OnUpgradeChanged;

    private readonly Dictionary<PieceUpgradeType, int> levels = new Dictionary<PieceUpgradeType, int>
    {
        { PieceUpgradeType.Bishop, 0 },
        { PieceUpgradeType.Knight, 0 },
        { PieceUpgradeType.Rook, 0 }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private GameDatabase Database => GameManager.Instance != null ? GameManager.Instance.Database : null;

    public int GetLevel(PieceUpgradeType type) => levels[type];

    public int MaxLevel => Database != null ? Database.MaxUpgradeLevel : 0;

    public bool IsMaxed(PieceUpgradeType type) => GetLevel(type) >= MaxLevel;

    public int NextCost(PieceUpgradeType type)
    {
        if (Database == null || IsMaxed(type)) return 0;
        PieceUpgradeRecord record = Database.GetUpgrade(GetLevel(type) + 1);
        return record != null ? record.cost : 0;
    }

    public float GetAtkPercentAt(PieceUpgradeType type, int level)
    {
        if (level <= 0 || Database == null) return 0f;
        PieceUpgradeRecord record = Database.GetUpgrade(level);
        return record != null ? SelectAtk(record, type) : 0f;
    }

    public float GetCoolPercentAt(PieceUpgradeType type, int level)
    {
        if (level <= 0 || Database == null) return 0f;
        PieceUpgradeRecord record = Database.GetUpgrade(level);
        return record != null ? SelectCool(record, type) : 0f;
    }

    public float GetAtkPercent(PieceUpgradeType type) => GetAtkPercentAt(type, GetLevel(type));

    public float GetCoolPercent(PieceUpgradeType type) => GetCoolPercentAt(type, GetLevel(type));

    public float GetAtkMultiplier(PieceUpgradeType type) => 1f + GetAtkPercent(type) / 100f;

    public float GetCoolMultiplier(PieceUpgradeType type) => Mathf.Max(0.05f, 1f - GetCoolPercent(type) / 100f);

    public bool TryUpgrade(PieceUpgradeType type)
    {
        if (Database == null || IsMaxed(type)) return false;
        int cost = NextCost(type);
        if (!GameManager.Instance.SpendGold(cost)) return false;
        levels[type] = GetLevel(type) + 1;
        OnUpgradeChanged?.Invoke(type);
        return true;
    }

    public void ResetLevels()
    {
        levels[PieceUpgradeType.Bishop] = 0;
        levels[PieceUpgradeType.Knight] = 0;
        levels[PieceUpgradeType.Rook] = 0;
        OnUpgradeChanged?.Invoke(PieceUpgradeType.Bishop);
        OnUpgradeChanged?.Invoke(PieceUpgradeType.Knight);
        OnUpgradeChanged?.Invoke(PieceUpgradeType.Rook);
    }

    public static bool TryGetType(string pieceName, out PieceUpgradeType type)
    {
        type = PieceUpgradeType.Bishop;
        if (string.IsNullOrEmpty(pieceName)) return false;
        switch (pieceName.ToLower())
        {
            case "bishop": type = PieceUpgradeType.Bishop; return true;
            case "knight": type = PieceUpgradeType.Knight; return true;
            case "rook": type = PieceUpgradeType.Rook; return true;
            default: return false;
        }
    }

    private static float SelectAtk(PieceUpgradeRecord record, PieceUpgradeType type)
    {
        switch (type)
        {
            case PieceUpgradeType.Bishop: return record.bishopAtk;
            case PieceUpgradeType.Knight: return record.knightAtk;
            case PieceUpgradeType.Rook: return record.rookAtk;
            default: return 0f;
        }
    }

    private static float SelectCool(PieceUpgradeRecord record, PieceUpgradeType type)
    {
        switch (type)
        {
            case PieceUpgradeType.Bishop: return record.bishopCool;
            case PieceUpgradeType.Knight: return record.knightCool;
            case PieceUpgradeType.Rook: return record.rookCool;
            default: return 0f;
        }
    }
}
