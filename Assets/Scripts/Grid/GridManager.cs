using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    public int Width => width;
    public int Height => height;

    [SerializeField] private GameObject whiteTilePrefab;
    [SerializeField] private GameObject greenTilePrefab;

    private GridCell[,] grid;

    private void Awake()
    {
        grid = new GridCell[width, height];
    }

    private void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        float offsetX = (8 - width) * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject prefab =
                    ((x + y) % 2 == 0)
                    ? whiteTilePrefab
                    : greenTilePrefab;

                GameObject obj = Instantiate(
                    prefab,
                    new Vector3(x + offsetX, y, 0),
                    Quaternion.identity,
                    transform);

                obj.name = $"Tile ({x},{y})";

                GridCell cell = obj.GetComponent<GridCell>();

                cell.Initialize(x, y);

                grid[x, y] = cell;
            }
        }
    }

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;

        return grid[x, y];
    }

    public GridCell GetEmptyCell()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y].IsEmpty)
                    return grid[x, y];
            }
        }

        return null;
    }

    public GridCell GetNearestEmptyCell(Vector3 worldPos)
    {
        int cx = Mathf.Clamp(Mathf.RoundToInt(worldPos.x), 0, width - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(worldPos.y), 0, height - 1);

        if (grid[cx, cy].IsEmpty)
            return grid[cx, cy];

        GridCell nearest = null;
        float nearestDist = float.MaxValue;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!grid[x, y].IsEmpty) continue;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = grid[x, y];
                }
            }
        }
        return nearest;
    }

    public void ClearGrid()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y].RemovePiece();
    }
}