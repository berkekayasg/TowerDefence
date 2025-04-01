using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "TowerDefense/TowerData")]
public class TowerData : ScriptableObject
{
    [Header("Identification")]
    public string towerName = "New Tower";
    public Sprite towerIcon; // For UI

    [Header("Prefabs")]
    public GameObject towerPrefab;
    public GameObject projectilePrefab; // If applicable

    [Header("Base Stats")]
    public float range = 5f;
    public float damage = 10f;
    public float fireRate = 1f; // Shots per second
    public int cost = 100;

    [Header("Upgrade Stats (Level 1 -> 2)")]
    public int upgradeCost = 50;
    public float upgradeRangeIncrease = 1f;
    public float upgradeDamageIncrease = 5f;
    public float upgradeFireRateIncrease = 0.2f; // Increase in shots per second

    // Add more levels or different upgrade paths if needed
    // Consider adding fields for special abilities (e.g., slow effect, splash radius)
}
