using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] private PieceData data;

    public PieceData Data => data;
    public int CurrentHP { get; private set; }
    public Team Team => data != null ? data.team : Team.Ally;
    public bool IsDead => CurrentHP <= 0;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ApplyData();
        RegisterToCombat();
    }

    private void OnDestroy()
    {
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
        CurrentHP = data.maxHP;
        if (spriteRenderer != null && data.sprite != null)
            spriteRenderer.sprite = data.sprite;
    }

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
