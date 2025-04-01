using UnityEngine;
using System.Collections.Generic;
using TowerDefence.Core;

public class Enemy : MonoBehaviour
{
    private EnemyData enemyData;

    private float currentHealth;
    private float currentMoveSpeed;

    private GridManager gridManager;
    private List<Tile> path;
    private int currentWaypointIndex = 0;
    private Tile targetTile;

    public void Initialize(EnemyData data)
    {
        if (data == null)
        {
            Debug.LogError($"Attempted to initialize Enemy {gameObject.name} with null EnemyData!");
            enabled = false;
            return;
        }
        enemyData = data;
        currentHealth = enemyData.health;
        currentMoveSpeed = enemyData.moveSpeed;
    }

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found in the scene!");
            enabled = false;
            return;
        }

        Tile startTile = gridManager.GetStartTile();
        targetTile = gridManager.GetEndTile();

        if (startTile != null && targetTile != null)
        {
            path = Pathfinding.FindPath(gridManager, startTile, targetTile);
            if (path == null || path.Count == 0)
            {
                Debug.LogError($"Path could not be found for the enemy {gameObject.name}!");
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogError($"Start or End tile is null for enemy {gameObject.name}!");
            Destroy(gameObject);
        }
    }


    void Update()
    {
        Move();
    }

     private void Move()
    {
        if (path == null || path.Count == 0 || currentWaypointIndex >= path.Count)
        {
            // Check if actually at the end tile before calling ReachedEnd
            if (targetTile != null && Vector3.Distance(transform.position, targetTile.transform.position) < 1.1f)
            {
                 ReachedEnd();
            }
            return;
        }

        Tile currentWaypoint = path[currentWaypointIndex];
        Vector3 targetPosition = currentWaypoint.transform.position + new Vector3(0, 1f, 0); // Adjust height for the enemy

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentMoveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position + new Vector3(0, transform.position.y - targetPosition.y,0)), Time.deltaTime * 5f);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentWaypointIndex++;
        }
    }

    public void TakeDamage(float amount)
    {
        // Play hit sound (optional)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEffect("EnemyHit"); // Using hardcoded name
        }

        // Play hit VFX
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayVFX("EnemyHit", transform.position);
        }

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Play death sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEffect("EnemyDeath"); // Using hardcoded name
        }

        // Play death VFX
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayVFX("EnemyDeath", transform.position);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyDefeated(this);
        }
        Destroy(gameObject);
    }

    public int GetCurrencyValue()
    {
        return (enemyData != null) ? enemyData.currencyValue : 0;
    }

    public void ReachedEnd()
    {
         // Debug.Log("Enemy reached the end!"); // Keep for debugging if needed
         if (GameManager.Instance != null)
         {
             GameManager.Instance.EnemyReachedEnd(this);
         }
         Destroy(gameObject);
    }
}
