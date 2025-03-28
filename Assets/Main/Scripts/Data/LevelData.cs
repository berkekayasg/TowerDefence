using UnityEngine;
using System.Collections.Generic;

public enum TileType
{
    Path,
    TowerPlacement,
    Obstacle,
    Start, // Explicit start tile type
    End    // Explicit end tile type
}

[CreateAssetMenu(fileName = "LevelData", menuName = "TowerDefense/LevelData", order = 2)]
public class LevelData : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    // We'll represent the grid layout as a flat list, index = y * width + x
    public List<TileType> tileLayout;

    [Header("Pathfinding")]
    // Coordinates are usually sufficient, GridManager can find the actual tiles
    public Vector2Int startTileCoords;
    public Vector2Int endTileCoords;

    [Header("Gameplay Settings")]
    public int startingLives = 20;
    public int startingCurrency = 100;
    public List<WaveData> waveDataList;

    // Optional: Add fields for background art, level name, etc.
    // public Sprite backgroundSprite;
    // public string levelName;

    // Add a helper method to get tile type at specific coordinates
    public TileType GetTileType(int x, int y)
    {
        if (tileLayout == null || tileLayout.Count != gridWidth * gridHeight)
        {
            Debug.LogError($"LevelData '{name}': Tile layout is invalid or doesn't match grid dimensions.");
            return TileType.Obstacle; // Default to obstacle on error
        }
        int index = y * gridWidth + x;
        if (index < 0 || index >= tileLayout.Count)
        {
            Debug.LogError($"LevelData '{name}': Coordinates ({x},{y}) are out of bounds.");
            return TileType.Obstacle; // Default to obstacle on error
        }
        return tileLayout[index];
    }

    // Optional: Add validation logic in OnValidate
    private void OnValidate()
    {
        // Ensure tileLayout list has the correct size
        if (tileLayout == null)
        {
            tileLayout = new List<TileType>();
        }

        int expectedSize = gridWidth * gridHeight;
        if (tileLayout.Count != expectedSize)
        {
            // Resize the list, preserving existing elements if possible
            List<TileType> resizedLayout = new List<TileType>(expectedSize);
            for (int i = 0; i < expectedSize; i++)
            {
                if (i < tileLayout.Count)
                {
                    resizedLayout.Add(tileLayout[i]);
                }
                else
                {
                    // Default new tiles to TowerPlacement or Obstacle
                    resizedLayout.Add(TileType.TowerPlacement);
                }
            }
            tileLayout = resizedLayout;
            Debug.LogWarning($"LevelData '{name}': Resized tileLayout to match grid dimensions ({gridWidth}x{gridHeight}). Please review the layout in the Inspector.");
        }

        // Basic validation for start/end coords (ensure they are within bounds)
        if (startTileCoords.x < 0 || startTileCoords.x >= gridWidth || startTileCoords.y < 0 || startTileCoords.y >= gridHeight)
        {
            Debug.LogError($"LevelData '{name}': Start coordinates {startTileCoords} are out of bounds.");
        }
        if (endTileCoords.x < 0 || endTileCoords.x >= gridWidth || endTileCoords.y < 0 || endTileCoords.y >= gridHeight)
        {
            Debug.LogError($"LevelData '{name}': End coordinates {endTileCoords} are out of bounds.");
        }
    }
}
