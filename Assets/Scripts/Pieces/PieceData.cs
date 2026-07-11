using UnityEngine;

public enum Team { Ally, Enemy }

[CreateAssetMenu(menuName = "Ubisoft/PieceData")]
public class PieceData : ScriptableObject
{
    public string pieceName;
    public Team team;
    public Sprite sprite;
    public int cost;
    public int maxHP;
    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    public float projectileSpeed;
}
