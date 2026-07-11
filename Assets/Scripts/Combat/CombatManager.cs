using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;

    private List<Piece> allyPieces = new List<Piece>();
    private Dictionary<Piece, float> lastAttackTime = new Dictionary<Piece, float>();

    public void RegisterPiece(Piece piece)
    {
        if (!allyPieces.Contains(piece))
            allyPieces.Add(piece);
    }

    public void UnregisterPiece(Piece piece)
    {
        allyPieces.Remove(piece);
        lastAttackTime.Remove(piece);
    }

    private void Update()
    {
        for (int i = allyPieces.Count - 1; i >= 0; i--)
        {
            Piece piece = allyPieces[i];
            if (piece == null || piece.IsDead)
            {
                allyPieces.RemoveAt(i);
                lastAttackTime.Remove(piece);
                continue;
            }
            TryAttack(piece);
        }
    }

    private void TryAttack(Piece piece)
    {
        float lastTime = 0;
        lastAttackTime.TryGetValue(piece, out lastTime);

        if (Time.time - lastTime < piece.Data.attackCooldown)
            return;

        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        lastAttackTime[piece] = Time.time;
        FireProjectile(piece, target);
    }

    private Enemy FindNearestEnemy(Vector3 position, float range)
    {
        Enemy nearest = null;
        float nearestDist = range;
        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            float dist = Vector3.Distance(position, enemy.transform.position);
            if (dist <= nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    private void FireProjectile(Piece piece, Enemy target)
    {
        GameObject proj = Instantiate(projectilePrefab, piece.transform.position, Quaternion.identity);
        Projectile projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
            projComp.Initialize(target, piece.Data.attackDamage, piece.Data.projectileSpeed);
    }
}
