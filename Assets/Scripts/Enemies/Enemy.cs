using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private const int DeathFragmentCount = 10;

    public static event Action OnAnyEnemyRemoved;

    [SerializeField] private PieceData data;
    [SerializeField] private float moveSpeed = 2f;

    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    private List<Vector3> waypoints;
    private int currentWaypointIndex;
    private SpriteRenderer spriteRenderer;
    private float slowTimer;
    private float slowMultiplier = 1f;
    private bool isDying;

    private static readonly Dictionary<Sprite, Sprite[]> fragmentSpriteCache = new();
    private static readonly Color HitFlashColor = new Color(1f, 0.3f, 0.25f);

    private Color originalColor = Color.white;
    private Vector3 baseScale = Vector3.one;
    private Coroutine hitFlashCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        baseScale = transform.localScale;
    }

    private void Start()
    {
        ApplyData();
        EnemyHealthBar.Attach(this);
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
        UpdateFacing(target - transform.position);
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
        MaxHP = data.maxHP;
        CurrentHP = MaxHP;
        if (spriteRenderer != null && data.sprite != null)
            spriteRenderer.sprite = data.sprite;
    }

    private void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sortingOrder = 10000 - Mathf.RoundToInt(transform.position.y * 100f);
    }

    // Enemy art faces right by default; flip only when actually moving left.
    private void UpdateFacing(Vector3 moveDirection)
    {
        if (spriteRenderer == null) return;
        if (Mathf.Abs(moveDirection.x) < 0.001f) return;
        spriteRenderer.flipX = moveDirection.x < 0f;
    }

    public void TakeDamage(float damage)
    {
        if (isDying) return;

        CurrentHP -= Mathf.RoundToInt(damage);
        SpawnDamagePopup(damage);

        if (CurrentHP <= 0)
        {
            Die();
            return;
        }

        SFXManager.Instance?.PlayEnemyHit();
        PlayHitFlash();
    }

    private void PlayHitFlash()
    {
        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        const float duration = 0.12f;
        const float punchScale = 1.22f;

        transform.localScale = baseScale * punchScale;
        if (spriteRenderer != null)
            spriteRenderer.color = HitFlashColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(baseScale * punchScale, baseScale, t);
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(HitFlashColor, originalColor, t);
            yield return null;
        }

        transform.localScale = baseScale;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        hitFlashCoroutine = null;
    }

    private void SpawnDamagePopup(float damage)
    {
        float verticalOffset = spriteRenderer != null ? spriteRenderer.bounds.extents.y + 0.15f : 0.6f;
        int sortingLayerId = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 3 : 3;

        DamagePopup.Spawn(transform.position + Vector3.up * verticalOffset, damage, sortingLayerId, sortingOrder);
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
        SFXManager.Instance?.PlayEnemyDestroyed();
        SpawnDeathFragments();
        Destroy(gameObject);
    }

    private void SpawnDeathFragments()
    {
        Sprite sourceSprite = data != null ? data.sprite : spriteRenderer != null ? spriteRenderer.sprite : null;
        Sprite[] fragmentSprites = GetFragmentSprites(sourceSprite);
        if (fragmentSprites == null) return;

        Bounds bounds = spriteRenderer != null ? spriteRenderer.bounds : new Bounds(transform.position, Vector3.one);
        int sortingLayerId = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 2;

        for (int i = 0; i < DeathFragmentCount; i++)
        {
            Vector3 spawnPosition = transform.position + new Vector3(
                UnityEngine.Random.Range(-bounds.extents.x * 0.5f, bounds.extents.x * 0.5f),
                UnityEngine.Random.Range(-bounds.extents.y * 0.3f, bounds.extents.y * 0.3f),
                0f);

            Sprite fragmentSprite = fragmentSprites[UnityEngine.Random.Range(0, fragmentSprites.Length)];

            var fragmentObject = new GameObject("Enemy Death Fragment");
            var fragment = fragmentObject.AddComponent<EnemyDeathFragment>();
            fragment.Initialize(
                fragmentSprite,
                spawnPosition,
                new Vector2(UnityEngine.Random.Range(-4.5f, 4.5f), UnityEngine.Random.Range(2.5f, 5.5f)),
                UnityEngine.Random.Range(0.45f, 0.85f),
                UnityEngine.Random.Range(-720f, 720f),
                sortingLayerId,
                sortingOrder);
        }
    }

    private static Sprite[] GetFragmentSprites(Sprite source)
    {
        if (source == null) return null;
        if (fragmentSpriteCache.TryGetValue(source, out Sprite[] cached))
            return cached;

        Rect r = source.rect;
        float halfW = r.width * 0.5f;
        float halfH = r.height * 0.5f;
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        float ppu = source.pixelsPerUnit;
        Texture2D texture = source.texture;

        var quadrants = new[]
        {
            Sprite.Create(texture, new Rect(r.x, r.y, halfW, halfH), pivot, ppu),
            Sprite.Create(texture, new Rect(r.x + halfW, r.y, halfW, halfH), pivot, ppu),
            Sprite.Create(texture, new Rect(r.x, r.y + halfH, halfW, halfH), pivot, ppu),
            Sprite.Create(texture, new Rect(r.x + halfW, r.y + halfH, halfW, halfH), pivot, ppu),
        };
        foreach (Sprite quadrant in quadrants)
            quadrant.hideFlags = HideFlags.HideAndDontSave;

        fragmentSpriteCache[source] = quadrants;
        return quadrants;
    }

    private void ReachEnd()
    {
        GameManager.Instance.LoseLife(1);
        OnAnyEnemyRemoved?.Invoke();
        Destroy(gameObject);
    }
}
