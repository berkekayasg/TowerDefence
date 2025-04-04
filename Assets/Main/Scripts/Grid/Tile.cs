using UnityEngine;
using UnityEngine.EventSystems; // Required for UI interaction checks

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool isPath = false;
    public bool isObstacle = false; // Added flag for obstacles
    public bool isTower => BuildManager.Instance != null && BuildManager.Instance.IsTileOccupied(this);

    // A* Pathfinding properties
    public int fCost => gCost + hCost;
    public int hCost;
    public int gCost;
    public Tile parent;

    [Header("Visuals")]
    [SerializeField] private Color basePathColor = Color.gray;
    [SerializeField] private Color baseTowerColor = Color.green;
    [SerializeField] private Color baseObstacleColor = Color.black * 0.8f; // Added color for obstacles
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color hoverColor = Color.yellow;

    private Renderer tileRenderer;
    private Color originalColor;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer == null)
        {
            Debug.LogError($"Tile {gameObject.name} is missing a Renderer component!");
            return;
        }

        if (isPath)
        {
            tileRenderer.material.color = basePathColor;
        }
        else if (isObstacle)
        {
            tileRenderer.material.color = baseObstacleColor;
        }
        else // TowerPlacement tile
        {
            tileRenderer.material.color = isTower ? occupiedColor : baseTowerColor;
        }
        originalColor = tileRenderer.material.color;
    }

    void OnMouseEnter()
    {
        if (IsBuildable() && GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Build)
        {
            if (tileRenderer != null)
            {
                tileRenderer.material.color = hoverColor;
            }
            if (BuildManager.Instance != null)
            {
                BuildManager.Instance.ShowBuildPreview(this);
            }
            // Play hover sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayEffect("TileHover");
            }
        }
    }

    void OnMouseExit()
    {
        if (tileRenderer != null)
        {
            // Restore original color based on tile type and state
            if (isPath)
            {
                tileRenderer.material.color = originalColor;
            }
            else if (isObstacle)
            {
                 tileRenderer.material.color = originalColor;
            }
            else // TowerPlacement tile
            {
                tileRenderer.material.color = IsBuildable() ? originalColor : occupiedColor;
            }
        }
        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.HideBuildPreview();
        }
    }

    void OnMouseDown()
    {
        // Check if the pointer is over a UI element
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // If it is, ignore the click for game world interaction
            return;
        }

        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.SelectTile(this);
        }
        else
        {
            Debug.LogError("BuildManager instance not found!");
        }
    }
     // Updated to include obstacle check
     public bool IsBuildable()
     {
         return !isPath && !isTower && !isObstacle;
     }

     public Vector3 GetBuildPosition()
     {
         return transform.position + Vector3.up * 0.2f;
     }
}
