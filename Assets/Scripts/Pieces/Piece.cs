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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    private void Start()
    {
        ApplyData();
        RegisterToCombat();
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

        float spriteHeight = spriteRenderer != null && spriteRenderer.sprite != null
            ? spriteRenderer.sprite.bounds.size.y
            : 1f;
        float normalized = spriteHeight > 0.0001f ? data.visualScale / spriteHeight : data.visualScale;
        transform.localScale = baseScale * normalized;
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
        if (UpgradeManager.TryGetType(data.pieceName, out PieceUpgradeType type))
            return UpgradeManager.Instance.GetAtkMultiplier(type);
        return 1f;
    }

    private float GetUpgradeCoolMultiplier()
    {
        if (UpgradeManager.Instance == null || data == null) return 1f;
        if (UpgradeManager.TryGetType(data.pieceName, out PieceUpgradeType type))
            return UpgradeManager.Instance.GetCoolMultiplier(type);
        return 1f;
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
