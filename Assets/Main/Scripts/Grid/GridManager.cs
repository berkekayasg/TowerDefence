using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Visuals")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private float tileSize = 1f;

    private Tile[,] grid;
    private int _gridWidth;
    private int _gridHeight;

    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    void Start()
    {
        // Get LevelData from LevelManager in Awake
        if (LevelManager.Instance == null)
        {
            Debug.LogError("GridManager (Awake): LevelManager instance not found! Cannot create grid.");
            enabled = false;
            return;
        }
        LevelData levelData = LevelManager.Instance.CurrentLevelData;
        if (levelData == null)
        {
             Debug.LogError("GridManager (Awake): LevelData not assigned in LevelManager! Cannot create grid.");
             enabled = false;
             return;
        }

        InitializeGrid(levelData);
    }

    void InitializeGrid(LevelData levelData)
    {
        if (tilePrefab == null)
        {
             Debug.LogError("GridManager: Tile Prefab not assigned!");
             enabled = false;
             return;
        }

        // Use dimensions from the passed LevelData
        _gridWidth = levelData.gridWidth;
        _gridHeight = levelData.gridHeight;
        grid = new Tile[_gridWidth, _gridHeight];

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                Vector3 position = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                Tile tile = tileObj.GetComponent<Tile>();
                tile.gridPosition = new Vector2Int(x, y);

                TileType type = levelData.GetTileType(x, y);
                tile.isPath = (type == TileType.Path || type == TileType.Start || type == TileType.End);
                tile.isObstacle = (type == TileType.Obstacle); // Set the obstacle flag

                grid[x, y] = tile;
            }
        }
    }

    public Vector3 GetGridCenter()
    {
        // Calculate center based on tile positions
        return new Vector3((_gridWidth - 1) * tileSize * 0.5f, 0, (_gridHeight - 1) * tileSize * 0.5f);
    }

    public Tile GetTile(int x, int y)
    {
        if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
        {
            return grid[x, y];
        }
        return null;
    }

    public Tile GetStartTile()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.CurrentLevelData == null)
        {
            Debug.LogError("GridManager (GetStartTile): LevelManager or LevelData not available.");
            return null;
        }
        LevelData levelData = LevelManager.Instance.CurrentLevelData;
        return GetTile(levelData.startTileCoords.x, levelData.startTileCoords.y);
    }

    public Tile GetEndTile()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.CurrentLevelData == null)
        {
             Debug.LogError("GridManager (GetEndTile): LevelManager or LevelData not available.");
             return null;
        }
        LevelData levelData = LevelManager.Instance.CurrentLevelData;
        return GetTile(levelData.endTileCoords.x, levelData.endTileCoords.y);
    }

    public float GetSize()
    {
        return (_gridWidth * tileSize > _gridHeight * tileSize) ? _gridWidth * tileSize : _gridHeight * tileSize;
    }
}
