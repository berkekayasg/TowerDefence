using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LevelDesigner : EditorWindow
{
    private LevelData targetLevelData;
    private int gridWidth = 15;
    private int gridHeight = 10;
    private Vector2 scrollPosition; // For potential future use if window gets crowded

    [MenuItem("Tools/Level Designer")]
    public static void ShowWindow()
    {
        GetWindow<LevelDesigner>("Level Designer");
    }

    void OnGUI()
    {
        GUILayout.Label("Level Generation Settings", EditorStyles.boldLabel);

        targetLevelData = (LevelData)EditorGUILayout.ObjectField("Target Level Data", targetLevelData, typeof(LevelData), false);
        gridWidth = EditorGUILayout.IntField("Grid Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Grid Height", gridHeight);

        // Ensure dimensions are positive
        gridWidth = Mathf.Max(5, gridWidth); // Minimum width 5
        gridHeight = Mathf.Max(5, gridHeight); // Minimum height 5

        if (GUILayout.Button("Generate Random Level"))
        {
            if (targetLevelData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a LevelData asset first.", "OK");
                return;
            }
            GenerateLevelLayout();
        }

        // Optional: Add a preview section later if needed
        // scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        // DrawPreview(); // Implement this method if needed
        // EditorGUILayout.EndScrollView();
    }

    private void GenerateLevelLayout()
    {
        // Initialize layout with TowerPlacement
        List<TileType> layout = new List<TileType>(gridWidth * gridHeight);
        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            layout.Add(TileType.TowerPlacement);
        }

        // --- Path Generation (Randomized DFS Approach) ---
        Vector2Int startCoords = new Vector2Int(0, Random.Range(0, gridHeight)); // Start on left edge
        Vector2Int endCoords = Vector2Int.zero; // Will be set when path reaches the end

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<Vector2Int> pathCoords = new List<Vector2Int>(); // Store path tiles

        stack.Push(startCoords);
        visited.Add(startCoords);

        bool pathComplete = false;
        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek(); // Peek instead of Pop initially
            pathCoords.Add(current); // Add current to path list

            // Check if we reached the end edge
            if (current.x == gridWidth - 1)
            {
                endCoords = current;
                pathComplete = true;
                break; // Path found
            }

            List<Vector2Int> neighbors = GetValidNeighbors(current, visited);

            if (neighbors.Count > 0)
            {
                // Choose a random neighbor
                Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                visited.Add(next);
                stack.Push(next);
            }
            else
            {
                // Backtrack if no valid neighbors
                stack.Pop();
                // Remove the dead-end tile from the final path list if backtracking
                if (pathCoords.Count > 0) pathCoords.RemoveAt(pathCoords.Count - 1);
            }
        }

        if (!pathComplete)
        {
             EditorUtility.DisplayDialog("Error", "Failed to generate a complete path. Try increasing grid size or adjusting parameters.", "OK");
             return; // Stop generation if path failed
        }


        // --- Update LevelData ---
        Undo.RecordObject(targetLevelData, "Generate Random Level Layout"); // Allow undo

        targetLevelData.gridWidth = gridWidth;
        targetLevelData.gridHeight = gridHeight;
        targetLevelData.startTileCoords = startCoords;
        targetLevelData.endTileCoords = endCoords;

        // Apply path to layout
        foreach (Vector2Int coord in pathCoords)
        {
            int index = coord.y * gridWidth + coord.x;
            if (index >= 0 && index < layout.Count)
            {
                 layout[index] = TileType.Path; // Mark path tiles
            }
        }

        // Ensure start and end are marked correctly (overwrites Path if necessary)
        int startIndex = startCoords.y * gridWidth + startCoords.x;
        if (startIndex >= 0 && startIndex < layout.Count) layout[startIndex] = TileType.Start;

        int endIndex = endCoords.y * gridWidth + endCoords.x;
         if (endIndex >= 0 && endIndex < layout.Count) layout[endIndex] = TileType.End;


        targetLevelData.tileLayout = layout;

        // Mark the asset as dirty so changes are saved
        EditorUtility.SetDirty(targetLevelData);
        AssetDatabase.SaveAssets(); // Explicitly save asset changes

        Debug.Log($"Generated random level layout for {targetLevelData.name}. Path length: {pathCoords.Count}");
        EditorUtility.DisplayDialog("Success", $"Generated random level layout for {targetLevelData.name}.", "OK");
    }

    // Helper for DFS: Gets valid, unvisited neighbors
    private List<Vector2Int> GetValidNeighbors(Vector2Int current, HashSet<Vector2Int> visited)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int[,] directions = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } }; // Right, Left, Up, Down

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            Vector2Int neighbor = new Vector2Int(current.x + directions[i, 0], current.y + directions[i, 1]);

            // Check bounds
            if (neighbor.x >= 0 && neighbor.x < gridWidth && neighbor.y >= 0 && neighbor.y < gridHeight)
            {
                // Check if not visited
                if (!visited.Contains(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
        }
        return neighbors;
    }

     // Optional: Implement DrawPreview if needed
    // private void DrawPreview() { ... }
}
