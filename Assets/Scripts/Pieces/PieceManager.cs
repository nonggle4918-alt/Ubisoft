using UnityEngine;

public class PieceManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Piece piecePrefab;

    public void SpawnPiece()
    {
        GridCell cell = gridManager.GetEmptyCell();

        if (cell == null)
        {
            Debug.Log("빈 칸이 없습니다.");
            return;
        }

        Piece piece = Instantiate(
            piecePrefab,
            cell.transform.position,
            Quaternion.identity);

        cell.SetPiece(piece);
    }
}