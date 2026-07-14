using UnityEngine;

public enum ProjectileMode { Standard, Line, Homing }

public class Projectile : MonoBehaviour
{
    private Enemy target;
    private float damage;
    private float speed;
    private ProjectileMode mode = ProjectileMode.Standard;
    private Vector3 direction;
    private float lifetime;
    private float maxLifetime;
    private float maxDistance;
    private Vector3 origin;
    private float extraRange;

    public void Initialize(Enemy targetEnemy, float attackDamage, float moveSpeed)
    {
        target = targetEnemy;
        damage = attackDamage;
        speed = moveSpeed;
        mode = ProjectileMode.Standard;
    }

    public void InitializeLine(Vector3 dir, float attackDamage, float moveSpeed, float maxDist)
    {
        target = null;
        damage = attackDamage;
        speed = moveSpeed;
        mode = ProjectileMode.Line;
        direction = dir.normalized;
        origin = transform.position;
        maxDistance = maxDist;
    }

    public void InitializeHoming(Enemy targetEnemy, float attackDamage, float moveSpeed, float homingTime)
    {
        target = targetEnemy;
        damage = attackDamage;
        speed = moveSpeed;
        mode = ProjectileMode.Homing;
        maxLifetime = homingTime;
        lifetime = 0;
    }

    private void Update()
    {
        switch (mode)
        {
            case ProjectileMode.Standard: UpdateStandard(); break;
            case ProjectileMode.Line: UpdateLine(); break;
            case ProjectileMode.Homing: UpdateHoming(); break;
        }
    }

    private void UpdateStandard()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
        {
            target.TakeDamage(damage);
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    private void UpdateLine()
    {
        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, origin) >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (Vector3.Distance(transform.position, enemy.transform.position) < 0.4f)
            {
                enemy.TakeDamage(damage);
                SpawnHitEffect();
                Destroy(gameObject);
                return;
            }
        }
    }

    public void SetExtraRange(float range)
    {
        extraRange = range;
    }

    private void UpdateHoming()
    {
        lifetime += Time.deltaTime;

        if (lifetime >= maxLifetime)
        {
            if (target != null)
                direction = (target.transform.position - transform.position).normalized;
            mode = ProjectileMode.Line;
            origin = transform.position;
            maxDistance = extraRange;
            return;
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
        {
            target.TakeDamage(damage);
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect()
    {
        var prefab = Resources.Load<GameObject>("FX/HitEffect");
        if (prefab != null)
        {
            GameObject effect = Instantiate(prefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
}
