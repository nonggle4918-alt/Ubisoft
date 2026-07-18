using UnityEngine;

// Floating HP bar above an enemy, shown only once it has lost health.
// Lives as its own root object that follows the enemy rather than as a child, so the
// enemy's hit-flash scale punch and sprite flipping never distort the bar.
public class EnemyHealthBar : MonoBehaviour
{
    private const float BarWidth = 0.8f;
    private const float BarHeight = 0.1f;
    private const float BorderThickness = 0.02f;
    private const float VerticalPadding = 0.16f;

    private static Sprite barSprite;

    private Enemy owner;
    private SpriteRenderer ownerRenderer;
    private SpriteRenderer background;
    private SpriteRenderer fill;
    private Transform fillPivot;

    public static void Attach(Enemy enemy)
    {
        var barObject = new GameObject("Enemy Health Bar");
        barObject.AddComponent<EnemyHealthBar>().Initialize(enemy);
    }

    private void Initialize(Enemy enemy)
    {
        owner = enemy;
        ownerRenderer = enemy.GetComponent<SpriteRenderer>();

        background = CreateQuad(transform, new Color(0.05f, 0.05f, 0.07f, 0.85f));
        background.transform.localScale = new Vector3(BarWidth + BorderThickness * 2f, BarHeight + BorderThickness * 2f, 1f);

        // The fill scales from its left edge, so it is parented to a pivot sitting there.
        fillPivot = new GameObject("Fill Pivot").transform;
        fillPivot.SetParent(transform, false);
        fillPivot.localPosition = new Vector3(-BarWidth * 0.5f, 0f, 0f);

        fill = CreateQuad(fillPivot, Color.green);
        fill.transform.localPosition = new Vector3(BarWidth * 0.5f, 0f, 0f);
        fill.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);

        SetVisible(false);
        UpdateBar();
    }

    private void LateUpdate()
    {
        // The enemy is destroyed on death or when it reaches the end; go with it.
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateBar();
    }

    private void UpdateBar()
    {
        if (owner.MaxHP <= 0)
        {
            SetVisible(false);
            return;
        }

        float ratio = Mathf.Clamp01((float)owner.CurrentHP / owner.MaxHP);
        if (ratio >= 1f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        FollowOwner();

        // Scale only the fill; the pivot keeps it anchored to the left edge.
        Vector3 scale = fill.transform.localScale;
        scale.x = BarWidth * ratio;
        fill.transform.localScale = scale;
        fill.transform.localPosition = new Vector3(scale.x * 0.5f, 0f, 0f);
        fill.color = HealthColor(ratio);
    }

    private void FollowOwner()
    {
        float topEdge = ownerRenderer != null
            ? ownerRenderer.bounds.max.y
            : owner.transform.position.y + 0.5f;
        transform.position = new Vector3(owner.transform.position.x, topEdge + VerticalPadding, -0.2f);

        // Enemies sort by their Y position, so track the owner's order to stay in front of it.
        int baseOrder = ownerRenderer != null ? ownerRenderer.sortingOrder : 0;
        background.sortingOrder = baseOrder + 1;
        fill.sortingOrder = baseOrder + 2;
    }

    private void SetVisible(bool visible)
    {
        background.enabled = visible;
        fill.enabled = visible;
    }

    private static Color HealthColor(float ratio)
    {
        return ratio > 0.5f
            ? Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(0.35f, 0.9f, 0.3f), (ratio - 0.5f) * 2f)
            : Color.Lerp(new Color(0.9f, 0.2f, 0.18f), new Color(1f, 0.85f, 0.2f), ratio * 2f);
    }

    private static SpriteRenderer CreateQuad(Transform parent, Color color)
    {
        var quad = new GameObject("Quad");
        quad.transform.SetParent(parent, false);

        var renderer = quad.AddComponent<SpriteRenderer>();
        renderer.sprite = BarSprite;
        renderer.color = color;
        return renderer;
    }

    // 4x4 white sprite at 4 pixels-per-unit, so it measures exactly 1x1 world units at
    // scale 1 and localScale can be used directly as the bar's size in world units.
    private static Sprite BarSprite
    {
        get
        {
            if (barSprite != null) return barSprite;

            const int size = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();

            barSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            barSprite.hideFlags = HideFlags.HideAndDontSave;
            return barSprite;
        }
    }
}
