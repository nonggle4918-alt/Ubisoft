using UnityEngine;

// One-shot lightning bolt that cracks down from above onto a newly arrived piece.
// Reads clearly against the busy board art where the old soft-particle burst did not:
// a bright high-contrast jagged bolt plus an impact flash at the strike point.
public class LightningStrikeEffect : MonoBehaviour
{
    private const float Duration = 0.55f;
    private const float BoltHeight = 4.5f;
    private const int SegmentCount = 16;
    private const float JitterX = 0.24f;
    private const float FlickerChance = 0.4f;

    private static Material boltMaterial;

    private LineRenderer bolt;
    private SpriteRenderer flash;
    private Color tint;
    private Vector3 strikePoint;
    private float elapsed;

    public static void Spawn(Vector3 position, Color tint)
    {
        var effectObject = new GameObject("Piece Lightning Strike");
        effectObject.AddComponent<LightningStrikeEffect>().Initialize(position, tint);
    }

    private void Initialize(Vector3 position, Color color)
    {
        // Sits in front of the board and pieces so the bolt is never occluded.
        strikePoint = new Vector3(position.x, position.y, -0.4f);
        tint = color;
        transform.position = strikePoint;

        bolt = CreateBolt();
        flash = CreateFlash();
        BuildBoltPath();

        // Seed the first frame explicitly - Update does not run until the next frame, and the
        // defaults (full-size opaque white flash) would pop before the animation takes over.
        ApplyBoltAlpha(1f);
        UpdateFlash(0f, 1f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Duration);

        // Re-jag the bolt during the first half so it crackles instead of sitting static.
        if (progress < 0.5f && Random.value < FlickerChance)
            BuildBoltPath();

        // Hold near full brightness briefly, then fall off quickly.
        float alpha = 1f - progress * progress;
        ApplyBoltAlpha(alpha);
        UpdateFlash(progress, alpha);

        if (progress >= 1f)
            Destroy(gameObject);
    }

    private LineRenderer CreateBolt()
    {
        if (boltMaterial == null)
        {
            boltMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        // LineRenderer and SpriteRenderer are both Renderers, so the bolt and the flash each
        // need their own GameObject - a second Renderer on one object would fail to be added.
        var boltObject = new GameObject("Bolt");
        boltObject.transform.SetParent(transform, false);

        var line = boltObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = SegmentCount;
        line.sharedMaterial = boltMaterial;
        line.numCapVertices = 2;
        line.sortingOrder = 12000;
        line.widthCurve = AnimationCurve.Linear(0f, 0.07f, 1f, 0.2f);
        return line;
    }

    private SpriteRenderer CreateFlash()
    {
        var flashObject = new GameObject("Impact Flash");
        flashObject.transform.SetParent(transform, false);
        flashObject.transform.position = strikePoint;

        var renderer = flashObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PromotionFireworksEffect.SparkSprite;
        renderer.sortingOrder = 11900;
        return renderer;
    }

    // Walks from the sky down to the strike point, offsetting each joint horizontally.
    // The jitter tapers to zero at the bottom so the bolt lands exactly on the piece.
    private void BuildBoltPath()
    {
        Vector3 top = strikePoint + Vector3.up * BoltHeight;

        for (int i = 0; i < SegmentCount; i++)
        {
            float t = (float)i / (SegmentCount - 1);
            Vector3 point = Vector3.Lerp(top, strikePoint, t);
            if (i > 0 && i < SegmentCount - 1)
                point.x += Random.Range(-JitterX, JitterX) * (1f - t);
            bolt.SetPosition(i, point);
        }
    }

    private void ApplyBoltAlpha(float alpha)
    {
        // White-hot core fading into the piece's tint toward the strike point.
        Color head = Color.Lerp(Color.white, tint, 0.15f);
        head.a = alpha;
        Color tail = tint;
        tail.a = alpha;

        bolt.startColor = head;
        bolt.endColor = tail;
    }

    private void UpdateFlash(float progress, float alpha)
    {
        float scale = Mathf.Lerp(0.5f, 2.1f, progress);
        flash.transform.localScale = Vector3.one * scale;

        Color color = Color.Lerp(Color.white, tint, 0.55f);
        color.a = alpha * 0.85f;
        flash.color = color;
    }
}
