using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private const int DeathFragmentCount = 10;

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
    private bool isDying;

    private static Sprite deathFragmentSprite;

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
        if (isDying) return;

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
        if (isDying) return;
        isDying = true;

        int goldReward = data != null ? data.goldReward : 10;
        GameManager.Instance.AddGold(goldReward);
        OnAnyEnemyRemoved?.Invoke();
        SpawnDeathFragments();
        SpawnDeathEffect();
        Destroy(gameObject);
    }

    private void SpawnDeathFragments()
    {
        Sprite fragmentSprite = GetDeathFragmentSprite();
        if (fragmentSprite == null) return;

        Bounds bounds = spriteRenderer != null ? spriteRenderer.bounds : new Bounds(transform.position, Vector3.one);
        int sortingLayerId = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 2;

        for (int i = 0; i < DeathFragmentCount; i++)
        {
            Vector3 spawnPosition = transform.position + new Vector3(
                Random.Range(-bounds.extents.x * 0.65f, bounds.extents.x * 0.65f),
                Random.Range(-bounds.extents.y * 0.35f, bounds.extents.y * 0.35f),
                0f);

            var fragmentObject = new GameObject("Enemy Death Fragment");
            var fragment = fragmentObject.AddComponent<EnemyDeathFragment>();
            fragment.Initialize(
                fragmentSprite,
                spawnPosition,
                new Vector2(Random.Range(-1.4f, 1.4f), Random.Range(0.45f, 1.7f)),
                Random.Range(0.055f, 0.11f),
                Random.Range(-540f, 540f),
                sortingLayerId,
                sortingOrder);
        }
    }

    private static Sprite GetDeathFragmentSprite()
    {
        if (deathFragmentSprite != null)
            return deathFragmentSprite;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        deathFragmentSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        deathFragmentSprite.hideFlags = HideFlags.HideAndDontSave;
        return deathFragmentSprite;
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
