using System;
using UnityEngine;
using System.Collections.Generic;
using TowerDefence.Core;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    // --- Events ---
    public static event Action<Tower, Tile> OnTowerPlaced;
    public static event Action<Tower, Tile> OnTowerSold;
    public static event Action<Tower> OnTowerUpgraded;
    // --- End Events ---

    [Header("Setup")]
    public List<TowerData> availableTowers;

    private TowerData towerToBuild;
    private Tile selectedTile;
    private Tower selectedTower;
    private GameObject previewTowerInstance;

    // Dictionary to track placed towers and their tiles
    private Dictionary<Tile, Tower> placedTowers = new Dictionary<Tile, Tower>();

    private int ignoreRaycastLayer;

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
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer == -1)
        {
             ignoreRaycastLayer = 2;
             Debug.LogWarning("'Ignore Raycast' layer not found. Defaulting to layer 2.");
        }
    }

    void Update()
    {
        // Check for right-click to deselect
        if (Input.GetMouseButtonDown(1))
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


    public TowerData GetTowerToBuild() // Changed return type
    {
        return towerToBuild;
    }

    // Method called by UI buttons to select a tower type
    public void SelectTowerToBuild(TowerData towerData) // Changed parameter type
    {
        DeselectTile(); // Deselect any existing tile/tower
        HideBuildPreview();

        towerToBuild = towerData; // Assign TowerData
        if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus($"Selected: {towerData.towerName}");
    }

     public void SelectTile(Tile tile)
    {
        // If a tower type is selected and we click a buildable tile, try to place it
        if (towerToBuild != null && tile.IsBuildable())
        {
            TryPlaceTower(tile);
            return;
        }
        // If we clicked the same tile where a preview is already showing, try placing
        else if (previewTowerInstance != null && selectedTile == tile)
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
        else if (!tile.IsBuildable())
        {
             Debug.Log("Selected unbuildable tile.");
             // Don't show preview if unbuildable
             HideBuildPreview();
             // Clear tower selection if clicking unbuildable tile
             towerToBuild = null;
             if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus("");
        }
        // If the tile is buildable but no tower type is selected, just show preview potential
        else if (towerToBuild != null)
        {
             ShowBuildPreview(tile);
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
        // Check if a tower type is selected and we are in build mode
        if (towerToBuild == null || towerToBuild.towerPrefab == null || GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Build)
        {
            HideBuildPreview(); // Ensure preview is hidden if conditions aren't met
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

        HideBuildPreview(); // Clear previous preview first

        selectedTile = tile; // Remember the tile we're previewing on

        Vector3 position = tile.GetBuildPosition();
        // Instantiate the prefab from the selected TowerData
        previewTowerInstance = Instantiate(towerToBuild.towerPrefab, position, Quaternion.identity);
        Tower previewTowerScript = previewTowerInstance.GetComponent<Tower>();

        if (previewTowerScript != null)
        {
            // Pass the TowerData to the preview script
            previewTowerScript.SetPreviewMode(true, towerToBuild);
            // ShowRangeIndicator is now handled within SetPreviewMode
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
        // Need a selected tower type to build
        if (towerToBuild == null || towerToBuild.towerPrefab == null)
        {
            Debug.LogError("Build attempt failed: No TowerData selected or prefab missing.");
            HideBuildPreview();
            return;
        }

        // Ensure we are in build state
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Build)
        {
             Debug.Log("Can only build during Build Phase!");
             if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus("Can only build during Build Phase!");
             HideBuildPreview();
             return;
        }

        // Check if the tile is buildable (includes occupancy check now)
        if (!tile.IsBuildable())
        {
            Debug.LogWarning($"Build attempt failed: Tile {tile.name} is not buildable or already occupied.");
            HideBuildPreview();
            return;
        }

        // Check currency against the selected TowerData's cost
        if (GameManager.Instance.CurrentCurrency < towerToBuild.cost)
        {
            Debug.Log($"Not enough currency to build {towerToBuild.towerName}! Need {towerToBuild.cost}");
            if (UIManager.Instance != null) UIManager.Instance.ShowBuildStatus($"Need {towerToBuild.cost} coins!");
            // Don't hide preview here, let player see they can't afford it
            return;
        }

        // Try spending currency
        if (GameManager.Instance.SpendCurrency(towerToBuild.cost))
        {
            // Instantiate the final tower from TowerData prefab
            Vector3 position = tile.GetBuildPosition();
            GameObject towerGO = Instantiate(towerToBuild.towerPrefab, position, Quaternion.identity);
            Tower newTower = towerGO.GetComponent<Tower>();

            if (newTower != null)
            {
                // Assign the TowerData to the new tower instance
                newTower.towerData = towerToBuild;
                // SetPreviewMode(false) is implicitly handled by not being in preview
                newTower.ShowRangeIndicator(false); // Ensure indicator is off initially
                SetLayerRecursively(towerGO, LayerMask.NameToLayer("Default")); // Set layer correctly

                Debug.Log($"Tower {towerToBuild.towerName} built on tile {tile.name}!");

                // Add to dictionary
                placedTowers.Add(tile, newTower);

                // Raise event instead of direct calls
                OnTowerPlaced?.Invoke(newTower, tile);

                // Clear selections after successful build
                HideBuildPreview(); // Destroy preview if it existed
                selectedTile = null;
                // Keep towerToBuild selected for potentially building more of the same type
                // towerToBuild = null; // Uncomment if you want to force re-selection after each build
            }
            else
            {
                Debug.LogError($"Instantiated tower prefab {towerToBuild.towerName} is missing Tower script!");
                // Refund currency if instantiation failed critically
                GameManager.Instance.AddCurrency(towerToBuild.cost);
                Destroy(towerGO); // Clean up failed instance
                HideBuildPreview();
            }
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

        // We raise the event here for external systems (like UI refresh) if needed.
        OnTowerUpgraded?.Invoke(selectedTower);


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

        // Raise event before destroying
        OnTowerSold?.Invoke(selectedTower, tile);

        Destroy(selectedTower.gameObject);
        // Remove from dictionary
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
