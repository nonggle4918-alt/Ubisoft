using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.6f;
    private const float RiseDistance = 0.8f;

    private static readonly Color PopupColor = new Color(1f, 0.92f, 0.35f);
    private static Material sharedOutlineMaterial;

    private TextMeshPro text;
    private Vector3 startPosition;
    private float elapsed;

    public static void Spawn(Vector3 position, float damage, int sortingLayerId, int sortingOrder)
    {
        Vector3 jitteredPosition = position + new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 0f, 0f);

        var go = new GameObject("Damage Popup");
        go.transform.position = jitteredPosition;

        var popup = go.AddComponent<DamagePopup>();
        popup.Setup(damage, sortingLayerId, sortingOrder);
    }

    private void Setup(float damage, int sortingLayerId, int sortingOrder)
    {
        startPosition = transform.position;

        text = gameObject.AddComponent<TextMeshPro>();
        text.text = Mathf.RoundToInt(damage).ToString();
        text.fontSize = 3.4f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = PopupColor;
        text.fontStyle = FontStyles.Bold;
        text.fontSharedMaterial = GetSharedOutlineMaterial(text);

        MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerID = sortingLayerId;
        meshRenderer.sortingOrder = sortingOrder;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Lifetime);
        float eased = 1f - (1f - t) * (1f - t);

        transform.position = startPosition + Vector3.up * (RiseDistance * eased);

        if (text != null)
        {
            Color color = text.color;
            color.a = 1f - t;
            text.color = color;
        }

        if (elapsed >= Lifetime)
            Destroy(gameObject);
    }

    private static Material GetSharedOutlineMaterial(TextMeshPro sampleText)
    {
        if (sharedOutlineMaterial == null)
        {
            sharedOutlineMaterial = new Material(sampleText.fontSharedMaterial);
            sharedOutlineMaterial.SetFloat("_OutlineWidth", 0.2f);
            sharedOutlineMaterial.SetColor("_OutlineColor", Color.black);
        }
        return sharedOutlineMaterial;
    }
}
