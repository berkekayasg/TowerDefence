using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using System.Collections.Generic; // Required for List

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Sequence")]
    [SerializeField] private List<LevelData> levelSequence;
    [SerializeField] private string gameSceneName = "SampleScene";

    private static int nextLevelIndexToLoad = 0; // Persists across scene loads

    private int currentLevelIndex = -1;
    private LevelData currentLevelData;

    public LevelData CurrentLevelData => currentLevelData;
    public int CurrentLevelIndex => currentLevelIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        LoadLevelDataForCurrentScene();
    }

    private void LoadLevelDataForCurrentScene()
    {
        if (levelSequence == null || levelSequence.Count == 0)
        {
            Debug.LogError("LevelManager: Level Sequence is not assigned or empty!");
            enabled = false;
            return;
        }

        // Clamp the index to be within valid bounds
        currentLevelIndex = Mathf.Clamp(nextLevelIndexToLoad, 0, levelSequence.Count - 1);

        if (nextLevelIndexToLoad >= levelSequence.Count)
        {
             Debug.LogWarning($"LevelManager: Attempted to load level index {nextLevelIndexToLoad}, loading last level.");
        }

        // Debug.Log($"LevelManager (LoadLevelData): nextLevelIndexToLoad = {nextLevelIndexToLoad}, currentLevelIndex set to = {currentLevelIndex}");
        currentLevelData = levelSequence[currentLevelIndex];

        if (currentLevelData == null)
        {
            Debug.LogError($"LevelManager: LevelData at index {currentLevelIndex} in the sequence is null!");
            enabled = false;
        }
        else
        {
             Debug.Log($"LevelManager: Loaded Level '{currentLevelData.name}' (Index: {currentLevelIndex})");
        }
    }

    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levelSequence.Count)
        {
            nextLevelIndexToLoad = nextIndex;
            // Debug.Log($"LevelManager (LoadNextLevel): Set nextLevelIndexToLoad to {nextLevelIndexToLoad}. Reloading scene: {gameSceneName}");
            // currentLevelData = levelSequence[nextIndex]; // This will be set in Awake of the new scene instance
            // currentLevelIndex = nextIndex; // This will be set in Awake of the new scene instance
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.Log("LevelManager: All levels completed!");
            // Consider loading a main menu or credits scene here
            // SceneManager.LoadScene("MainMenu");
        }
    }

    public void RestartLevel()
    {
        nextLevelIndexToLoad = currentLevelIndex; // Ensure we reload the same level index
        // Debug.Log($"LevelManager: Restarting level (Index: {currentLevelIndex}). Reloading scene: {gameSceneName}");
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadLevelByIndex(int index)
    {
        if (index >= 0 && index < levelSequence.Count)
        {
            nextLevelIndexToLoad = index;
            // Debug.Log($"LevelManager: Loading level by index (Index: {nextLevelIndexToLoad}). Reloading scene: {gameSceneName}");
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError($"LevelManager: Invalid level index requested: {index}");
        }
    }
}
