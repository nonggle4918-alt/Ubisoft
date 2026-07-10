using UnityEngine;

public class GridCell : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public Piece CurrentPiece { get; private set; }

    public bool IsEmpty => CurrentPiece == null;

    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void SetPiece(Piece piece)
    {
        CurrentPiece = piece;
    }

    public void RemovePiece()
    {
        CurrentPiece = null;
    }
}