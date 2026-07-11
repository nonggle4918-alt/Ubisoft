using UnityEngine;

public class PieceManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Piece piecePrefab;
    [SerializeField] private PieceData allyPieceData;

    public PieceData GetCurrentPieceData() => allyPieceData;

    public void SpawnPiece()
    {
        if (GameManager.Instance == null) return;
        if (allyPieceData == null)
        {
            Debug.Log("PieceData가 할당되지 않았습니다.");
            return;
        }

        if (!GameManager.Instance.SpendGold(allyPieceData.cost))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }

        GridCell cell = gridManager.GetEmptyCell();

        if (cell == null)
        {
            Debug.Log("빈 칸이 없습니다.");
            GameManager.Instance.AddGold(allyPieceData.cost);
            return;
        }

        Piece piece = Instantiate(
            piecePrefab,
            cell.transform.position,
            Quaternion.identity);

        piece.SetData(allyPieceData);
        cell.SetPiece(piece);
    }
}
