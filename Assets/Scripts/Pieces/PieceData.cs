using UnityEngine;

public enum Team { Ally, Enemy }

public enum AttackType
{
    Projectile,
    Direct,
    DiagonalProjectile,
    Laser,
    Homing,
    Splash,
    Pegasus,
    Dragon,
    Cannon,
    Meteor,
    Alchemy
}

public enum UpgradeFamily
{
    None,
    Bishop,
    Knight,
    Rook
}

[CreateAssetMenu(menuName = "Ubisoft/PieceData")]
public class PieceData : ScriptableObject
{
    public string pieceName;
    public Team team;
    public AttackType attackType;
    public Sprite sprite;
    public int cost;
    public int maxHP;
    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    public float projectileSpeed;
    public float visualScale = 1f;
    public float movementSpeed = 2f;
    public int goldReward = 10;
    public int tier = 1;
    public UpgradeFamily upgradeFamily;
    public bool isBoss;

    [Header("Knight")]
    public float bonusMaxHpPercent = 5f;
    public float bonusDamageCapPercent = 100f;
    public bool bonusUsesTargetMaxHP;

    [Header("Bishop / Rook / Queen")]
    public int projectileCount = 4;
    public float extraRange = 3f;

    [Header("Rook")]
    public float chargeDuration = 3f;
    public float maxChargeMultiplier = 2f;

    [Header("Queen")]
    public float homingDuration = 1.5f;

    [Header("King")]
    public float splashRadius = 1f;
    public float slowPercent = 20f;
    public float buffRange = 1.5f;
    public float buffAttackPercent = 20f;

    [Header("Gacha")]
    public int gachaWeight = 1000;

    public static Color TierColor(int tier)
    {
        switch (tier)
        {
            case 2: return new Color(0.3f, 1f, 0.45f, 1f);
            case 3: return new Color(1f, 0.9f, 0.25f, 1f);
            case 4: return new Color(0.35f, 0.6f, 1f, 1f);
            case 5: return new Color(1f, 0.3f, 0.3f, 1f);
            default: return new Color(1f, 1f, 1f, 1f);
        }
    }
}
