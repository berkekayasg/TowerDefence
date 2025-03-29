using UnityEngine;
using System.Collections.Generic; // Added for Dictionary
using System.Linq; // Added for Linq

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("Setup")]
    public GameObject standardTowerPrefab; // Assign in Inspector

    private GameObject towerToBuild;
    private Tile selectedTile;
    private Tower selectedTower;
    private GameObject previewTowerInstance;

    // Dictionary to track placed towers and their tiles
    private Dictionary<Tile, Tower> placedTowers = new Dictionary<Tile, Tower>();

    private int ignoreRaycastLayer;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer == -1)
        {
             ignoreRaycastLayer = 2; // Default layer 2
             Debug.LogWarning("'Ignore Raycast' layer not found. Defaulting to layer 2.");
        }
    }

    void Update()
    {
        // Check for right-click to deselect
        if (Input.GetMouseButtonDown(1)) // 1 is the right mouse button
        {
            DeselectTile();
            towerToBuild = null; // Also clear the tower to build selection
            HideBuildPreview();
            if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus(""); // Clear build status message
        }
    }

    // --- Helper methods for dictionary ---
    public bool IsTileOccupied(Tile tile)
    {
        return placedTowers.ContainsKey(tile);
    }

    public Tower GetTowerOnTile(Tile tile)
    {
        placedTowers.TryGetValue(tile, out Tower tower);
        return tower;
    }

    public Tile GetTileForTower(Tower tower)
    {
        foreach (var kvp in placedTowers)
        {
            if (kvp.Value == tower)
            {
                return kvp.Key;
            }
        }
        return null;
    }
    // --- End Helper methods ---


    public GameObject GetTowerToBuild()
    {
        return towerToBuild;
    }

    // Method called by UI buttons to select a tower type
    public void SelectTowerToBuild(GameObject towerPrefab)
    {
        DeselectTile(); // Deselect any existing tile/tower
        HideBuildPreview();

        towerToBuild = towerPrefab;
    }

    public void SelectTile(Tile tile)
    {
        if (previewTowerInstance != null && selectedTile == tile)
        {
            TryPlaceTower(tile);
            return;
        }

        DeselectTile();
        HideBuildPreview();

        selectedTile = tile;

        // Use helper method to check for tower
        Tower existingTower = GetTowerOnTile(tile);
        if (existingTower != null)
        {
            selectedTower = existingTower;
            towerToBuild = null;
            Debug.Log($"Selected existing tower: {selectedTower.name}");
            if (UIManager.Instance != null) UIManager.Instance.ShowUpgradeUI(selectedTower);
            selectedTower.ShowRangeIndicator(true);
        }
        else if (!tile.IsBuildable()) // IsBuildable now checks IsTileOccupied
        {
             Debug.Log("Selected unbuildable tile.");
             HideBuildPreview();
        }
    }

    public void SelectTower(Tower tower)
    {
         HideBuildPreview();

        if (selectedTower == tower)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowUpgradeUI(selectedTower);
            if (selectedTower != null) selectedTower.ShowRangeIndicator(true);
            return;
        }

        DeselectTile();

        selectedTower = tower;
        selectedTile = null; // Ensure tile is deselected when selecting tower directly
        towerToBuild = null;

        Debug.Log($"Selected existing tower directly: {selectedTower.name}");
        if (UIManager.Instance != null) UIManager.Instance.ShowUpgradeUI(selectedTower);
        selectedTower.ShowRangeIndicator(true);
    }

    public void ShowBuildPreview(Tile tile)
    {
        if (towerToBuild == null || GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Build)
        {
            return;
        }
        
        if (!tile.IsBuildable())
        {
            HideBuildPreview();
            return;
        }

        if (previewTowerInstance != null && selectedTile == tile)
        {
            return;
        }

        HideBuildPreview();

        selectedTile = tile;

        Vector3 position = tile.GetBuildPosition();
        previewTowerInstance = Instantiate(towerToBuild, position, Quaternion.identity);
        Tower previewTowerScript = previewTowerInstance.GetComponent<Tower>();

        if (previewTowerScript != null)
        {
            previewTowerScript.SetPreviewMode(true);
            previewTowerScript.ShowRangeIndicator(true);
            SetLayerRecursively(previewTowerInstance, ignoreRaycastLayer);
        }
        else
        {
             Debug.LogError("Tower prefab is missing Tower script component!");
             Destroy(previewTowerInstance);
             previewTowerInstance = null;
        }
    }

    public void HideBuildPreview()
    {
        if (previewTowerInstance != null)
        {
            Destroy(previewTowerInstance);
            previewTowerInstance = null;
        }
        if (selectedTower == null)
        {
            selectedTile = null;
        }
    }

    public void DeselectTile()
    {
        if (selectedTower != null)
        {
            selectedTower.ShowRangeIndicator(false);
        }
        HideBuildPreview();

        selectedTile = null;
        selectedTower = null;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowUpgradeUI(null);
        }
    }

    private void TryPlaceTower(Tile tile)
    {
        if (previewTowerInstance == null || selectedTile != tile)
        {
             Debug.LogError("Build attempt failed: No valid preview found on the tile.");
             HideBuildPreview();
             return;
        }

        if (GameManager.Instance.CurrentState != GameManager.GameState.Build)
        {
             Debug.Log("Can only build during Build Phase!");
             if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus("Can only build during Build Phase!");
             HideBuildPreview();
             return;
        }

        Tower towerComponent = previewTowerInstance.GetComponent<Tower>();
        if (towerComponent == null)
        {
             Debug.LogError("Preview instance is missing Tower component!");
             HideBuildPreview();
             return;
        }

        // Check if tile is already occupied using the dictionary
        if (IsTileOccupied(tile))
        {
            Debug.LogWarning($"Build attempt failed: Tile {tile.name} is already occupied.");
            HideBuildPreview();
            return;
        }


        if (GameManager.Instance.CurrentCurrency < towerComponent.GetCost())
        {
            Debug.Log($"Not enough currency to build {towerToBuild.name}! Need {towerComponent.GetCost()}");
            if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus($"Need {towerComponent.GetCost()} coins!");
            return;
        }

        if (GameManager.Instance.SpendCurrency(towerComponent.GetCost()))
        {
            // Finalize tower placement
            towerComponent.SetPreviewMode(false);
            towerComponent.ShowRangeIndicator(false);
            SetLayerRecursively(previewTowerInstance, LayerMask.NameToLayer("Default"));

            Debug.Log($"Tower {previewTowerInstance.name} built on tile {tile.name}!");

            // Add to dictionary instead of setting Tile.Tower
            placedTowers.Add(tile, towerComponent);

            previewTowerInstance = null; // Clear preview reference
            selectedTile = null; // Clear selected tile specifically after build
        }
        else
        {
             Debug.LogWarning("Failed to spend currency even after check passed.");
        }
    }

    public void UpgradeSelectedTower()
    {
        if (selectedTower == null)
        {
            Debug.LogError("Upgrade attempt failed: No tower selected.");
            return;
        }

        if (!selectedTower.CanUpgrade())
        {
             Debug.LogWarning("Selected tower cannot be upgraded further.");
             if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus("Max level reached!");
             return;
        }

        if (GameManager.Instance.CurrentCurrency < selectedTower.GetUpgradeCost())
        {
             Debug.LogWarning($"Not enough currency to upgrade. Need {selectedTower.GetUpgradeCost()}");
              if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus($"Need {selectedTower.GetUpgradeCost()} coins!");
             return;
        }

        selectedTower.Upgrade();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowUpgradeUI(selectedTower); // Refresh UI after upgrade
        }
    }

    public void SellSelectedTower()
    {
        if (selectedTower == null)
        {
            Debug.LogError("Sell attempt failed: No tower selected.");
            return;
        }

        int sellValue = selectedTower.GetCost() / 2; // Sell for half the original cost

        // Get the tile the tower is on using the helper method
        Tile tile = GetTileForTower(selectedTower);

        if (tile == null)
        {
            Debug.LogError($"Could not find the Tile for tower {selectedTower.name} in the registry!");
            return; // Stop if tile cannot be determined
        }


        GameManager.Instance.AddCurrency(sellValue);
        Destroy(selectedTower.gameObject);
        // Remove from dictionary instead of setting Tile.Tower = null
        placedTowers.Remove(tile);

        DeselectTile(); // Deselect after selling

        if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus($"Sold for {sellValue} coins!");
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
