using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] private float range = 10f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private int cost = 50;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 75;
    [SerializeField] private float upgradedRange = 12f;
    [SerializeField] private float upgradedDamage = 15f;
    [SerializeField] private float upgradedFireRate = 1.5f;

    private bool isUpgraded = false;
    private bool isPreview = false;

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform partToRotate;
    [SerializeField] public GameObject rangeIndicator; // Assign in Inspector

    private float fireCountdown = 0f;

    private float currentRange;
    private float currentDamage;
    private float currentFireRate;


    void Start()
    {
        currentRange = range;
        currentDamage = damage;
        currentFireRate = fireRate;

        if (!isPreview)
        {
            ShowRangeIndicator(false);
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
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    void Shoot()
    {
        if (isPreview) return;

        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError("Projectile prefab or fire point not set!");
            return;
        }

        GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile projectile = projectileGO.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Seek(target);
        }
    }

    void OnDrawGizmosSelected()
    {
        float rangeToShow = (Application.isPlaying && currentRange > 0) ? currentRange : range;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeToShow);
    }

    public int GetCost()
    {
        return cost;
    }

    public int GetUpgradeCost()
    {
        return upgradeCost;
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
        if (isPreview) return;

        if (!CanUpgrade())
        {
            Debug.LogWarning("Tower cannot be upgraded further.");
            return;
        }

        if (GameManager.Instance.SpendCurrency(upgradeCost))
        {
            isUpgraded = true;
            currentRange = upgradedRange;
            currentDamage = upgradedDamage;
            currentFireRate = upgradedFireRate;

            if (rangeIndicator != null && rangeIndicator.activeSelf)
            {
                ShowRangeIndicator(true); // Update indicator size
            }

            Debug.Log("Tower Upgraded!");
        }
        else
        {
            Debug.LogWarning("Not enough currency to upgrade tower!");
        }
    }

    public void SetPreviewMode(bool preview)
    {
        isPreview = preview;
        if (preview)
        {
            target = null;
            fireCountdown = 0f;
            if (currentRange <= 0) currentRange = range; // Ensure range is set for scaling
        }
    }

    public void ShowRangeIndicator(bool show)
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(show);
            if (show)
            {
                 float rangeToScale = (currentRange > 0) ? currentRange : range;
                float diameter = rangeToScale * 2f;
                float originalYScale = (rangeIndicator.transform.localScale.y != 0) ? rangeIndicator.transform.localScale.y : 1f;
                rangeIndicator.transform.localScale = new Vector3(diameter, originalYScale, diameter);
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
