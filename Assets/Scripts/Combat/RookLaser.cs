using System.Collections.Generic;
using UnityEngine;

public class RookLaser : MonoBehaviour
{
    private const float TrackTurnSpeedDeg = 120f;
    private const float HitRadius = 0.45f;

    private const float CoreWidth = 0.13f;
    private const float GlowWidth = 0.62f;
    private const int StrandCount = 3;
    private const float StrandWidth = 0.14f;
    private const float StrandRadius = 0.2f;
    private const float TwistsPerUnit = 0.35f;
    private const float SegmentsPerUnit = 14f;
    private const float SpinSpeed = 7f;
    private const float ParticlesPerUnitPerSecond = 80f;
    private const int EffectSortingBase = 11000;

    private static Material sharedGlowMaterial;
    private static Texture2D sharedGlowTexture;

    private Piece owner;
    private Enemy aimTarget;
    private float totalDamage;
    private float duration;
    private float range;

    private float elapsed;
    private float spinPhase;
    private float emitAccumulator;
    private Vector3 currentDirection;
    private Color tierColor;
    private Color coreColor;
    private readonly Dictionary<Enemy, float> pendingDamage = new Dictionary<Enemy, float>();

    private LineRenderer glowLine;
    private LineRenderer coreLine;
    private LineRenderer[] strandLines;
    private ParticleSystem beamParticles;
    private ParticleSystem.EmitParams emitParams;

    public void Initialize(Piece ownerPiece, Enemy initialTarget, float damage, float attackDuration, float attackRange, Texture2D beamTexture)
    {
        owner = ownerPiece;
        aimTarget = initialTarget;
        totalDamage = damage;
        duration = attackDuration;
        range = attackRange;

        int tier = owner != null && owner.Data != null ? owner.Data.tier : 1;
        tierColor = PieceData.TierColor(tier);
        coreColor = Color.Lerp(tierColor, Color.white, 0.5f);

        Vector3 initialDir = aimTarget != null ? aimTarget.transform.position - owner.transform.position : Vector3.right;
        currentDirection = initialDir.sqrMagnitude > 0.0001f ? initialDir.normalized : Vector3.right;

        glowLine = CreateLine("Glow", EffectSortingBase - 2, GlowWidth, WithAlpha(tierColor, 0.2f));
        coreLine = CreateLine("Core", EffectSortingBase + 1, CoreWidth, WithAlpha(coreColor, 0.6f));

        strandLines = new LineRenderer[StrandCount];
        for (int i = 0; i < StrandCount; i++)
            strandLines[i] = CreateLine("Strand" + i, EffectSortingBase - 1, StrandWidth, WithAlpha(tierColor, 0.55f));

        CreateParticles();
        UpdateBeam();
    }

    private void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        spinPhase += SpinSpeed * Time.deltaTime;

        if (aimTarget == null || aimTarget.IsDead || Vector3.Distance(owner.transform.position, aimTarget.transform.position) > range)
            aimTarget = FindNearestEnemyInRange(owner.transform.position, range);

        if (aimTarget != null)
        {
            Vector3 desiredDir = (aimTarget.transform.position - owner.transform.position).normalized;
            currentDirection = Vector3.RotateTowards(
                currentDirection, desiredDir, TrackTurnSpeedDeg * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }

        UpdateBeam();
        EmitParticles();
        ApplyBeamDamage();

        if (elapsed >= duration)
            Destroy(gameObject);
    }

    private void ApplyBeamDamage()
    {
        Vector3 origin = owner.transform.position;
        float dps = totalDamage / duration;
        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            Vector3 toEnemy = enemy.transform.position - origin;
            float along = Vector3.Dot(toEnemy, currentDirection);
            if (along < 0f || along > range) continue;

            Vector3 closestPointOnBeam = origin + currentDirection * along;
            if (Vector3.Distance(enemy.transform.position, closestPointOnBeam) > HitRadius) continue;

            pendingDamage.TryGetValue(enemy, out float accumulated);
            accumulated += dps * Time.deltaTime;

            if (accumulated >= 1f)
            {
                int tickDamage = Mathf.FloorToInt(accumulated);
                accumulated -= tickDamage;
                enemy.TakeDamage(tickDamage);
            }
            pendingDamage[enemy] = accumulated;
        }
    }

    private void UpdateBeam()
    {
        Vector3 origin = owner.transform.position;
        Vector3 endPoint = origin + currentDirection * range;

        float fade = Mathf.Clamp01(Mathf.Min(elapsed * 5f, (duration - elapsed) * 5f));
        float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 25f, 0f);

        SetStraight(glowLine, origin, endPoint, WithAlpha(tierColor, 0.2f * fade));
        SetStraight(coreLine, origin, endPoint, WithAlpha(coreColor, 0.6f * fade * flicker));
        coreLine.startWidth = CoreWidth * flicker;
        coreLine.endWidth = CoreWidth * flicker;

        GetPerpendicularBasis(currentDirection, out Vector3 right, out Vector3 up);
        int segments = Mathf.Max(2, Mathf.CeilToInt(range * SegmentsPerUnit));
        float totalTwist = range * TwistsPerUnit * Mathf.PI * 2f;

        for (int s = 0; s < strandLines.Length; s++)
        {
            LineRenderer strand = strandLines[s];
            strand.positionCount = segments + 1;
            float strandPhase = spinPhase + s * (Mathf.PI * 2f / StrandCount);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float taper = Mathf.Sin(t * Mathf.PI);
                float angle = strandPhase + t * totalTwist;
                Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * StrandRadius * taper;
                strand.SetPosition(i, origin + currentDirection * (range * t) + offset);
            }

            Color c = WithAlpha(tierColor, 0.55f * fade);
            strand.startColor = c;
            strand.endColor = c;
        }
    }

    private void EmitParticles()
    {
        if (beamParticles == null) return;

        float fade = Mathf.Clamp01(Mathf.Min(elapsed * 5f, (duration - elapsed) * 5f));
        if (fade <= 0f) return;

        Vector3 origin = owner.transform.position;
        GetPerpendicularBasis(currentDirection, out Vector3 right, out Vector3 up);

        emitAccumulator += ParticlesPerUnitPerSecond * range * Time.deltaTime;
        int count = Mathf.FloorToInt(emitAccumulator);
        emitAccumulator -= count;

        for (int i = 0; i < count; i++)
        {
            float t = Random.value;
            float radialAngle = Random.value * Mathf.PI * 2f;
            float radialDist = StrandRadius * (0.3f + Random.value * 0.9f);
            Vector3 radial = right * Mathf.Cos(radialAngle) + up * Mathf.Sin(radialAngle);
            Vector3 pos = origin + currentDirection * (range * t) + radial * radialDist;

            emitParams.position = pos;
            emitParams.velocity = radial * (0.4f + Random.value * 0.6f) + currentDirection * (Random.value - 0.5f);
            emitParams.startColor = WithAlpha(Color.Lerp(tierColor, coreColor, Random.value * 0.5f), (0.28f + Random.value * 0.32f) * fade);
            emitParams.startSize = 0.12f + Random.value * 0.22f;
            emitParams.startLifetime = 0.2f + Random.value * 0.25f;
            beamParticles.Emit(emitParams, 1);
        }
    }

    private static void SetStraight(LineRenderer line, Vector3 a, Vector3 b, Color color)
    {
        line.positionCount = 2;
        line.SetPosition(0, a);
        line.SetPosition(1, b);
        line.startColor = color;
        line.endColor = color;
    }

    private static void GetPerpendicularBasis(Vector3 forward, out Vector3 right, out Vector3 up)
    {
        Vector3 reference = Mathf.Abs(Vector3.Dot(forward, Vector3.forward)) > 0.95f ? Vector3.up : Vector3.forward;
        right = Vector3.Cross(forward, reference).normalized;
        up = Vector3.Cross(right, forward).normalized;
    }

    private LineRenderer CreateLine(string childName, int order, float width, Color color)
    {
        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.material = GetSharedGlowMaterial();
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = order;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    private void CreateParticles()
    {
        GameObject go = new GameObject("BeamParticles");
        go.transform.SetParent(transform, false);

        beamParticles = go.AddComponent<ParticleSystem>();

        var main = beamParticles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1500;
        main.startLifetime = 0.35f;
        main.startSpeed = 0f;
        main.startSize = 0.2f;
        main.gravityModifier = 0f;

        var emission = beamParticles.emission;
        emission.rateOverTime = 0f;

        var shape = beamParticles.shape;
        shape.enabled = false;

        var colorOverLifetime = beamParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = grad;

        var sizeOverLifetime = beamParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetSharedGlowMaterial();
        renderer.sortingOrder = EffectSortingBase;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        beamParticles.Play();
        emitParams = new ParticleSystem.EmitParams();
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Material GetSharedGlowMaterial()
    {
        if (sharedGlowMaterial != null) return sharedGlowMaterial;

        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Particles/Additive");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");

        if (shader != null)
        {
            sharedGlowMaterial = new Material(shader);
            if (sharedGlowMaterial.HasProperty("_TintColor"))
                sharedGlowMaterial.SetColor("_TintColor", Color.white);
        }
        else
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            sharedGlowMaterial = new Material(shader);
            ConfigureAdditive(sharedGlowMaterial);
        }

        sharedGlowMaterial.mainTexture = GetSharedGlowTexture();
        return sharedGlowMaterial;
    }

    private static void ConfigureAdditive(Material mat)
    {
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f);
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
    }

    private static Texture2D GetSharedGlowTexture()
    {
        if (sharedGlowTexture != null) return sharedGlowTexture;

        int size = 64;
        sharedGlowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        sharedGlowTexture.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        float maxDist = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / maxDist;
                float a = Mathf.Clamp01(1f - dist);
                a = a * a;
                sharedGlowTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        sharedGlowTexture.Apply();
        return sharedGlowTexture;
    }

    private static Enemy FindNearestEnemyInRange(Vector3 position, float searchRange)
    {
        Enemy nearest = null;
        float nearestDist = searchRange;
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
}
