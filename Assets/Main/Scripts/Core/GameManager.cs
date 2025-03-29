using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { None, Build, Wave, GameOver, Victory }
    public GameState CurrentState { get; private set; } = GameState.None;

    [Header("Game Settings")]
    [SerializeField] private int waveCompletionBonus = 25;
    [SerializeField] private float timeBetweenWaves = 5f;

    private List<WaveData> _waveDataList; // Populated from LevelData

    public int TotalWaves => _waveDataList != null ? _waveDataList.Count : 0;

    [Header("Game Status")]
    public int CurrentLives { get; private set; }
    public int CurrentCurrency { get; private set; }
    private int currentWaveNumber = 0;
    private float waveElapsedTime = 0f;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private bool isSpawning = false;
    private Coroutine buildTimerCoroutine;

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
    }

    void Start()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("GameManager (Awake): LevelManager instance not found!");
            enabled = false;
            return;
        }
        LevelData levelData = LevelManager.Instance.CurrentLevelData;
        if (levelData == null)
        {
            Debug.LogError("GameManager (Awake): LevelData not assigned in LevelManager!");
            enabled = false;
            return;
        }

        CurrentLives = levelData.startingLives;
        CurrentCurrency = levelData.startingCurrency;
        _waveDataList = levelData.waveDataList;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(CurrentLives);
            UIManager.Instance.UpdateCurrency(CurrentCurrency);
            UIManager.Instance.UpdateWave(0, TotalWaves);
        }
        else
        {
            Debug.LogWarning("UIManager instance not found in Start!");
        }
        SetState(GameState.Build);
    }

    void Update()
    {
        if (CurrentState == GameState.Wave && !isSpawning && activeEnemies.Count == 0)
        {
             EndWave();
        }

        if (CurrentState == GameState.Wave)
        {
            waveElapsedTime += Time.deltaTime;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimer($"Wave Time: {waveElapsedTime:F1}s");
            }
        }
    }

    private void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        if (buildTimerCoroutine != null)
        {
            StopCoroutine(buildTimerCoroutine);
            buildTimerCoroutine = null;
        }
        if (UIManager.Instance != null) UIManager.Instance.UpdateTimer(null);


        switch (newState)
        {
            case GameState.Build:
                if (UIManager.Instance != null) UIManager.Instance.ShowTemporaryMessage("Build Phase");
                buildTimerCoroutine = StartCoroutine(StartNextWaveTimer());
                break;
            case GameState.Wave:
                waveElapsedTime = 0f;
                 if (UIManager.Instance != null)
                 {
                     UIManager.Instance.UpdateWave(currentWaveNumber + 1, TotalWaves);
                     UIManager.Instance.ShowTemporaryMessage($"Wave {currentWaveNumber + 1} Starting!");
                 }
                StartCoroutine(SpawnWave());
                break;
            case GameState.GameOver:
                Debug.LogError("GAME OVER!");
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowGameOverScreen();
                    UIManager.Instance.UpdateTimer(null);
                }
                Time.timeScale = 0;
                break;
            case GameState.Victory:
                 Debug.Log("VICTORY!");
                 if (UIManager.Instance != null)
                 {
                     UIManager.Instance.ShowVictoryScreen();
                     UIManager.Instance.UpdateTimer(null);
                 }
                 if (LevelManager.Instance != null)
                 {
                     LevelManager.Instance.LoadNextLevel();
                 }
                 else
                 {
                     Debug.LogError("GameManager: LevelManager instance not found! Cannot load next level.");
                 }
                break;
            case GameState.None:
                if (UIManager.Instance != null) UIManager.Instance.UpdateTimer(null);
                break;
        }
    }

    IEnumerator StartNextWaveTimer()
    {
        float countdown = timeBetweenWaves;

        while (countdown > 0)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimer($"Next Wave: {Mathf.CeilToInt(countdown)}s");
            }
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTimer("Next Wave: 0s");
        }
        yield return new WaitForSeconds(0.1f);

        if (CurrentState == GameState.Build)
        {
             SetState(GameState.Wave);
        }
        buildTimerCoroutine = null;
    }

    IEnumerator SpawnWave()
    {
        isSpawning = true;
        currentWaveNumber++;

        if (_waveDataList == null)
        {
             Debug.LogError("Wave data list is null! Cannot spawn wave.");
             isSpawning = false;
             yield break;
        }


        if (currentWaveNumber <= _waveDataList.Count)
        {
            WaveData currentWave = _waveDataList[currentWaveNumber - 1];

            if (currentWave.initialWaveDelay > 0)
            {
                yield return new WaitForSeconds(currentWave.initialWaveDelay);
            }

            foreach (var spawnGroup in currentWave.enemySpawnGroups)
            {
                if (spawnGroup.enemyData == null || spawnGroup.enemyData.enemyPrefab == null)
                {
                    Debug.LogError($"Wave {currentWaveNumber} contains invalid EnemyData or prefab in WaveData asset!");
                    continue;
                }
                for (int i = 0; i < spawnGroup.spawnCount; i++)
                {
                    SpawnEnemy(spawnGroup.enemyData);
                    if (spawnGroup.timeBetweenSpawns > 0)
                    {
                        yield return new WaitForSeconds(spawnGroup.timeBetweenSpawns);
                    }
                }
                if (spawnGroup.delayAfterGroup > 0)
                {
                    yield return new WaitForSeconds(spawnGroup.delayAfterGroup);
                }
            }
        }
        else
        {
            Debug.LogWarning($"No wave data for wave {currentWaveNumber}.");
        }

        isSpawning = false;
    }

    void SpawnEnemy(EnemyData enemyDataToSpawn = null)
    {
        EnemyData data = enemyDataToSpawn;
        GameObject prefabToInstantiate = null;

        if (data != null && data.enemyPrefab != null)
        {
            prefabToInstantiate = data.enemyPrefab;
        }

        if (prefabToInstantiate != null && GridManager.Instance != null && GridManager.Instance.GetStartTile() != null)
        {
            GameObject enemyGO = Instantiate(prefabToInstantiate, GridManager.Instance.GetStartTile().transform.position, Quaternion.identity);
            Enemy newEnemy = enemyGO.GetComponent<Enemy>();

            if (newEnemy != null)
            {
                newEnemy.Initialize(data);
                activeEnemies.Add(newEnemy);
            }
            else
            {
                Debug.LogError($"Spawned object {enemyGO.name} is missing Enemy component!");
                Destroy(enemyGO);
            }
        }
        else
        {
            if (prefabToInstantiate == null)
                Debug.LogError("Cannot spawn enemy: No valid prefab determined.");
            else
                Debug.LogError("Cannot spawn enemy: GridManager or Start Tile not available.");
        }
    }

    private void EndWave()
    {
        if (currentWaveNumber >= TotalWaves)
        {
            SetState(GameState.Victory);
        }
        else
        {
            AddCurrency(waveCompletionBonus);
            SetState(GameState.Build);
        }
    }

    public void EnemyReachedEnd(Enemy enemy)
    {
        CurrentLives--;
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
        Debug.Log($"Enemy reached end. Lives remaining: {CurrentLives}");
        if (UIManager.Instance != null) UIManager.Instance.UpdateLives(CurrentLives);
        if (CurrentLives <= 0 && CurrentState != GameState.GameOver)
        {
            SetState(GameState.GameOver);
        }
    }

     public void EnemyDefeated(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
             activeEnemies.Remove(enemy);
        }
        AddCurrency(enemy.GetCurrencyValue());
    }

    public void AddCurrency(int amount)
    {
        CurrentCurrency += amount;
        if (UIManager.Instance != null) UIManager.Instance.UpdateCurrency(CurrentCurrency);
    }

    public bool SpendCurrency(int amount)
    {
        if (CurrentCurrency >= amount)
        {
            CurrentCurrency -= amount;
            if (UIManager.Instance != null) UIManager.Instance.UpdateCurrency(CurrentCurrency);
            return true;
        }
        else
        {
            Debug.LogWarning($"Not enough currency. Needed: {amount}, Have: {CurrentCurrency}");
            return false;
        }
    }
}
