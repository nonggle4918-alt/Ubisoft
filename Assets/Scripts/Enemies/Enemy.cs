using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static event Action OnAnyEnemyRemoved;

    [SerializeField] private PieceData data;
    [SerializeField] private float moveSpeed = 2f;

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    private List<Vector3> waypoints;
    private int currentWaypointIndex;
    private SpriteRenderer spriteRenderer;
    private float slowTimer;
    private float slowMultiplier = 1f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ApplyData();
    }

    private void Update()
    {
        if (slowTimer > 0)
            slowTimer -= Time.deltaTime;
        else
            slowMultiplier = 1f;

        if (waypoints == null || currentWaypointIndex >= waypoints.Count)
            return;

        Vector3 target = waypoints[currentWaypointIndex];
        float speed = data != null && data.movementSpeed > 0f ? data.movementSpeed : moveSpeed;
        transform.position = Vector3.MoveTowards(
            transform.position, target, speed * slowMultiplier * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
                ReachEnd();
        }
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    public void SetData(PieceData newData)
    {
        data = newData;
        ApplyData();
    }

    public void SetWaypoints(List<Vector3> waypointList)
    {
        waypoints = waypointList;
        currentWaypointIndex = 0;
    }

    private void ApplyData()
    {
        if (data == null) return;
        CurrentHP = data.maxHP;
        if (spriteRenderer != null && data.sprite != null)
            spriteRenderer.sprite = data.sprite;
    }

    private void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sortingOrder = 10000 - Mathf.RoundToInt(transform.position.y * 100f);
    }

    public void TakeDamage(float damage)
    {
        CurrentHP -= Mathf.RoundToInt(damage);
        if (CurrentHP <= 0)
            Die();
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    private void Die()
    {
        int goldReward = data != null ? data.goldReward : 10;
        GameManager.Instance.AddGold(goldReward);
        OnAnyEnemyRemoved?.Invoke();
        SpawnDeathEffect();
        Destroy(gameObject);
    }

    private void SpawnDeathEffect()
    {
        var prefab = Resources.Load<GameObject>("FX/DeathEffect");
        if (prefab != null)
        {
            GameObject effect = Instantiate(prefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    private void ReachEnd()
    {
        GameManager.Instance.LoseLife(1);
        OnAnyEnemyRemoved?.Invoke();
        Destroy(gameObject);
    }
}
