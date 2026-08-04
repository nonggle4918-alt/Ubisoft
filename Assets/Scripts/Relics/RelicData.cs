using UnityEngine;

public enum PieceCategory { Normal, Special, Hero }

public enum RelicScope { Global, PieceFamily, PieceCategory, ColossusOnly }

public enum RelicEffectType
{
    AttackDamagePercent,
    AttackSpeedPercent,
    GoldGainPercent,
    UpgradeEffectPercent,
    EnemySlowPercent,
    EnemyDamageTakenPercent
}

[CreateAssetMenu(menuName = "Ubisoft/RelicData")]
public class RelicData : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public int price = 300;
    public bool unique = true;

    public RelicScope scope;
    public UpgradeFamily family;      // only read when scope == PieceFamily (Bishop/Knight/Rook)
    public PieceCategory category;    // only read when scope == PieceCategory
    public RelicEffectType effectType;
    public float value;               // percentage points
}
