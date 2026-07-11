using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Enemy target;
    private float damage;
    private float speed;

    public void Initialize(Enemy targetEnemy, float attackDamage, float moveSpeed)
    {
        target = targetEnemy;
        damage = attackDamage;
        speed = moveSpeed;
    }

    private void Update()
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

    private void SpawnHitEffect()
    {
        var prefab = Resources.Load<GameObject>("FX/HitEffect");
        if (prefab != null)
            Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
