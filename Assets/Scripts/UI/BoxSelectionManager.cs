using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Rubber-band selection for bulk selling: dragging from an empty tile draws a box and
// selects every ally piece inside it. PieceDragHandler (one instance per piece) only acts
// when a click lands on its own Piece, so this only starts a drag when the click hits
// nothing (Physics2D.OverlapPoint returns null) — the two never compete for the same click.
public class BoxSelectionManager : MonoBehaviour
{
    public static event Action<List<Piece>> OnBoxSelectionChanged;

    [SerializeField] private float dragThreshold = 6f;

    private Camera mainCamera;
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private RectTransform boxRect;
    private Image boxImage;

    private bool isDragging;
    private Vector2 dragStartScreenPos;
    private readonly List<Piece> selectedPieces = new List<Piece>();
    private readonly List<SpriteRenderer> highlighted = new List<SpriteRenderer>();

    private static readonly Color HighlightColor = new Color(1f, 0.95f, 0.55f, 1f);
    private static readonly Color BoxColor = new Color(0.3f, 0.8f, 1f, 0.25f);

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

            if (hit == null)
            {
                isDragging = true;
                dragStartScreenPos = mouseScreenPos;
                ShowBox(mouseScreenPos, mouseScreenPos);
            }
            else if (selectedPieces.Count > 0)
            {
                // Clicking any piece leaves multi-select mode, so stale highlights don't
                // linger while the single-select status panel takes over.
                ClearSelection();
            }
        }

        if (isDragging)
        {
            Vector2 currentScreenPos = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.isPressed)
            {
                ShowBox(dragStartScreenPos, currentScreenPos);
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
                HideBox();

                if (Vector2.Distance(dragStartScreenPos, currentScreenPos) < dragThreshold)
                    ClearSelection();
                else
                    CommitSelection(dragStartScreenPos, currentScreenPos);
            }
        }
    }

    private void CommitSelection(Vector2 startScreenPos, Vector2 endScreenPos)
    {
        Rect screenRect = Rect.MinMaxRect(
            Mathf.Min(startScreenPos.x, endScreenPos.x), Mathf.Min(startScreenPos.y, endScreenPos.y),
            Mathf.Max(startScreenPos.x, endScreenPos.x), Mathf.Max(startScreenPos.y, endScreenPos.y));

        ClearHighlights();
        selectedPieces.Clear();

        foreach (Piece piece in FindObjectsByType<Piece>(FindObjectsSortMode.None))
        {
            if (piece == null || piece.Team != Team.Ally || piece.IsDead) continue;

            Vector2 screenPos = mainCamera.WorldToScreenPoint(piece.transform.position);
            if (screenRect.Contains(screenPos))
            {
                selectedPieces.Add(piece);
                Highlight(piece);
            }
        }

        OnBoxSelectionChanged?.Invoke(new List<Piece>(selectedPieces));
    }

    private void ClearSelection()
    {
        if (selectedPieces.Count == 0) return;

        ClearHighlights();
        selectedPieces.Clear();
        OnBoxSelectionChanged?.Invoke(new List<Piece>(selectedPieces));
    }

    private void Highlight(Piece piece)
    {
        SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        renderer.color = HighlightColor;
        highlighted.Add(renderer);
    }

    private void ClearHighlights()
    {
        foreach (SpriteRenderer renderer in highlighted)
        {
            if (renderer != null)
                renderer.color = Color.white;
        }
        highlighted.Clear();
    }

    private void EnsureBoxVisual()
    {
        if (boxRect != null) return;

        rootCanvas = FindFirstObjectByType<Canvas>();
        if (rootCanvas == null) return;
        canvasRect = rootCanvas.transform as RectTransform;

        var boxObject = new GameObject("SelectionBox", typeof(RectTransform), typeof(Image));
        boxObject.transform.SetParent(rootCanvas.transform, false);

        boxRect = boxObject.GetComponent<RectTransform>();
        // Pivot/anchor matches the canvas root's own pivot so a local point returned by
        // ScreenPointToLocalPointInRectangle(canvasRect, ...) can be used directly below.
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);

        boxImage = boxObject.GetComponent<Image>();
        boxImage.color = BoxColor;
        boxImage.raycastTarget = false;

        boxObject.SetActive(false);
    }

    private void ShowBox(Vector2 startScreenPos, Vector2 currentScreenPos)
    {
        EnsureBoxVisual();
        if (boxRect == null || canvasRect == null) return;

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPos, uiCamera, out Vector2 startLocal)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, currentScreenPos, uiCamera, out Vector2 currentLocal)) return;

        boxRect.gameObject.SetActive(true);

        Vector2 min = Vector2.Min(startLocal, currentLocal);
        Vector2 max = Vector2.Max(startLocal, currentLocal);
        boxRect.anchoredPosition = (min + max) / 2f;
        boxRect.sizeDelta = max - min;
    }

    private void HideBox()
    {
        if (boxRect != null)
            boxRect.gameObject.SetActive(false);
    }
}
