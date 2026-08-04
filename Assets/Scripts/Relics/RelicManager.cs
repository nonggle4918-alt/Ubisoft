using System;
using System.Collections.Generic;
using UnityEngine;

// Values are looked up live from owned relics rather than baked into pieces, so a
// purchase takes effect on the very next attack/gold-gain/etc. without any migration
// step for pieces already on the board. The relic count stays small (well under 20), so
// recomputing on every call (same approach UpgradeManager already uses) is cheap enough.
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    // Loaded from Resources rather than a serialized list so RelicManager works whether
    // or not it's wired up in a scene — same reasoning as GameDatabase's JSON tables.
    private const string RelicResourcesPath = "Relics";
    private List<RelicData> allRelics = new List<RelicData>();

    private readonly List<RelicData> owned = new List<RelicData>();
    public IReadOnlyList<RelicData> OwnedRelics => owned;
    public event Action<RelicData> OnRelicAcquired;

    private static readonly HashSet<string> HeroNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "Pegasus", "Dragon", "The Colossus", "Cannon", "Astronomer", "Alchemist" };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        allRelics = new List<RelicData>(Resources.LoadAll<RelicData>(RelicResourcesPath));
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsOwned(RelicData relic) => relic != null && owned.Contains(relic);

    // Gold is charged by the caller (MerchantManager) before this runs; this only records
    // ownership and rejects a second copy of a unique relic.
    public bool TryPurchase(RelicData relic)
    {
        if (relic == null || (relic.unique && IsOwned(relic))) return false;

        owned.Add(relic);
        OnRelicAcquired?.Invoke(relic);
        return true;
    }

    public List<RelicData> GetRandomRelics(int count)
    {
        var available = new List<RelicData>();
        foreach (RelicData relic in allRelics)
        {
            if (relic != null && !(relic.unique && IsOwned(relic)))
                available.Add(relic);
        }

        var result = new List<RelicData>();
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
        }
        return result;
    }

    public void ResetRelics() => owned.Clear();

    public static PieceCategory Classify(PieceData data)
    {
        if (data == null) return PieceCategory.Normal;
        if (HeroNames.Contains(data.pieceName)) return PieceCategory.Hero;
        if (data.isSpecialPiece) return PieceCategory.Special;
        return PieceCategory.Normal;
    }

    public float GetAttackDamageMultiplier(Piece piece) => ComputeMultiplier(piece, RelicEffectType.AttackDamagePercent);

    // <1 = faster attacks, matching how PieceData.attackCooldown is consumed elsewhere.
    public float GetAttackSpeedMultiplier(Piece piece)
    {
        float bonus = 0f;
        if (piece == null || piece.Data == null) return 1f;

        foreach (RelicData r in owned)
        {
            if (r.effectType != RelicEffectType.AttackSpeedPercent || !Applies(piece.Data, r)) continue;
            bonus += r.value;
        }
        return Mathf.Max(0.05f, 1f - bonus / 100f);
    }

    public float GoldMultiplier => 1f + SumGlobal(RelicEffectType.GoldGainPercent) / 100f;
    public float UpgradeEffectAmplifier => 1f + SumGlobal(RelicEffectType.UpgradeEffectPercent) / 100f;
    public float EnemyPermanentSlowMultiplier => Mathf.Max(0.3f, 1f - SumGlobal(RelicEffectType.EnemySlowPercent) / 100f);
    public float EnemyDamageTakenMultiplier => 1f + SumGlobal(RelicEffectType.EnemyDamageTakenPercent) / 100f;

    private float ComputeMultiplier(Piece piece, RelicEffectType type)
    {
        if (piece == null || piece.Data == null) return 1f;

        float mult = 1f;
        foreach (RelicData r in owned)
        {
            if (r.effectType != type || !Applies(piece.Data, r)) continue;
            mult *= 1f + r.value / 100f;
        }
        return mult;
    }

    private static bool Applies(PieceData data, RelicData relic)
    {
        switch (relic.scope)
        {
            case RelicScope.Global: return true;
            case RelicScope.PieceFamily: return data.upgradeFamily == relic.family;
            case RelicScope.PieceCategory: return Classify(data) == relic.category;
            case RelicScope.ColossusOnly: return string.Equals(data.pieceName, "The Colossus", StringComparison.OrdinalIgnoreCase);
            default: return false;
        }
    }

    private float SumGlobal(RelicEffectType type)
    {
        float total = 0f;
        foreach (RelicData r in owned)
            if (r.effectType == type) total += r.value;
        return total;
    }
}
