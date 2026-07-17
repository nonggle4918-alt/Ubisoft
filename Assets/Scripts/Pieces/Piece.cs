using System.Collections;
using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] private PieceData data;

    public PieceData Data => data;
    public int CurrentHP { get; private set; }
    public Team Team => data != null ? data.team : Team.Ally;
    public bool IsDead => CurrentHP <= 0;
    public float AttackBuff { get; private set; } = 1f;
    public GridCell CurrentCell { get; set; }

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private Vector3 visualScale;
    private Coroutine lungeCoroutine;
    private Coroutine attackPunchCoroutine;
    private Coroutine promotionCoroutine;
    private float spawnedAt;
    private bool promotionStarted;

    private const float DebugPromotionDelay = 10f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        visualScale = baseScale;
    }

    private void Start()
    {
        ApplyData();
        spawnedAt = Time.time;
        RegisterToCombat();
    }

    private void Update()
    {
        if (promotionStarted || data == null || !string.Equals(data.pieceName, "Pawn", System.StringComparison.OrdinalIgnoreCase)) return;
        if (Time.time - spawnedAt < DebugPromotionDelay) return;

        promotionStarted = true;
        promotionCoroutine = StartCoroutine(PromotionRoutine());
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    private void OnDestroy()
    {
        if (CurrentCell != null)
            CurrentCell.RemovePiece();

        var cm = FindFirstObjectByType<CombatManager>();
        if (cm != null)
            cm.UnregisterPiece(this);
    }

    private void ApplyData()
    {
        if (data == null) return;
        CurrentHP = data.maxHP;
        if (spriteRenderer != null && data.sprite != null)
            spriteRenderer.sprite = data.sprite;

        // The piece renders at its sprite's original image size (governed by the
        // sprite's Pixels Per Unit), scaled by the optional visualScale multiplier.
        float scale = data.visualScale > 0.0001f ? data.visualScale : 1f;
        visualScale = baseScale * scale;
        transform.localScale = visualScale;
    }

    private void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100f);
    }

    private void RegisterToCombat()
    {
        if (Team != Team.Ally) return;
        var cm = FindFirstObjectByType<CombatManager>();
        if (cm != null)
            cm.RegisterPiece(this);
    }

    public void SetData(PieceData newData)
    {
        data = newData;
        ApplyData();
    }

    public void AddAttackBuff(float multiplier)
    {
        AttackBuff += multiplier;
    }

    public void ResetAttackBuff()
    {
        AttackBuff = 1f;
    }

    public float GetAttackDamage()
    {
        return data.attackDamage * AttackBuff * GetUpgradeAtkMultiplier();
    }

    public float GetAttackCooldown()
    {
        return data.attackCooldown * GetUpgradeCoolMultiplier();
    }

    private float GetUpgradeAtkMultiplier()
    {
        if (UpgradeManager.Instance == null || data == null) return 1f;
        if (TryGetUpgradeType(out PieceUpgradeType type))
            return UpgradeManager.Instance.GetAtkMultiplier(type);
        return 1f;
    }

    private float GetUpgradeCoolMultiplier()
    {
        if (UpgradeManager.Instance == null || data == null) return 1f;
        if (TryGetUpgradeType(out PieceUpgradeType type))
            return UpgradeManager.Instance.GetCoolMultiplier(type);
        return 1f;
    }

    private bool TryGetUpgradeType(out PieceUpgradeType type)
    {
        type = PieceUpgradeType.Bishop;
        if (data == null) return false;

        switch (data.upgradeFamily)
        {
            case UpgradeFamily.Bishop: type = PieceUpgradeType.Bishop; return true;
            case UpgradeFamily.Knight: type = PieceUpgradeType.Knight; return true;
            case UpgradeFamily.Rook: type = PieceUpgradeType.Rook; return true;
            default: return UpgradeManager.TryGetType(data.pieceName, out type);
        }
    }

    private IEnumerator PromotionRoutine()
    {
        PromotionFireworksEffect.Spawn(transform.position);
        yield return new WaitForSeconds(0.45f);

        if (this == null || data == null) yield break;
        SetData(PromotionFactory.Create(data));
        PromotionFireworksEffect.Spawn(transform.position);
        promotionCoroutine = null;
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        float deltaX = targetPosition.x - transform.position.x;
        if (Mathf.Abs(deltaX) < 0.001f) return;
        spriteRenderer.flipX = deltaX < 0f;
    }

    public void PlayAttackPunch(float attackInterval = 0f)
    {
        if (attackPunchCoroutine != null)
            StopCoroutine(attackPunchCoroutine);

        transform.localScale = visualScale;
        const float defaultDuration = 0.28f;
        float duration = attackInterval > 0f ? Mathf.Min(attackInterval, defaultDuration) : defaultDuration;
        attackPunchCoroutine = StartCoroutine(AttackPunchRoutine(duration));
    }

    private IEnumerator AttackPunchRoutine(float totalDuration)
    {
        const float shrinkFactor = 0.8f;
        const float overshootFactor = 1.15f;
        float shrinkDuration = totalDuration * 0.25f;
        float growDuration = totalDuration * 0.32f;
        float settleDuration = totalDuration * 0.43f;

        Vector3 restScale = visualScale;
        Vector3 shrinkScale = restScale * shrinkFactor;
        Vector3 overshootScale = restScale * overshootFactor;

        yield return ScaleLerp(restScale, shrinkScale, shrinkDuration);
        yield return ScaleLerp(shrinkScale, overshootScale, growDuration);
        yield return ScaleLerp(overshootScale, restScale, settleDuration);

        transform.localScale = restScale;
        attackPunchCoroutine = null;
    }

    private IEnumerator ScaleLerp(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / duration));
            transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.localScale = to;
    }

    public void PlayLungeAttack(Enemy target, float damage)
    {
        if (lungeCoroutine != null)
            StopCoroutine(lungeCoroutine);
        lungeCoroutine = StartCoroutine(LungeRoutine(target, damage));
    }

    private IEnumerator LungeRoutine(Enemy target, float damage)
    {
        const float outDuration = 0.09f;
        const float backDuration = 0.16f;
        const float lungeFraction = 0.85f;
        const float impactSquash = 1.18f;

        Vector3 origin = transform.position;
        Vector3 restScale = transform.localScale;
        Vector3 squashScale = new Vector3(restScale.x * impactSquash, restScale.y / impactSquash, restScale.z);

        Vector3 toTarget = target != null ? target.transform.position - origin : Vector3.zero;
        float distance = toTarget.magnitude;
        Vector3 lungePosition = distance > 0.001f
            ? origin + toTarget.normalized * Mathf.Min(distance * lungeFraction, Mathf.Max(distance - 0.1f, 0f))
            : origin;

        float elapsed = 0f;
        while (elapsed < outDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / outDuration));
            transform.position = Vector3.Lerp(origin, lungePosition, t);
            yield return null;
        }
        transform.position = lungePosition;

        if (target != null && !target.IsDead)
            target.TakeDamage(damage);

        transform.localScale = squashScale;

        elapsed = 0f;
        while (elapsed < backDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / backDuration));
            transform.position = Vector3.Lerp(lungePosition, origin, t);
            transform.localScale = Vector3.Lerp(squashScale, restScale, t);
            yield return null;
        }

        transform.position = origin;
        transform.localScale = restScale;
        lungeCoroutine = null;
    }

    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

    public void TakeDamage(float damage)
    {
        CurrentHP -= Mathf.RoundToInt(damage);
        if (CurrentHP <= 0)
            Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}

public static class PromotionFactory
{
    public static PieceData Create(PieceData pawnData)
    {
        var data = ScriptableObject.CreateInstance<PieceData>();
        data.team = Team.Ally;
        data.maxHP = pawnData != null ? pawnData.maxHP : 50;
        data.cost = pawnData != null ? pawnData.cost : 50;
        data.visualScale = 1f;
        data.movementSpeed = 2f;
        data.gachaWeight = 0;
        data.tier = 1;

        switch (Random.Range(0, 6))
        {
            case 0:
                Configure(data, "Pegasus", AttackType.Pegasus, UpgradeFamily.Knight, "Sprites/White/char_white_pegasus", 30f, 4f, 0.5f, 8f);
                break;
            case 1:
                Configure(data, "Dragon", AttackType.Dragon, UpgradeFamily.Knight, "Sprites/White/char_white_dragon", 15f, 4f, 1.5f, 7f);
                break;
            case 2:
                Configure(data, "The Colossus", AttackType.Direct, UpgradeFamily.Rook, "Sprites/White/char_white_thecolosus", 120f, 3f, 3f, 0f);
                data.bonusMaxHpPercent = 5f;
                data.bonusDamageCapPercent = 500f;
                data.bonusUsesTargetMaxHP = true;
                break;
            case 3:
                Configure(data, "Cannon", AttackType.Cannon, UpgradeFamily.Rook, "Sprites/White/Char_White_cannon", 30f, 6f, 1f, 8f);
                data.splashRadius = 1.4f;
                break;
            case 4:
                Configure(data, "Astronomer", AttackType.Meteor, UpgradeFamily.Bishop, "Sprites/White/Char_White_Astronomer", 30f, 6f, 3f, 0f);
                data.splashRadius = 1.25f;
                break;
            default:
                Configure(data, "Alchemist", AttackType.Alchemy, UpgradeFamily.Bishop, "Sprites/White/Char_White_alchemist", 26f, 5f, 1f, 7f);
                data.splashRadius = 1.15f;
                data.slowPercent = 15f;
                break;
        }

        return data;
    }

    private static void Configure(PieceData data, string name, AttackType attackType, UpgradeFamily family, string spritePath, float damage, float range, float cooldown, float projectileSpeed)
    {
        data.pieceName = name;
        data.attackType = attackType;
        data.upgradeFamily = family;
        data.attackDamage = damage;
        data.attackRange = range;
        data.attackCooldown = cooldown;
        data.projectileSpeed = projectileSpeed;
        data.sprite = Resources.Load<Sprite>(spritePath);
    }
}

public static class PromotionFireworksEffect
{
    private static Sprite sparkSprite;

    public static void Spawn(Vector3 position)
    {
        Color[] colors =
        {
            new Color(1f, 0.75f, 0.2f),
            new Color(1f, 0.35f, 0.65f),
            new Color(0.3f, 0.85f, 1f),
            Color.white
        };

        for (int i = 0; i < 18; i++)
        {
            float angle = i * Mathf.PI * 2f / 18f + Random.Range(-0.12f, 0.12f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Random.Range(1.3f, 2.7f);
            var spark = new GameObject("Promotion Spark").AddComponent<PromotionSpark>();
            spark.Initialize(position, velocity, colors[i % colors.Length], Random.Range(0.06f, 0.12f));
        }
    }

    public static Sprite SparkSprite
    {
        get
        {
            if (sparkSprite != null) return sparkSprite;

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(center - distance));
            }
            texture.SetPixels(pixels);
            texture.Apply();
            sparkSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sparkSprite.hideFlags = HideFlags.HideAndDontSave;
            return sparkSprite;
        }
    }
}

public class PromotionSpark : MonoBehaviour
{
    private Vector3 velocity;
    private Color color;
    private float lifetime;
    private float elapsed;
    private SpriteRenderer spriteRenderer;

    public void Initialize(Vector3 position, Vector3 initialVelocity, Color sparkColor, float scale)
    {
        transform.position = position + Vector3.back * 0.3f;
        velocity = initialVelocity;
        color = sparkColor;
        transform.localScale = Vector3.one * scale;
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = PromotionFireworksEffect.SparkSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = 12000;
        lifetime = Random.Range(0.35f, 0.6f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        velocity += Vector3.down * 2.5f * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        color.a = 1f - Mathf.Clamp01(elapsed / lifetime);
        spriteRenderer.color = color;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
