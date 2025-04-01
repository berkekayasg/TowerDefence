using UnityEngine;
using TowerDefence.Core;

public class Tower : MonoBehaviour
{
    [Header("Data")]
    [HideInInspector] public TowerData towerData;

    private bool isUpgraded = false;
    private bool isPreview = false;

    [Header("References")]
    private Transform target;
    [SerializeField] private Transform partToRotate;
    public GameObject rangeIndicator; // Assign in Inspector

    private float fireCountdown = 0f;

    // Current stats (can be modified by upgrades)
    private float currentRange;
    private float currentDamage;
    private float currentFireRate;

    // References from TowerData
    private GameObject currentProjectilePrefab;


    void Start()
    {
        if (towerData == null)
        {
            Debug.LogError("TowerData not assigned to Tower!", this);
            return;
        }

        // Initialize stats from TowerData
        currentRange = towerData.range;
        currentDamage = towerData.damage;
        currentFireRate = towerData.fireRate;
        currentProjectilePrefab = towerData.projectilePrefab; // Get prefab from data

        if (!isPreview)
        {
            ShowRangeIndicator(false);
            // Ensure range indicator is updated if Start runs after preview mode is set
            ShowRangeIndicator(rangeIndicator != null && rangeIndicator.activeSelf);
        }
        else
        {
             // Ensure preview has base range for indicator scaling if Start runs in preview mode
             if (currentRange <= 0) currentRange = towerData.range;
             ShowRangeIndicator(true); // Show indicator for preview
        }
    }

     void Update()
    {
        if (isPreview) return;

        if (target == null)
        {
            FindTarget();
            return;
        }

        if (partToRotate != null)
        {
            Vector3 dir = target.position - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * 10f).eulerAngles;
            partToRotate.rotation = Quaternion.Euler(0f, rotation.y, 0f);
        }

        if (Vector3.Distance(transform.position, target.position) > currentRange)
        {
            target = null;
            return;
        }

        if (fireCountdown <= 0f)
        {
            // Check if we have valid data before shooting
            if (towerData == null) return;

            Shoot();
            fireCountdown = 1f / currentFireRate;
        }

        fireCountdown -= Time.deltaTime;
    }

    void FindTarget()
    {
        if (isPreview) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, currentRange);
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                float distanceToEnemy = Vector3.Distance(transform.position, collider.transform.position);
                if (distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    nearestEnemy = collider.transform;
                }
            }
        }

        target = nearestEnemy;
    }

    [Header("Projectile")]
    // Removed projectilePrefab field, now comes from TowerData
    [SerializeField] private Transform firePoint;

    void Shoot()
    {
        if (isPreview || towerData == null) return; // Don't shoot in preview or without data

        // Play shoot sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEffect("TowerShoot"); // Using hardcoded name
        }

        // Play shoot VFX
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayVFX("TowerShoot", firePoint.position, firePoint.rotation);
        }

        if (currentProjectilePrefab == null || firePoint == null)
        {
            Debug.LogError($"Projectile prefab ({currentProjectilePrefab}) or fire point ({firePoint}) not set correctly for {towerData.towerName}!", this);
            return;
        }

        GameObject projectileGO = Instantiate(currentProjectilePrefab, firePoint.position, firePoint.rotation);
        Projectile projectile = projectileGO.GetComponent<Projectile>();
        if (projectile != null)
        {
            float speed = towerData.projectileSpeed;
            float damage = currentDamage;

            // Initialize the projectile with target, speed, and damage
            projectile.Initialize(target, speed, damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Use currentRange if playing and valid, otherwise fallback to TowerData range
        float rangeToShow = (Application.isPlaying && currentRange > 0) ? currentRange : (towerData != null ? towerData.range : 0f);
        if (rangeToShow > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, rangeToShow);
        }
    }

    // Getters now pull from TowerData
    public int GetCost()
    {
        return towerData != null ? towerData.cost : 0;
    }

    public int GetUpgradeCost()
    {
        return towerData != null ? towerData.upgradeCost : 0;
    }

    public bool IsUpgraded()
    {
        return isUpgraded;
    }

    public bool CanUpgrade()
    {
        return !isUpgraded;
    }

    public void Upgrade()
    {
        if (isPreview || towerData == null) return; // Can't upgrade preview or without data

        if (!CanUpgrade())
        {
            Debug.LogWarning("Tower cannot be upgraded further.");
            return;
        }

        int costToUpgrade = GetUpgradeCost(); // Get cost from data
        if (GameManager.Instance.SpendCurrency(costToUpgrade))
        {
            isUpgraded = true;

            // Apply upgrades from TowerData
            currentRange += towerData.upgradeRangeIncrease;
            currentDamage += towerData.upgradeDamageIncrease;
            currentFireRate += towerData.upgradeFireRateIncrease;
            // Note: Projectile prefab doesn't change on upgrade in this basic setup

            if (rangeIndicator != null && rangeIndicator.activeSelf)
            {
                ShowRangeIndicator(true); // Update indicator size
            }
        }
        else
        {
            Debug.LogWarning("Not enough currency to upgrade tower!");
        }
    }

    public void SetPreviewMode(bool preview, TowerData dataForPreview = null)
    {
        isPreview = preview;
        if (preview)
        {
            // Assign data if provided (important for range indicator scaling)
            if (dataForPreview != null)
            {
                towerData = dataForPreview;
                currentRange = towerData.range; // Set base range for preview scaling
                currentProjectilePrefab = towerData.projectilePrefab; // Set prefab for preview if needed later
            }
            else
            {
                 Debug.LogWarning("Preview mode set without TowerData. Range indicator might not scale correctly initially.", this);
                 // Attempt to use existing data if available, otherwise default range might be 0
                 currentRange = (towerData != null) ? towerData.range : 0f;
            }

            target = null;
            fireCountdown = 0f;
            ShowRangeIndicator(true); // Always show indicator in preview
        }
        else
        {
            // When exiting preview, ensure stats are correctly initialized if Start hasn't run yet
             if (towerData != null && currentRange <= 0)
             {
                 currentRange = towerData.range;
                 currentDamage = towerData.damage;
                 currentFireRate = towerData.fireRate;
                 currentProjectilePrefab = towerData.projectilePrefab;
             }
        }
    }

     public void ShowRangeIndicator(bool show)
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(show);
            if (show)
            {
                // Use currentRange if valid, otherwise fallback to TowerData range
                float rangeToScale = (currentRange > 0) ? currentRange : (towerData != null ? towerData.range : 0f);
                if (rangeToScale > 0)
                {
                    float diameter = rangeToScale * 2f;
                    // Preserve Y scale if it's meaningful, otherwise use 1
                    float originalYScale = (rangeIndicator.transform.localScale.y != 0 && !float.IsNaN(rangeIndicator.transform.localScale.y)) ? rangeIndicator.transform.localScale.y : 1f;
                    rangeIndicator.transform.localScale = new Vector3(diameter, originalYScale, diameter);
                }
                else
                {
                    // Hide or set to zero scale if range is invalid
                    rangeIndicator.transform.localScale = Vector3.zero;
                     Debug.LogWarning("Cannot scale range indicator: range is zero or TowerData is missing.", this);
                }
            }
        }
    }

    private void OnMouseDown()
    {
        if (isPreview) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Build)
        {
            BuildManager.Instance.SelectTower(this);
        }
        else
        {
            Debug.Log("Can only select towers during Build phase.");
        }
    }
}
