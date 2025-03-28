using UnityEngine;
using System.Collections.Generic;

public static class Pathfinding // Changed to static class
{
    // Removed MonoBehaviour inheritance

    public static List<Tile> FindPath(GridManager gridManager, Tile startTile, Tile endTile)
    {
        // A* Pathfinding algorithm implementation
        List<Tile> openSet = new List<Tile>();
        HashSet<Tile> closedSet = new HashSet<Tile>();

        openSet.Add(startTile);

        while (openSet.Count > 0)
        {
            Tile current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost || openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost)
                {
                    current = openSet[i];
                }
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == endTile)
            {
                return RetracePath(startTile, current);
            }

            foreach (Tile neighbor in GetNeighbors(gridManager, current))
            {
                if (neighbor == null || !neighbor.isPath || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newCostToNeighbor = current.gCost + GetDistance(current, neighbor);
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, endTile);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // No path found
    }

    private static List<Tile> RetracePath(Tile startTile, Tile endTile)
    {
        List<Tile> path = new List<Tile>();
        Tile current = endTile;

        while (current != startTile)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private static int GetDistance(Tile tileA, Tile tileB)
    {
        int distanceX = Mathf.Abs(tileA.gridPosition.x - tileB.gridPosition.x);
        int distanceY = Mathf.Abs(tileA.gridPosition.y - tileB.gridPosition.y);

        return distanceX + distanceY;
    }

    private static List<Tile> GetNeighbors(GridManager gridManager, Tile tile)
    {
        List<Tile> neighbors = new List<Tile>();

        int x = tile.gridPosition.x;
        int y = tile.gridPosition.y;

        // Check all possible neighbors (up, down, left, right)
        AddNeighbor(gridManager, neighbors, x - 1, y);
        AddNeighbor(gridManager, neighbors, x + 1, y);
        AddNeighbor(gridManager, neighbors, x, y - 1);
        AddNeighbor(gridManager, neighbors, x, y + 1);

        return neighbors;
    }

    private static void AddNeighbor(GridManager gridManager, List<Tile> neighbors, int x, int y)
    {
        // Use the public properties GridWidth and GridHeight
        if (x >= 0 && x < gridManager.GridWidth && y >= 0 && y < gridManager.GridHeight)
        {
            neighbors.Add(gridManager.GetTile(x, y));
        }
    }
}
