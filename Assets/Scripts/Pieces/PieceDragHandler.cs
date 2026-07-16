using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Piece))]
public class PieceDragHandler : MonoBehaviour
{
    public static event Action<Piece> OnAllyPieceSelected;
    public static event Action OnAllyPieceDeselected;

    [SerializeField] private float holdThreshold = 0.25f;

    private Piece piece;
    private GridManager gridManager;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;

    private bool isHolding;
    private bool isDragging;
    private float holdStartTime;
    private Vector3 originalPos;
    private GridCell originalCell;
    private int originalSortingOrder;

    private GameObject ghost;
    private float currentRangeRadius;

    private static GameObject rangeIndicator;
    private static LineRenderer rangeLine;
    private static bool rangeInited;

    private void Awake()
    {
        piece = GetComponent<Piece>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == gameObject)
            {
                float range = piece.Data != null ? piece.Data.attackRange : 0;
                ShowRange(range);
                if (piece.Team == Team.Ally && !piece.IsDead)
                    OnAllyPieceSelected?.Invoke(piece);
                BeginHold();
            }
            else if (hit == null || hit.gameObject.GetComponent<Piece>()?.Team != Team.Ally)
            {
                HideRange();
                OnAllyPieceDeselected?.Invoke();
            }
        }

        if (isHolding)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                if (Time.time - holdStartTime >= holdThreshold)
                {
                    isHolding = false;
                    BeginDrag();
                }
            }
            else
            {
                CancelHold();
            }
        }

        if (isDragging)
        {
            if (Mouse.current.leftButton.isPressed)
                UpdateDrag();
            if (Mouse.current.leftButton.wasReleasedThisFrame)
                EndDrag();
        }
    }

    private void BeginHold()
    {
        if (piece.Team != Team.Ally || piece.IsDead) return;

        isHolding = true;
        holdStartTime = Time.time;
        originalPos = transform.position;
        originalCell = piece.CurrentCell;
        if (spriteRenderer != null) originalSortingOrder = spriteRenderer.sortingOrder;

        if (originalCell != null)
        {
            originalCell.RemovePiece();
            piece.CurrentCell = null;
        }
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 100;
    }

    private void CancelHold()
    {
        isHolding = false;
        if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder;
        if (originalCell != null)
        {
            originalCell.SetPiece(piece);
            piece.CurrentCell = originalCell;
        }
    }

    private void BeginDrag()
    {
        isDragging = true;
        CreateGhost();
    }

    private void UpdateDrag()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = -0.5f;
        ghost.transform.position = mousePos;

        if (rangeIndicator != null && rangeIndicator.activeSelf)
            UpdateRangePosition(new Vector3(mousePos.x, mousePos.y, -0.1f));
    }

    private void EndDrag()
    {
        isDragging = false;

        if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder;

        Vector3 dropPos = ghost.transform.position;
        Destroy(ghost);
        ghost = null;

        GridCell targetCell = gridManager.GetNearestCell(dropPos);

        if (targetCell == null || targetCell == originalCell || !IsDropOnGrid(dropPos))
        {
            transform.position = originalPos;
            if (originalCell != null)
            {
                originalCell.SetPiece(piece);
                piece.CurrentCell = originalCell;
            }
            return;
        }

        Piece existingPiece = targetCell.CurrentPiece;
        if (existingPiece != null && existingPiece != piece)
        {
            existingPiece.CurrentCell = originalCell;
            if (originalCell != null)
            {
                originalCell.SetPiece(existingPiece);
                existingPiece.transform.position = new Vector3(originalCell.transform.position.x, originalCell.transform.position.y, 0);
            }
        }
        else if (originalCell != null)
        {
            originalCell.RemovePiece();
        }

        transform.position = new Vector3(targetCell.transform.position.x, targetCell.transform.position.y, 0);
        targetCell.SetPiece(piece);
        piece.CurrentCell = targetCell;
    }

    private bool IsDropOnGrid(Vector3 pos)
    {
        return pos.x >= -0.5f && pos.x <= gridManager.Width - 0.5f
            && pos.y >= -0.5f && pos.y <= gridManager.Height - 0.5f;
    }

    private void CreateGhost()
    {
        ghost = new GameObject("PieceGhost");
        var sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.color = new Color(1, 1, 1, 0.5f);
        sr.sortingOrder = spriteRenderer.sortingOrder + 1;

        Vector3 m = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        m.z = -0.5f;
        ghost.transform.position = m;
    }

    private void ShowRange(float radius)
    {
        if (!rangeInited)
            InitRangeIndicator();

        if (radius <= 0)
        {
            rangeIndicator.SetActive(false);
            return;
        }

        currentRangeRadius = radius;
        UpdateRangePosition(new Vector3(transform.position.x, transform.position.y, -0.1f));
        rangeIndicator.SetActive(true);
    }

    private void UpdateRangePosition(Vector3 center)
    {
        if (rangeLine == null) return;
        int segments = 32;
        rangeLine.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            float x = center.x + Mathf.Cos(angle) * currentRangeRadius;
            float y = center.y + Mathf.Sin(angle) * currentRangeRadius;
            rangeLine.SetPosition(i, new Vector3(x, y, center.z));
        }
    }

    private static void HideRange()
    {
        if (rangeIndicator != null)
            rangeIndicator.SetActive(false);
    }

    private static void InitRangeIndicator()
    {
        rangeInited = true;
        rangeIndicator = new GameObject("RangeIndicator");
        rangeIndicator.SetActive(false);
        rangeLine = rangeIndicator.AddComponent<LineRenderer>();
        rangeLine.useWorldSpace = true;
        rangeLine.loop = true;
        rangeLine.startWidth = 0.08f;
        rangeLine.endWidth = 0.08f;
        rangeLine.startColor = new Color(0, 0.6f, 1, 0.7f);
        rangeLine.endColor = new Color(0, 0.6f, 1, 0.7f);
        rangeLine.material = new Material(Shader.Find("Sprites/Default"));
        rangeLine.sortingOrder = 200;
    }
}
