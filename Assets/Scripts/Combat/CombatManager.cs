using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;

    private List<Piece> allyPieces = new List<Piece>();
    private Dictionary<Piece, float> lastAttackTime = new Dictionary<Piece, float>();
    private Dictionary<Piece, float> chargeStartTime = new Dictionary<Piece, float>();

    public void RegisterPiece(Piece piece)
    {
        if (!allyPieces.Contains(piece))
            allyPieces.Add(piece);
    }

    public void UnregisterPiece(Piece piece)
    {
        allyPieces.Remove(piece);
        lastAttackTime.Remove(piece);
        chargeStartTime.Remove(piece);
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
                chargeStartTime.Remove(piece);
                continue;
            }
            piece.ResetAttackBuff();
        }

        ApplyKingBuffs();

        for (int i = allyPieces.Count - 1; i >= 0; i--)
        {
            Piece piece = allyPieces[i];
            if (piece == null || piece.IsDead) continue;
            TryAttack(piece);
        }
    }

    private void TryAttack(Piece piece)
    {
        float lastTime = 0;
        lastAttackTime.TryGetValue(piece, out lastTime);

        if (Time.time - lastTime < piece.GetAttackCooldown())
            return;

        switch (piece.Data.attackType)
        {
            case AttackType.Direct: TryDirectAttack(piece); break;
            case AttackType.Projectile: TryProjectileAttack(piece); break;
            case AttackType.DiagonalProjectile: TryDiagonalAttack(piece); break;
            case AttackType.Laser: TryLaserAttack(piece); break;
            case AttackType.Homing: TryHomingAttack(piece); break;
            case AttackType.Splash: TrySplashAttack(piece); break;
        }
    }

    private void TryDirectAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        lastAttackTime[piece] = Time.time;

        float atk = piece.GetAttackDamage();
        float bonusDamage = target.CurrentHP * (piece.Data.bonusMaxHpPercent / 100f);
        float maxBonus = atk * (piece.Data.bonusDamageCapPercent / 100f);
        float totalDamage = atk + Mathf.Min(bonusDamage, maxBonus);

        target.TakeDamage(totalDamage);
    }

    private void TryProjectileAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        lastAttackTime[piece] = Time.time;
        FireProjectile(piece, target, piece.GetAttackDamage());
    }

    private void TryDiagonalAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        lastAttackTime[piece] = Time.time;
        float maxDist = piece.Data.attackRange + piece.Data.extraRange;

        Vector3[] diagonals = {
            new Vector3(1, 1, 0).normalized,
            new Vector3(1, -1, 0).normalized,
            new Vector3(-1, 1, 0).normalized,
            new Vector3(-1, -1, 0).normalized
        };

        float atk = piece.GetAttackDamage();
        foreach (var dir in diagonals)
        {
            GameObject proj = Instantiate(projectilePrefab, piece.transform.position, Quaternion.identity);
            Projectile projComp = proj.GetComponent<Projectile>();
            if (projComp != null)
                projComp.InitializeLine(dir, atk, piece.Data.projectileSpeed, maxDist);
        }
    }

    private void TryLaserAttack(Piece piece)
    {
        float chargeTime;
        if (!chargeStartTime.TryGetValue(piece, out chargeTime))
        {
            chargeStartTime[piece] = Time.time;
            return;
        }

        float elapsed = Time.time - chargeTime;
        if (elapsed < piece.Data.chargeDuration) return;

        chargeStartTime.Remove(piece);

        float maxDist = piece.Data.attackRange + piece.Data.extraRange;
        Vector3 origin = piece.transform.position;
        Vector3[] directions = { Vector3.right, Vector3.up, Vector3.left, Vector3.down };

        float multiplier = Mathf.Lerp(1f, piece.Data.maxChargeMultiplier, elapsed / piece.Data.chargeDuration);
        float atk = piece.GetAttackDamage();

        foreach (var dir in directions)
        {
            var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 toEnemy = enemy.transform.position - origin;
                float dot = Vector3.Dot(toEnemy.normalized, dir);
                if (dot > 0.7f && toEnemy.magnitude <= maxDist)
                {
                    enemy.TakeDamage(atk * multiplier);
                }
            }
        }

        lastAttackTime[piece] = Time.time;
    }

    private void TryHomingAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        lastAttackTime[piece] = Time.time;
        float atk = piece.GetAttackDamage();
        GameObject proj = Instantiate(projectilePrefab, piece.transform.position, Quaternion.identity);
        Projectile projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
        {
            projComp.InitializeHoming(target, atk, piece.Data.projectileSpeed, piece.Data.homingDuration);
            projComp.SetExtraRange(piece.Data.extraRange);
        }
    }

    private void TrySplashAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        lastAttackTime[piece] = Time.time;
        float atk = piece.GetAttackDamage();

        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (Vector3.Distance(enemy.transform.position, target.transform.position) <= piece.Data.splashRadius)
            {
                enemy.TakeDamage(atk);
                enemy.ApplySlow(piece.Data.slowPercent / 100f, 2f);
            }
        }
    }

    private void ApplyKingBuffs()
    {
        foreach (var king in allyPieces)
        {
            if (king == null || king.IsDead || king.Data.attackType != AttackType.Splash)
                continue;

            foreach (var ally in allyPieces)
            {
                if (ally == null || ally.IsDead || ally == king)
                    continue;

                float dist = Vector3.Distance(king.transform.position, ally.transform.position);
                if (dist <= king.Data.buffRange)
                    ally.AddAttackBuff(king.Data.buffAttackPercent / 100f);
            }
        }
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

    private void FireProjectile(Piece piece, Enemy target, float damage)
    {
        GameObject proj = Instantiate(projectilePrefab, piece.transform.position, Quaternion.identity);
        Projectile projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
            projComp.Initialize(target, damage, piece.Data.projectileSpeed);
    }
}