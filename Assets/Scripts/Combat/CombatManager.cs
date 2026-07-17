using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Texture2D rookLaserBeamTexture;

    private List<Piece> allyPieces = new List<Piece>();
    private Dictionary<Piece, float> lastAttackTime = new Dictionary<Piece, float>();
    private Dictionary<Piece, RookLaser> activeLasers = new Dictionary<Piece, RookLaser>();
    private Dictionary<Piece, DragonBreathEffect> activeDragonBreaths = new Dictionary<Piece, DragonBreathEffect>();

    public void RegisterPiece(Piece piece)
    {
        if (!allyPieces.Contains(piece))
            allyPieces.Add(piece);
    }

    public void UnregisterPiece(Piece piece)
    {
        allyPieces.Remove(piece);
        lastAttackTime.Remove(piece);
        activeLasers.Remove(piece);
        activeDragonBreaths.Remove(piece);
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
                activeLasers.Remove(piece);
                activeDragonBreaths.Remove(piece);
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
            case AttackType.Pegasus: TryPegasusAttack(piece); break;
            case AttackType.Dragon: TryDragonAttack(piece); break;
            case AttackType.Cannon: TryCannonAttack(piece); break;
            case AttackType.Meteor: TryMeteorAttack(piece); break;
            case AttackType.Alchemy: TryAlchemyAttack(piece); break;
        }
    }

    private void TryDirectAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        piece.FaceTarget(target.transform.position);
        lastAttackTime[piece] = Time.time;

        float atk = piece.GetAttackDamage();
        float targetHpForBonus = piece.Data.bonusUsesTargetMaxHP ? target.MaxHP : target.CurrentHP;
        float bonusDamage = targetHpForBonus * (piece.Data.bonusMaxHpPercent / 100f);
        float maxBonus = atk * (piece.Data.bonusDamageCapPercent / 100f);
        float totalDamage = atk + Mathf.Min(bonusDamage, maxBonus);

        piece.PlayLungeAttack(target, totalDamage);
    }

    private void TryProjectileAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        piece.FaceTarget(target.transform.position);
        lastAttackTime[piece] = Time.time;
        FireProjectile(piece, target, piece.GetAttackDamage());
    }

    private void TryDiagonalAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        piece.FaceTarget(target.transform.position);
        piece.PlayAttackPunch();
        lastAttackTime[piece] = Time.time;
        float maxDist = piece.Data.attackRange + piece.Data.extraRange;

        Vector3[] diagonals = {
            new Vector3(1, 1, 0).normalized,
            new Vector3(1, -1, 0).normalized,
            new Vector3(-1, 1, 0).normalized,
            new Vector3(-1, -1, 0).normalized
        };

        float atk = piece.GetAttackDamage();
        Color tierColor = PieceData.TierColor(piece.Data.tier);
        tierColor.a = 0.8f;
        foreach (var dir in diagonals)
        {
            GameObject proj = Instantiate(projectilePrefab, piece.transform.position, Quaternion.identity);
            Projectile projComp = proj.GetComponent<Projectile>();
            if (projComp != null)
            {
                projComp.InitializeLine(dir, atk, piece.Data.projectileSpeed, maxDist);
                projComp.SetColor(tierColor);
                projComp.SetSortingOrder(11000);
            }
        }
    }

    private void TryLaserAttack(Piece piece)
    {
        if (activeLasers.TryGetValue(piece, out RookLaser existingLaser))
        {
            if (existingLaser != null) return;

            activeLasers.Remove(piece);
            lastAttackTime[piece] = Time.time;
            return;
        }

        float range = piece.Data.attackRange;
        Enemy target = FindNearestEnemy(piece.transform.position, range);
        if (target == null || target.IsDead) return;

        piece.FaceTarget(target.transform.position);

        float totalDamage = piece.GetAttackDamage() * piece.Data.maxChargeMultiplier;
        GameObject laserGo = new GameObject("Rook Laser");
        laserGo.transform.position = piece.transform.position;
        RookLaser laser = laserGo.AddComponent<RookLaser>();
        laser.Initialize(piece, target, totalDamage, piece.Data.chargeDuration, range, rookLaserBeamTexture);

        activeLasers[piece] = laser;
    }

    private void TryHomingAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        piece.FaceTarget(target.transform.position);
        piece.PlayAttackPunch(piece.GetAttackCooldown());
        lastAttackTime[piece] = Time.time;
        float atk = piece.GetAttackDamage();
        GameObject proj = Instantiate(projectilePrefab, piece.transform.position, Quaternion.identity);
        Projectile projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
        {
            projComp.InitializeHoming(target, atk, piece.Data.projectileSpeed, piece.Data.homingDuration);
            projComp.ConfigureAsQueenMainOrb();
        }

        Vector3 mainDirection = (target.transform.position - piece.transform.position).normalized;
        FireQueenSupportProjectile(piece, mainDirection, -10f, atk * 0.5f);
        FireQueenSupportProjectile(piece, mainDirection, 10f, atk * 0.5f);
    }

    private void TrySplashAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null || target.IsDead) return;

        piece.FaceTarget(target.transform.position);
        piece.PlayAttackPunch();
        lastAttackTime[piece] = Time.time;
        float atk = piece.GetAttackDamage();

        KingWaveEffect.Spawn(target.transform.position, piece.Data.splashRadius);

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

    private void TryPegasusAttack(Piece piece)
    {
        List<Enemy> targets = FindEnemiesInRange(piece.transform.position, piece.Data.attackRange, 3);
        if (targets.Count == 0) return;

        lastAttackTime[piece] = Time.time;
        float attack = piece.GetAttackDamage();
        foreach (Enemy target in targets)
        {
            piece.FaceTarget(target.transform.position);
            MultiLungeEffect.Spawn(piece, target, attack);
        }
    }

    private void TryDragonAttack(Piece piece)
    {
        if (activeDragonBreaths.TryGetValue(piece, out DragonBreathEffect existing))
        {
            if (existing != null) return;
            activeDragonBreaths.Remove(piece);
        }

        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null) return;

        piece.FaceTarget(target.transform.position);
        lastAttackTime[piece] = Time.time;
        var effect = new GameObject("Dragon Breath").AddComponent<DragonBreathEffect>();
        effect.Initialize(piece, (target.transform.position - piece.transform.position).normalized, piece.GetAttackDamage(), piece.Data.attackRange);
        activeDragonBreaths[piece] = effect;
    }

    private void TryCannonAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null) return;

        piece.FaceTarget(target.transform.position);
        piece.PlayAttackPunch(piece.GetAttackCooldown());
        lastAttackTime[piece] = Time.time;
        CannonShell.Spawn(piece.transform.position, (target.transform.position - piece.transform.position).normalized, piece.GetAttackDamage(), piece.Data.projectileSpeed, piece.Data.attackRange, piece.Data.splashRadius);
    }

    private void TryMeteorAttack(Piece piece)
    {
        List<Enemy> targets = FindEnemiesInRange(piece.transform.position, piece.Data.attackRange, 0);
        if (targets.Count == 0) return;

        Enemy target = targets[Random.Range(0, targets.Count)];
        piece.FaceTarget(target.transform.position);
        piece.PlayAttackPunch(piece.GetAttackCooldown());
        lastAttackTime[piece] = Time.time;
        MeteorStrike.Spawn(target.transform.position, piece.GetAttackDamage() * 2.5f, piece.Data.splashRadius);
    }

    private void TryAlchemyAttack(Piece piece)
    {
        Enemy target = FindNearestEnemy(piece.transform.position, piece.Data.attackRange);
        if (target == null) return;

        piece.FaceTarget(target.transform.position);
        piece.PlayAttackPunch(piece.GetAttackCooldown());
        lastAttackTime[piece] = Time.time;
        PotionProjectile.Spawn(piece.transform.position, (target.transform.position - piece.transform.position).normalized, piece.GetAttackDamage(), piece.Data.projectileSpeed, piece.Data.attackRange, piece.Data.splashRadius);
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

    private List<Enemy> FindEnemiesInRange(Vector3 position, float range, int maxCount)
    {
        var result = new List<Enemy>();
        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (Vector3.Distance(position, enemy.transform.position) <= range)
                result.Add(enemy);
        }

        result.Sort((a, b) => Vector3.Distance(position, a.transform.position).CompareTo(Vector3.Distance(position, b.transform.position)));
        if (maxCount > 0 && result.Count > maxCount)
            result.RemoveRange(maxCount, result.Count - maxCount);
        return result;
    }

    private void FireProjectile(Piece piece, Enemy target, float damage)
    {
        GameObject proj = Instantiate(projectilePrefab, piece.transform.position, Quaternion.identity);
        Projectile projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
            projComp.Initialize(target, damage, piece.Data.projectileSpeed);
    }

    private void FireQueenSupportProjectile(Piece piece, Vector3 mainDirection, float angleOffset, float damage)
    {
        Vector3 direction = Quaternion.Euler(0f, 0f, angleOffset) * mainDirection;
        GameObject proj = Instantiate(projectilePrefab, piece.transform.position, Quaternion.identity);
        Projectile projComp = proj.GetComponent<Projectile>();
        if (projComp == null) return;

        projComp.InitializeLine(direction, damage, piece.Data.projectileSpeed, piece.Data.attackRange);
        projComp.ConfigureAsQueenSupportOrb();
    }
}

public class KingWaveEffect : MonoBehaviour
{
    private const int Segments = 48;
    private const float Duration = 0.42f;
    private const float SecondWaveDelay = 0.12f;

    private static Material waveMaterial;

    private Vector3 center;
    private float maxRadius;
    private float elapsed;
    private LineRenderer firstWave;
    private LineRenderer secondWave;

    public static void Spawn(Vector3 position, float radius)
    {
        var waveObject = new GameObject("King Wave Effect");
        var waveEffect = waveObject.AddComponent<KingWaveEffect>();
        waveEffect.Initialize(position, radius);
    }

    private void Initialize(Vector3 position, float radius)
    {
        center = new Vector3(position.x, position.y, -0.2f);
        maxRadius = Mathf.Max(0.1f, radius);
        firstWave = CreateWave(0.09f);
        secondWave = CreateWave(0.06f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        UpdateWave(firstWave, elapsed / Duration);
        UpdateWave(secondWave, (elapsed - SecondWaveDelay) / Duration);

        if (elapsed >= Duration + SecondWaveDelay)
            Destroy(gameObject);
    }

    private LineRenderer CreateWave(float width)
    {
        if (waveMaterial == null)
        {
            waveMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        var wave = gameObject.AddComponent<LineRenderer>();
        wave.useWorldSpace = true;
        wave.loop = true;
        wave.positionCount = Segments;
        wave.sharedMaterial = waveMaterial;
        wave.widthMultiplier = width;
        wave.sortingOrder = 10500;
        return wave;
    }

    private void UpdateWave(LineRenderer wave, float progress)
    {
        if (wave == null) return;

        if (progress <= 0f || progress >= 1f)
        {
            wave.enabled = false;
            return;
        }

        wave.enabled = true;
        float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);
        float radius = Mathf.Lerp(0.08f, maxRadius, easedProgress);
        Color color = new Color(0.25f, 0.8f, 1f, (1f - progress) * 0.75f);
        wave.startColor = color;
        wave.endColor = color;
        wave.widthMultiplier = Mathf.Lerp(0.1f, 0.025f, progress);

        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            wave.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }
}

public static class HeroEffectSprites
{
    private static Sprite smallCircle;
    private static Sprite cannonCircle;
    private static Sprite dragonProjectile;
    private static Sprite cannonProjectile;
    private static Sprite astronomerProjectile;
    private static Sprite alchemistProjectile;

    public static Sprite SmallCircle => GetCircle(ref smallCircle, 48, 48f);
    public static Sprite CannonCircle => GetCircle(ref cannonCircle, 256, 100f);
    public static Sprite DragonProjectile => Load(ref dragonProjectile, "Sprites/White/dragon_proj");
    public static Sprite CannonProjectile => Load(ref cannonProjectile, "Sprites/White/cannon_proj");
    public static Sprite AstronomerProjectile => Load(ref astronomerProjectile, "Sprites/White/astronomer_proj");
    public static Sprite AlchemistProjectile => Load(ref alchemistProjectile, "Sprites/White/alchemist_proj");

    public static float ScaleFor(Sprite sprite, float desiredWorldHeight)
    {
        return sprite == null || sprite.bounds.size.y <= 0.0001f ? desiredWorldHeight : desiredWorldHeight / sprite.bounds.size.y;
    }

    private static Sprite Load(ref Sprite cache, string path)
    {
        if (cache == null)
            cache = Resources.Load<Sprite>(path);
        return cache != null ? cache : SmallCircle;
    }

    private static Sprite GetCircle(ref Sprite cache, int size, float pixelsPerUnit)
    {
        if (cache != null) return cache;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color[size * size];
        float radius = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
            pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius + 0.5f - distance));
        }
        texture.SetPixels(pixels);
        texture.Apply();
        cache = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        cache.hideFlags = HideFlags.HideAndDontSave;
        return cache;
    }
}

public class HeroEffectParticle : MonoBehaviour
{
    private Vector3 velocity;
    private float lifetime;
    private float elapsed;
    private Color color;
    private SpriteRenderer spriteRenderer;
    private Vector3 origin;
    private float maxTravelDistance;

    // spriteFacingDeg: the angle (deg) the sprite art points toward by default; when set (not NaN),
    // the particle rotates so that facing aligns with its travel direction. Left-facing art = 180.
    // maxTravelDistance: when > 0, the particle keeps full opacity and is destroyed once it has
    // travelled this far from its spawn point (instead of the default slow fade-out).
    public static void Spawn(Vector3 position, Vector3 initialVelocity, Color particleColor, float scale, float duration, Sprite sprite = null, float spriteFacingDeg = float.NaN, float maxTravelDistance = 0f)
    {
        var particle = new GameObject("Hero Effect Particle").AddComponent<HeroEffectParticle>();
        particle.Initialize(position, initialVelocity, particleColor, scale, duration, sprite, spriteFacingDeg, maxTravelDistance);
    }

    private void Initialize(Vector3 position, Vector3 initialVelocity, Color particleColor, float scale, float duration, Sprite sprite, float spriteFacingDeg, float maxTravelDistance)
    {
        transform.position = position + Vector3.back * 0.25f;
        origin = transform.position;
        this.maxTravelDistance = maxTravelDistance;
        velocity = initialVelocity;
        color = particleColor;
        lifetime = duration;
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite != null ? sprite : HeroEffectSprites.SmallCircle;
        transform.localScale = Vector3.one * HeroEffectSprites.ScaleFor(spriteRenderer.sprite, scale);
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = 11500;

        if (!float.IsNaN(spriteFacingDeg) && initialVelocity.sqrMagnitude > 0.0001f)
        {
            float travelDeg = Mathf.Atan2(initialVelocity.y, initialVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, travelDeg - spriteFacingDeg);
        }
    }

    // Fraction of maxTravelDistance, near the end, over which the particle quickly fades out.
    private const float RangeFadeZoneFraction = 0.15f;

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (maxTravelDistance > 0f)
        {
            float traveled = Vector3.Distance(origin, transform.position);
            float fadeStart = maxTravelDistance * (1f - RangeFadeZoneFraction);
            float fadeAlpha = fadeStart < maxTravelDistance
                ? 1f - Mathf.Clamp01((traveled - fadeStart) / (maxTravelDistance - fadeStart))
                : 1f;

            Color c = color;
            c.a = color.a * fadeAlpha;
            spriteRenderer.color = c;

            if (traveled >= maxTravelDistance || elapsed >= lifetime)
                Destroy(gameObject);
            return;
        }

        color.a *= 1f - Mathf.Clamp01(Time.deltaTime * 4f);
        spriteRenderer.color = color;
        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}

public static class HeroBurstEffect
{
    public static void Spawn(Vector3 position, Color color, int count, float speed, float scale)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Random.Range(speed * 0.55f, speed);
            HeroEffectParticle.Spawn(position, velocity, color, Random.Range(scale * 0.65f, scale), Random.Range(0.2f, 0.45f));
        }
    }
}

public class MultiLungeEffect : MonoBehaviour
{
    private Enemy target;
    private Vector3 origin;
    private Vector3 impact;
    private float damage;
    private float elapsed;
    private bool applied;

    public static void Spawn(Piece owner, Enemy target, float damage)
    {
        if (owner == null || target == null) return;
        var source = owner.GetComponent<SpriteRenderer>();
        if (source == null) return;

        var effect = new GameObject("Pegasus Attack Image").AddComponent<MultiLungeEffect>();
        effect.transform.position = owner.transform.position;
        effect.transform.localScale = owner.transform.localScale;
        var renderer = effect.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.color = new Color(1f, 1f, 1f, 0.9f);
        renderer.flipX = source.flipX;
        renderer.sortingOrder = source.sortingOrder + 10;
        effect.target = target;
        effect.origin = owner.transform.position;
        Vector3 delta = target.transform.position - effect.origin;
        effect.impact = effect.origin + delta.normalized * Mathf.Max(0f, delta.magnitude - 0.12f);
        effect.damage = damage;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / 0.2f);
        transform.position = Vector3.Lerp(origin, impact, 1f - (1f - progress) * (1f - progress));
        if (!applied && progress >= 0.72f)
        {
            applied = true;
            if (target != null && !target.IsDead)
                target.TakeDamage(damage);
        }
        if (progress >= 1f)
            Destroy(gameObject);
    }
}

public class DragonBreathEffect : MonoBehaviour
{
    private const float Duration = 3f;
    private const float TickInterval = 0.12f;
    private const float FanHalfAngle = 35f;

    private readonly Dictionary<Enemy, float> damageByEnemy = new Dictionary<Enemy, float>();
    private Piece owner;
    private Vector3 direction;
    private float baseAttack;
    private float range;
    private float elapsed;
    private float nextTick;

    public void Initialize(Piece source, Vector3 initialDirection, float attack, float attackRange)
    {
        owner = source;
        direction = initialDirection.sqrMagnitude > 0.001f ? initialDirection.normalized : Vector3.right;
        baseAttack = attack;
        range = attackRange;
    }

    private void Update()
    {
        if (owner == null || owner.IsDead || elapsed >= Duration)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed < nextTick) return;
        nextTick += TickInterval;

        Enemy target = FindNearestEnemy(owner.transform.position, range);
        if (target != null)
        {
            direction = (target.transform.position - owner.transform.position).normalized;
            owner.FaceTarget(target.transform.position);
        }

        EmitFireballs();
        ApplyConeDamage();
    }

    private void EmitFireballs()
    {
        for (int i = -1; i <= 1; i++)
        {
            Vector3 fireDirection = Quaternion.Euler(0f, 0f, i * 22f + Random.Range(-5f, 5f)) * direction;
            // dragon_proj art points left by default (180), so orient it to the travel direction.
            // The fireball flies until it reaches the breath's max range, then disappears (no fade).
            HeroEffectParticle.Spawn(owner.transform.position + fireDirection * 0.35f, fireDirection * Random.Range(3.8f, 5.2f), Color.white, Random.Range(0.85f, 1.05f), 3f, HeroEffectSprites.DragonProjectile, 180f, range);
        }
    }

    private void ApplyConeDamage()
    {
        float damageThisTick = baseAttack * 0.15f;
        float maximumDamage = baseAttack * 3f;
        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            Vector3 toEnemy = enemy.transform.position - owner.transform.position;
            if (toEnemy.magnitude > range || Vector3.Angle(direction, toEnemy) > FanHalfAngle) continue;

            damageByEnemy.TryGetValue(enemy, out float dealt);
            float damage = Mathf.Min(damageThisTick, maximumDamage - dealt);
            if (damage <= 0f) continue;

            enemy.TakeDamage(damage);
            damageByEnemy[enemy] = dealt + damage;
        }
    }

    private static Enemy FindNearestEnemy(Vector3 position, float searchRange)
    {
        Enemy nearest = null;
        float distance = searchRange;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.IsDead) continue;
            float candidate = Vector3.Distance(position, enemy.transform.position);
            if (candidate <= distance)
            {
                distance = candidate;
                nearest = enemy;
            }
        }
        return nearest;
    }
}

public class CannonShell : MonoBehaviour
{
    private Vector3 direction;
    private Vector3 origin;
    private float damage;
    private float speed;
    private float range;
    private float explosionRadius;

    public static void Spawn(Vector3 position, Vector3 fireDirection, float attack, float moveSpeed, float maxRange, float radius)
    {
        var shell = new GameObject("Cannon Shell").AddComponent<CannonShell>();
        shell.transform.position = position + Vector3.back * 0.2f;
        shell.direction = fireDirection.sqrMagnitude > 0.001f ? fireDirection.normalized : Vector3.right;
        shell.origin = position;
        shell.damage = attack;
        shell.speed = Mathf.Max(1f, moveSpeed);
        shell.range = maxRange;
        shell.explosionRadius = Mathf.Max(0.5f, radius);
        var renderer = shell.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = HeroEffectSprites.CannonProjectile;
        shell.transform.localScale = Vector3.one;
        renderer.color = Color.white;
        renderer.sortingOrder = 11200;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        if (Vector3.Distance(origin, transform.position) >= range)
        {
            Destroy(gameObject);
            return;
        }

        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.IsDead) continue;
            if (Vector3.Distance(transform.position, enemy.transform.position) <= 0.6f)
            {
                Explode();
                return;
            }
        }
    }

    private void Explode()
    {
        HeroBurstEffect.Spawn(transform.position, new Color(0.15f, 0.15f, 0.15f), 20, 4.5f, 0.18f);
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy != null && !enemy.IsDead && Vector3.Distance(transform.position, enemy.transform.position) <= explosionRadius)
                enemy.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}

public class MeteorStrike : MonoBehaviour
{
    private Vector3 targetPosition;
    private float damage;
    private float radius;
    private float elapsed;

    public static void Spawn(Vector3 target, float attackDamage, float explosionRadius)
    {
        var meteor = new GameObject("Astronomer Meteor").AddComponent<MeteorStrike>();
        meteor.targetPosition = target;
        meteor.transform.position = target + Vector3.up * 3.5f + Vector3.back * 0.2f;
        meteor.damage = attackDamage;
        meteor.radius = Mathf.Max(0.5f, explosionRadius);
        var renderer = meteor.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = HeroEffectSprites.AstronomerProjectile;
        meteor.transform.localScale = Vector3.one;
        renderer.color = Color.white;
        renderer.sortingOrder = 11300;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / 2f);
        transform.position = Vector3.Lerp(targetPosition + Vector3.up * 3.5f + Vector3.back * 0.2f, targetPosition + Vector3.back * 0.2f, progress * progress);
        if (progress < 1f) return;

        HeroBurstEffect.Spawn(targetPosition, new Color(1f, 0.35f, 0.08f), 26, 4.5f, 0.22f);
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy != null && !enemy.IsDead && Vector3.Distance(targetPosition, enemy.transform.position) <= radius)
                enemy.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}

public class PotionProjectile : MonoBehaviour
{
    private Vector3 direction;
    private Vector3 origin;
    private float damage;
    private float speed;
    private float range;
    private float zoneRadius;

    public static void Spawn(Vector3 position, Vector3 throwDirection, float attack, float moveSpeed, float maxRange, float stickyRadius)
    {
        var potion = new GameObject("Alchemist Potion").AddComponent<PotionProjectile>();
        potion.transform.position = position + Vector3.back * 0.2f;
        potion.direction = throwDirection.sqrMagnitude > 0.001f ? throwDirection.normalized : Vector3.right;
        potion.origin = position;
        potion.damage = attack;
        potion.speed = Mathf.Max(1f, moveSpeed);
        potion.range = maxRange;
        potion.zoneRadius = Mathf.Max(0.5f, stickyRadius);
        var renderer = potion.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = HeroEffectSprites.AlchemistProjectile;
        potion.transform.localScale = Vector3.one;
        renderer.color = Color.white;
        renderer.sortingOrder = 11200;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        transform.Rotate(0f, 0f, 540f * Time.deltaTime);
        if (Vector3.Distance(origin, transform.position) >= range)
        {
            StickyZone.Spawn(transform.position, zoneRadius);
            Destroy(gameObject);
            return;
        }

        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.IsDead) continue;
            if (Vector3.Distance(transform.position, enemy.transform.position) <= 0.38f)
            {
                enemy.TakeDamage(damage);
                StickyZone.Spawn(transform.position, zoneRadius);
                Destroy(gameObject);
                return;
            }
        }
    }
}

public class StickyZone : MonoBehaviour
{
    private const float Duration = 2f;
    private float radius;
    private float elapsed;
    private SpriteRenderer spriteRenderer;

    public static void Spawn(Vector3 position, float zoneRadius)
    {
        var zone = new GameObject("Alchemist Sticky Zone").AddComponent<StickyZone>();
        zone.transform.position = position + Vector3.back * 0.15f;
        zone.radius = zoneRadius;
        zone.transform.localScale = Vector3.one * zoneRadius * 2f;
        zone.spriteRenderer = zone.gameObject.AddComponent<SpriteRenderer>();
        zone.spriteRenderer.sprite = HeroEffectSprites.SmallCircle;
        zone.spriteRenderer.color = new Color(0.25f, 0.8f, 0.25f, 0.38f);
        zone.spriteRenderer.sortingOrder = 10900;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy != null && !enemy.IsDead && Vector3.Distance(transform.position, enemy.transform.position) <= radius)
                enemy.ApplySlow(0.85f, 2f);
        }

        Color color = spriteRenderer.color;
        color.a = 0.38f * (1f - Mathf.Clamp01(elapsed / Duration));
        spriteRenderer.color = color;
        if (elapsed >= Duration)
            Destroy(gameObject);
    }
}
