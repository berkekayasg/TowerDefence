using UnityEngine;

namespace TowerDefence.Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private VFXStorage vfxStorage;

        private void Awake()
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

        // --- Event Subscription ---
        void OnEnable()
        {
            BuildManager.OnTowerPlaced += HandleTowerPlaced;
            BuildManager.OnTowerSold += HandleTowerSold;
            BuildManager.OnTowerUpgraded += HandleTowerUpgraded;
        }

        void OnDisable()
        {
            // Check if BuildManager instance exists before unsubscribing
            if (BuildManager.Instance != null)
            {
                BuildManager.OnTowerPlaced -= HandleTowerPlaced;
                BuildManager.OnTowerSold -= HandleTowerSold;
                BuildManager.OnTowerUpgraded -= HandleTowerUpgraded;
            }
        }
        // --- End Event Subscription ---

        // --- Event Handlers ---
        private void HandleTowerPlaced(Tower tower, Tile tile)
        {
            if (tile != null)
            {
                PlayVFX("TowerBuild", tile.GetBuildPosition());
            }
        }

        private void HandleTowerSold(Tower tower, Tile tile)
        {
             if (tower != null)
             {
                 PlayVFX("TowerSell", tower.transform.position);
             }
        }

        private void HandleTowerUpgraded(Tower tower)
        {
             if (tower != null)
             {
                 PlayVFX("TowerUpgrade", tower.transform.position);
             }
        }
        // --- End Event Handlers ---


        public void PlayVFX(string name, Vector3 position, Quaternion rotation)
        {
            if (vfxStorage == null)
            {
                Debug.LogWarning("VFXManager: VFXStorage not assigned.");
                return;
            }

            VFXData vfxData = vfxStorage.GetVFXData(name);
            if (vfxData == null || vfxData.vfxPrefab == null)
            {
                Debug.LogWarning($"VFXManager: VFX '{name}' not found or prefab is null in VFXStorage.");
                return;
            }

            GameObject vfxInstance = Instantiate(vfxData.vfxPrefab, position, rotation);

            // Optional: Destroy after delay
            if (vfxData.destroyDelay > 0)
            {
                Destroy(vfxInstance, vfxData.destroyDelay);
            }
        }

        // Overload for playing at a position with default rotation
        public void PlayVFX(string name, Vector3 position)
        {
            PlayVFX(name, position, Quaternion.identity);
        }
    }
}
