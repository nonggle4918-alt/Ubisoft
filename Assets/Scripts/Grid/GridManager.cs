using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;

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
                    new Vector3(x, y, 0),
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
}