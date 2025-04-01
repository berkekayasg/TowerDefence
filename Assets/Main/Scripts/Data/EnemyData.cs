using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "TowerDefense/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Stats")]
    public float health = 100f;
    public float moveSpeed = 2f;
    public int currencyValue = 5;
    public bool isFlying = false; // Added for flying enemies
    [Header("Visuals")]
    public GameObject enemyPrefab; // Reference to the visual prefab for this enemy type
    // Other potential fields like resistances, special abilities etc. later
}
