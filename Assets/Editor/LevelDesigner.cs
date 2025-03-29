using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LevelDesigner : EditorWindow
{
    private LevelData targetLevelData;
    private int gridWidth = 15;
    private int gridHeight = 10;
    private int pathPercentage = 25; // Default path density percentage
    private int maxPathAttempts = 10; // Max attempts for pathfinding
    private Vector2 scrollPosition;
    private const float TILE_PREVIEW_SIZE = 20f; // Size of each tile in the preview

    // Define colors for preview
    private readonly Color pathColor = Color.grey;
    private readonly Color startColor = Color.green;
    private readonly Color endColor = Color.red;
    private readonly Color towerPlacementColor = Color.white;
    private readonly Color gridLineColor = Color.black * 0.5f; // Dim grid lines

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
        pathPercentage = EditorGUILayout.IntSlider("Path Density (%)", pathPercentage, 10, 50); // Min 5%, Max 80%
        maxPathAttempts = EditorGUILayout.IntSlider("Max Path Attempts", maxPathAttempts, 1, 50);


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

        GUILayout.Space(10);
        GUILayout.Label("Level Preview", EditorStyles.boldLabel);

        // Calculate required scroll view size
        float previewWidth = gridWidth * TILE_PREVIEW_SIZE + 20; // Add padding
        float previewHeight = gridHeight * TILE_PREVIEW_SIZE + 20;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(previewWidth + 20), GUILayout.Height(previewHeight + 20)); // Add scrollbar padding
        // Reserve space for the preview drawing area
        Rect previewArea = GUILayoutUtility.GetRect(previewWidth, previewHeight);
        DrawPreview(previewArea);
        EditorGUILayout.EndScrollView();
    }

    private void GenerateLevelLayout()
    {
        // Initialize layout with TowerPlacement
        List<TileType> layout = new List<TileType>(gridWidth * gridHeight);
        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            layout.Add(TileType.TowerPlacement);
        }

        // --- Path Generation: Rule-Constrained Walk with Backtracking ---
        Vector2Int startCoords = new Vector2Int(0, Random.Range(0, gridHeight));
        Vector2Int endCoords = Vector2Int.zero;

        Stack<Vector2Int> pathStack = new Stack<Vector2Int>();
        HashSet<Vector2Int> pathSet = new HashSet<Vector2Int>();
        List<Vector2Int> finalPathCoords = new List<Vector2Int>(); // Stores the final successful path

        pathStack.Push(startCoords);
        pathSet.Add(startCoords);

        int totalTiles = gridWidth * gridHeight;
        int targetPathTiles = Mathf.FloorToInt(totalTiles * (pathPercentage / 100.0f));
        targetPathTiles = Mathf.Max(targetPathTiles, gridWidth); // Min length is grid width

        int maxAttempts = totalTiles * 5; // Safety break
        int attempts = 0;
        bool pathComplete = false;
        // Key: Current Node, Value: List of neighbors tried FROM this node
        Dictionary<Vector2Int, List<Vector2Int>> triedMoves = new Dictionary<Vector2Int, List<Vector2Int>>();


        for (int i = 0; i < maxPathAttempts; i++)
        {
            //reset for each attempt
            pathStack.Clear();
            pathSet.Clear();
            pathStack.Push(startCoords);
            pathSet.Add(startCoords);
            endCoords = Vector2Int.zero; // Reset endCoords for each attempt
            attempts = 0;
            pathComplete = false;
            triedMoves.Clear(); // Reset tried moves for each attempt


            while (pathStack.Count > 0 && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int current = pathStack.Peek();

                // Check for completion
                if (current.x == gridWidth - 1 && pathSet.Count >= targetPathTiles)
                {
                    endCoords = current;
                    pathComplete = true;
                    break;
                }

                // Find valid, untried moves adhering to rules
                List<Vector2Int> validMoves = FindValidUntriedMoves(current, pathSet, triedMoves);

                if (validMoves.Count > 0)
                {
                    // Choose next move based on density target
                    Vector2Int nextMove;
                    bool targetMet = pathSet.Count >= targetPathTiles;

                    Vector2Int rightMove = current + Vector2Int.right;
                    List<Vector2Int> verticalMoves = new List<Vector2Int>();
                    if (validMoves.Contains(current + Vector2Int.up)) verticalMoves.Add(current + Vector2Int.up);
                    if (validMoves.Contains(current + Vector2Int.down)) verticalMoves.Add(current + Vector2Int.down);
                    bool canMoveRight = validMoves.Contains(rightMove);

                    // Prioritize vertical if target not met, otherwise prioritize right
                    if (!targetMet && verticalMoves.Count > 0)
                    {
                        // Small chance to go right anyway to avoid getting stuck vertically
                        if (canMoveRight && Random.value < 0.15f) {
                            nextMove = rightMove;
                        } else {
                            nextMove = verticalMoves[Random.Range(0, verticalMoves.Count)];
                        }
                    }
                    else if (canMoveRight)
                    {
                        nextMove = rightMove;
                    }
                    else if (verticalMoves.Count > 0) // Only vertical possible
                    {
                        nextMove = verticalMoves[Random.Range(0, verticalMoves.Count)];
                    }
                    else // Should technically not be reached if validMoves.Count > 0
                    {
                        nextMove = validMoves[Random.Range(0, validMoves.Count)];
                    }

                    // Record the try and advance
                    if (!triedMoves.ContainsKey(current)) triedMoves[current] = new List<Vector2Int>();
                    triedMoves[current].Add(nextMove);

                    pathStack.Push(nextMove);
                    pathSet.Add(nextMove);
                }
                else
                {
                    // Backtrack
                    Vector2Int backtrackNode = pathStack.Pop();
                    // Don't remove from pathSet here, let the overwrite handle it if needed
                    triedMoves.Remove(backtrackNode); // Clear tried moves for the node we are leaving

                    // Add the backtrackNode to the tried list of the *new* current top node
                    if (pathStack.Count > 0)
                    {
                        Vector2Int newCurrent = pathStack.Peek();
                        if (!triedMoves.ContainsKey(newCurrent)) triedMoves[newCurrent] = new List<Vector2Int>();
                        if (!triedMoves[newCurrent].Contains(backtrackNode)) // Avoid duplicates if complex backtrack happens
                        {
                            triedMoves[newCurrent].Add(backtrackNode);
                        }
                    }
                }
            }
            if (pathComplete) break; // Exit outer loop if path is complete
        }

        if (!pathComplete)
        {
             Debug.LogError($"Path generation failed after {attempts} attempts. Could not reach end satisfying density. Try adjusting parameters.");
             EditorUtility.DisplayDialog("Error", "Failed to generate path. Try different density or grid size.", "OK");
             return;
        }

        // Store the successful path
        finalPathCoords = new List<Vector2Int>(pathStack);
        finalPathCoords.Reverse();

        // --- Update LevelData ---
        Undo.RecordObject(targetLevelData, "Generate Random Level Layout"); // Allow undo

        targetLevelData.gridWidth = gridWidth;
        targetLevelData.gridHeight = gridHeight;
        targetLevelData.startTileCoords = startCoords;
        targetLevelData.endTileCoords = endCoords;

        // Apply final pathSet to layout
        HashSet<Vector2Int> finalSet = new HashSet<Vector2Int>(finalPathCoords); // Use the actual path found
        for (int i = 0; i < layout.Count; i++)
        {
            int x = i % gridWidth;
            int y = i / gridWidth;
            Vector2Int coord = new Vector2Int(x, y);
            if (finalSet.Contains(coord)) // Check against the final path
            {
                layout[i] = TileType.Path;
            }
            else
            {
                layout[i] = TileType.TowerPlacement;
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

        Debug.Log($"Generated random level layout for {targetLevelData.name}. Target Path Tiles: >= {targetPathTiles}, Actual Path Tiles: {finalSet.Count}");
        EditorUtility.DisplayDialog("Success", $"Generated random level layout for {targetLevelData.name}.", "OK");
    }

     // Helper to find valid moves from 'current', respecting rules and tried moves
    private List<Vector2Int> FindValidUntriedMoves(Vector2Int current, HashSet<Vector2Int> pathSet, Dictionary<Vector2Int, List<Vector2Int>> triedMoves)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.right }; // Order matters slightly for checks

        List<Vector2Int> tried = triedMoves.ContainsKey(current) ? triedMoves[current] : new List<Vector2Int>();

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = current + dir;

            // Basic checks: Bounds, not already path, not already tried from current
            if (!IsValid(neighbor) || pathSet.Contains(neighbor) || tried.Contains(neighbor))
            {
                continue;
            }

            // Rule Check 1: Does this move connect to >1 existing path tiles (excluding 'current')?
            int existingPathNeighbors = 0;
            foreach (Vector2Int checkDir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int adjacentToNeighbor = neighbor + checkDir;
                if (adjacentToNeighbor != current && pathSet.Contains(adjacentToNeighbor))
                {
                    existingPathNeighbors++;
                }
            }
            if (existingPathNeighbors > 0)
            {
                continue; // Invalid move, would create branch/join
            }

            // Rule Check 2: Does this move create a 2x2 block? (Simplified check)
            if (Creates2x2Block(current, neighbor, pathSet))
            {
                 continue; // Invalid move, would create block
            }


            // If all checks pass, it's a valid move
            validMoves.Add(neighbor);
        }
        return validMoves;
    }

    // Simplified check for 2x2 block formation
    private bool Creates2x2Block(Vector2Int from, Vector2Int to, HashSet<Vector2Int> pathSet)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        // Check the two other tiles that would form the square
        Vector2Int corner1 = new Vector2Int(from.x + dx + (dx == 0 ? 1 : 0), from.y + dy + (dy == 0 ? 1 : 0)); // Diagonally opposite 'from'
        Vector2Int corner2 = new Vector2Int(from.x + (dx == 0 ? 1 : 0), from.y + (dy == 0 ? 1 : 0)); // Adjacent to both 'from' and 'to'

        Vector2Int corner3 = new Vector2Int(from.x + dx + (dx == 0 ? -1 : 0), from.y + dy + (dy == 0 ? -1 : 0)); // Diagonally opposite 'from' other side
        Vector2Int corner4 = new Vector2Int(from.x + (dx == 0 ? -1 : 0), from.y + (dy == 0 ? -1 : 0)); // Adjacent to both 'from' and 'to' other side


        // Check one potential square
        if (pathSet.Contains(corner1) && pathSet.Contains(corner2)) return true;
        // Check the other potential square
        if (pathSet.Contains(corner3) && pathSet.Contains(corner4)) return true;


        return false;
    }


    // Helper to check if coordinates are within grid bounds
    private bool IsValid(Vector2Int coords)
    {
        return coords.x >= 0 && coords.x < gridWidth && coords.y >= 0 && coords.y < gridHeight;
    }

    // Draw the level preview in the specified area
    private void DrawPreview(Rect area)
    {
        if (targetLevelData == null || targetLevelData.tileLayout == null || targetLevelData.tileLayout.Count != gridWidth * gridHeight)
        {
            EditorGUI.LabelField(area, "Assign LevelData and Generate to see preview.");
            return;
        }

        // Draw background for the grid area
        EditorGUI.DrawRect(area, new Color(0.1f, 0.1f, 0.1f)); // Dark background

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int index = y * gridWidth + x;
                TileType type = targetLevelData.tileLayout[index];
                Color tileColor;

                switch (type)
                {
                    case TileType.Path: tileColor = pathColor; break;
                    case TileType.Start: tileColor = startColor; break;
                    case TileType.End: tileColor = endColor; break;
                    case TileType.TowerPlacement:
                    default: tileColor = towerPlacementColor; break;
                }

                // Calculate tile position within the preview area
                // Invert Y for typical GUI coordinate system (top-left origin)
                Rect tileRect = new Rect(
                    area.x + x * TILE_PREVIEW_SIZE,
                    area.y + (gridHeight - 1 - y) * TILE_PREVIEW_SIZE, // Inverted Y
                    TILE_PREVIEW_SIZE,
                    TILE_PREVIEW_SIZE
                );

                // Draw the tile background
                EditorGUI.DrawRect(tileRect, tileColor);
                // Draw grid lines (optional, but helpful)
                EditorGUI.DrawRect(new Rect(tileRect.x, tileRect.y, tileRect.width, 1), gridLineColor); // Top line
                EditorGUI.DrawRect(new Rect(tileRect.x, tileRect.y, 1, tileRect.height), gridLineColor); // Left line
                if (x == gridWidth - 1) EditorGUI.DrawRect(new Rect(tileRect.xMax -1, tileRect.y, 1, tileRect.height), gridLineColor); // Right edge
                if (y == 0) EditorGUI.DrawRect(new Rect(tileRect.x, tileRect.yMax - 1, tileRect.width, 1), gridLineColor); // Bottom edge (inverted y)

            }
        }
    }
}
