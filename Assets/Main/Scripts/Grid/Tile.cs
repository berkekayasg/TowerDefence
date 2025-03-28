using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool isPath = false;
    public bool isTower = false; // Tracks if a tower is currently placed here

    // A* Pathfinding properties
    public int fCost => gCost + hCost;
    public int hCost;
    public int gCost;
    public Tile parent;

    [Header("Visuals")]
    [SerializeField] private Color basePathColor = Color.gray;
    [SerializeField] private Color baseTowerColor = Color.green;
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
        else
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
        }
    }

    void OnMouseExit()
    {
        if (tileRenderer != null)
        {
            // Restore original color based on tile type
            if (isPath)
            {
                tileRenderer.material.color = originalColor; // originalColor is basePathColor for path tiles
            }
            else
            {
                // For non-path tiles, use buildable status to determine color
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
        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.SelectTile(this);
        }
        else
        {
            Debug.LogError("BuildManager instance not found!");
        }
    }

     public bool IsBuildable()
     {
         // Consider adding a check against BuildManager's preview location if needed
         return !isPath && !isTower;
     }

     public Vector3 GetBuildPosition()
     {
         return transform.position + Vector3.up * 0.2f;
     }
}
