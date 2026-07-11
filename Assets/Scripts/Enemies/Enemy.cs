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
        if (waypoints == null || currentWaypointIndex >= waypoints.Count)
            return;

        Vector3 target = waypoints[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(
            transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
                ReachEnd();
        }
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

    public void TakeDamage(float damage)
    {
        CurrentHP -= Mathf.RoundToInt(damage);
        if (CurrentHP <= 0)
            Die();
    }

    private void Die()
    {
        GameManager.Instance.AddGold(10);
        OnAnyEnemyRemoved?.Invoke();
        SpawnDeathEffect();
        Destroy(gameObject);
    }

    private void SpawnDeathEffect()
    {
        var prefab = Resources.Load<GameObject>("FX/DeathEffect");
        if (prefab != null)
            Instantiate(prefab, transform.position, Quaternion.identity);
    }

    private void ReachEnd()
    {
        GameManager.Instance.LoseLife(1);
        OnAnyEnemyRemoved?.Invoke();
        Destroy(gameObject);
    }
}
