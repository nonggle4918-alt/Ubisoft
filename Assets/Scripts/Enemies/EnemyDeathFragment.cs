using UnityEngine;

public class EnemyDeathFragment : MonoBehaviour
{
    private const float Lifetime = 1.1f;
    private const float Gravity = 8.5f;

    private SpriteRenderer spriteRenderer;
    private Vector2 velocity;
    private float angularVelocity;
    private float elapsed;

    public void Initialize(Sprite sprite, Vector3 position, Vector2 initialVelocity, float size, float rotationSpeed, int sortingLayerId, int sortingOrder)
    {
        transform.position = position;
        transform.localScale = Vector3.one * size;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingLayerID = sortingLayerId;
        spriteRenderer.sortingOrder = sortingOrder;

        velocity = initialVelocity;
        angularVelocity = rotationSpeed;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        velocity += Vector2.down * Gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, angularVelocity * Time.deltaTime);

        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(1f - elapsed / Lifetime);
        spriteRenderer.color = color;

        if (elapsed >= Lifetime)
            Destroy(gameObject);
    }
}
