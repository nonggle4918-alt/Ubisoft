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
    private static Sprite queenOrbSprite;
    private static Material queenOrbParticleMaterial;
    private Transform orbParticles;
    private ParticleSystem orbParticleSystem;
    private float orbRotationSpeed;

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
        if (orbParticles != null && orbRotationSpeed != 0f)
            orbParticles.Rotate(0f, 0f, orbRotationSpeed * Time.deltaTime);

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
            DestroyProjectile();
            return;
        }

        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
        {
            target.TakeDamage(damage);
            DestroyProjectile();
        }
    }

    private void UpdateLine()
    {
        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, origin) >= maxDistance)
        {
            DestroyProjectile();
            return;
        }

        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (Vector3.Distance(transform.position, enemy.transform.position) < 0.4f)
            {
                enemy.TakeDamage(damage);
                DestroyProjectile();
                return;
            }
        }
    }

    public void ConfigureAsQueenMainOrb()
    {
        ConfigureQueenOrb(new Color(0.03f, 0.1f, 0.55f, 1f), 0.32f, 360f, 20f);
    }

    public void ConfigureAsQueenSupportOrb()
    {
        ConfigureQueenOrb(new Color(0.2f, 0.48f, 0.95f, 0.95f), 0.18f, 0f, 12f);
    }

    private void ConfigureQueenOrb(Color color, float scale, float rotationSpeed, float particleRate)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = GetQueenOrbSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 11000;
        }

        transform.localScale = Vector3.one * scale;
        orbRotationSpeed = rotationSpeed;
        CreateQueenOrbParticles(color, particleRate);
    }

    public void SetColor(Color color)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = color;
    }

    public void SetSortingOrder(int order)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = order;
    }

    private void UpdateHoming()
    {
        lifetime += Time.deltaTime;

        if (lifetime >= maxLifetime)
        {
            DestroyProjectile();
            return;
        }

        if (target == null)
        {
            DestroyProjectile();
            return;
        }

        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
        {
            target.TakeDamage(damage);
            DestroyProjectile();
        }
    }

    private static Sprite GetQueenOrbSprite()
    {
        if (queenOrbSprite != null) return queenOrbSprite;

        const int size = 32;
        const float radius = (size - 1) * 0.5f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Queen Orb",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        queenOrbSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        queenOrbSprite.hideFlags = HideFlags.HideAndDontSave;
        return queenOrbSprite;
    }

    private void CreateQueenOrbParticles(Color color, float particleRate)
    {
        if (orbParticles != null) return;

        var particleObject = new GameObject("Queen Orb Particles");
        orbParticles = particleObject.transform;
        orbParticles.SetParent(transform, false);

        var particles = particleObject.AddComponent<ParticleSystem>();
        orbParticleSystem = particles;
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startColor = color;
        main.maxParticles = 24;

        var emission = particles.emission;
        emission.rateOverTime = particleRate;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.45f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = GetQueenOrbParticleMaterial();
        particleRenderer.sortingOrder = 11001;
    }

    private void DestroyProjectile()
    {
        if (orbParticleSystem != null)
            orbParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Destroy(gameObject);
    }

    private static Material GetQueenOrbParticleMaterial()
    {
        if (queenOrbParticleMaterial != null) return queenOrbParticleMaterial;

        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Particles/Additive");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        queenOrbParticleMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        if (queenOrbParticleMaterial.HasProperty("_MainTex"))
            queenOrbParticleMaterial.mainTexture = GetQueenOrbSprite().texture;
        if (queenOrbParticleMaterial.HasProperty("_TintColor"))
            queenOrbParticleMaterial.SetColor("_TintColor", Color.white);

        return queenOrbParticleMaterial;
    }
}
