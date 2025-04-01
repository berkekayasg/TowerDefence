using System;
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

    // --- Events ---
    public static event Action<int> OnCurrencyChanged;
    public static event Action<int> OnLivesChanged;
    public static event Action<GameState> OnStateChanged;

    [Header("Game Status")]
    private int _currentLives;
    public int CurrentLives
    {
        get { return _currentLives; }
        private set
        {
            if (_currentLives != value)
            {
                _currentLives = value;
                OnLivesChanged?.Invoke(_currentLives);
                 if (_currentLives <= 0 && CurrentState != GameState.GameOver) // Check for game over when lives change
                 {
                     SetState(GameState.GameOver);
                 }
            }
        }
    }
    private int _currentCurrency;
    public int CurrentCurrency
    {
        get { return _currentCurrency; }
        private set
        {
            if (_currentCurrency != value)
            {
                _currentCurrency = value;
                OnCurrencyChanged?.Invoke(_currentCurrency);
            }
        }
    }
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

        // Use property setters to trigger initial events
        CurrentLives = levelData.startingLives;
        CurrentCurrency = levelData.startingCurrency;
        _waveDataList = levelData.waveDataList;

        // Initial UI updates are handled by subscribers listening to events
        if (UIManager.Instance != null)
        {
             UIManager.Instance.UpdateWave(0, TotalWaves); // Keep this for now
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
        OnStateChanged?.Invoke(CurrentState); // Raise the event

        if (buildTimerCoroutine != null)
        {
            StopCoroutine(buildTimerCoroutine);
            buildTimerCoroutine = null;
        }
        if (UIManager.Instance != null) UIManager.Instance.UpdateTimer(null);


        switch (newState)
        {
            case GameState.Build:
                buildTimerCoroutine = StartCoroutine(StartNextWaveTimer());
                break;
            case GameState.Wave:
                waveElapsedTime = 0f;
                 if (UIManager.Instance != null) // Keep specific data updates for now
                 {
                     UIManager.Instance.UpdateWave(currentWaveNumber + 1, TotalWaves);
                 }
                StartCoroutine(SpawnWave());
                break;
            case GameState.GameOver:
                Debug.LogError("GAME OVER!");
                Time.timeScale = 0; // Keep game logic here
                break;
            case GameState.Victory:
                 Debug.Log("VICTORY!");
                 if (LevelManager.Instance != null) // Keep game logic here
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
        CurrentLives--; // Use property setter, which also handles Game Over check
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
        Debug.Log($"Enemy reached end. Lives remaining: {CurrentLives}");
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
        CurrentCurrency += amount; // Use property setter
    }

    public bool SpendCurrency(int amount)
    {
        if (CurrentCurrency >= amount) // Check against property getter
        {
            CurrentCurrency -= amount; // Use property setter
            return true;
        }
        else
        {
            Debug.LogWarning($"Not enough currency. Needed: {amount}, Have: {CurrentCurrency}");
            return false;
        }
    }
}
