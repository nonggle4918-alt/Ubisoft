using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PieceManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Piece piecePrefab;
    [SerializeField] private int pullCost = 50;
    [SerializeField] private List<PieceData> allyPiecePool = new List<PieceData>();

    public event Action<PieceData> OnPiecePulled;

    private List<PieceData> gachaPool = new List<PieceData>();
    private int totalWeight;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            PullPiece();
    }

    private void Start()
    {
        ApplyCharacterDatabase();

        foreach (var pd in allyPiecePool)
        {
            if (pd != null && pd.team == Team.Ally && pd.gachaWeight > 0)
            {
                gachaPool.Add(pd);
                totalWeight += pd.gachaWeight;
            }
        }
    }

    public void PullPiece()
    {
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.SpendGold(pullCost))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }

        PieceData selected = WeightedRandom();
        if (selected == null)
        {
            GameManager.Instance.AddGold(pullCost);
            return;
        }

        GridCell cell = gridManager.GetEmptyCell();
        if (cell == null)
        {
            Debug.Log("빈 칸이 없습니다.");
            GameManager.Instance.AddGold(pullCost);
            return;
        }

        Piece piece = Instantiate(piecePrefab, cell.transform.position, Quaternion.identity);
        piece.SetData(selected);
        piece.CurrentCell = cell;
        cell.SetPiece(piece);

        OnPiecePulled?.Invoke(selected);
    }

    private void ApplyCharacterDatabase()
    {
        GameDatabase database = GameManager.Instance?.Database;
        if (database == null) return;

        foreach (PieceData pieceData in allyPiecePool)
        {
            if (pieceData == null || pieceData.team != Team.Ally) continue;

            CharacterRecord record = database.Characters.rows.Find(row =>
                string.Equals(row.name, pieceData.pieceName, System.StringComparison.OrdinalIgnoreCase));
            if (record == null) continue;

            pieceData.attackDamage = record.attackDamage;
            pieceData.attackRange = record.attackRange;
            pieceData.attackCooldown = record.attackCooldown;
            pieceData.cost = record.cost;

            Sprite sprite = database.GetSprite(record.imageResourceId);
            if (sprite != null)
                pieceData.sprite = sprite;
        }
    }

    private PieceData WeightedRandom()
    {
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var pd in gachaPool)
        {
            cumulative += pd.gachaWeight;
            if (roll < cumulative)
                return pd;
        }
        return gachaPool.Count > 0 ? gachaPool[0] : null;
    }
}
