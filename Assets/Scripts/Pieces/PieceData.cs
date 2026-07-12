using UnityEngine;

public enum Team { Ally, Enemy }

public enum AttackType
{
    Projectile,
    Direct,
    DiagonalProjectile,
    Laser,
    Homing,
    Splash
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

    [Header("Knight")]
    public float bonusMaxHpPercent = 5f;
    public float bonusDamageCapPercent = 100f;

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
}
