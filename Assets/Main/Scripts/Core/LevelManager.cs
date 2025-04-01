using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
            Instance.LoadLevelDataForCurrentScene();
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        Instance.LoadLevelDataForCurrentScene();
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

        currentLevelData = levelSequence[currentLevelIndex];

        if (currentLevelData == null)
        {
            Debug.LogError($"LevelManager: LevelData at index {currentLevelIndex} in the sequence is null!");
            enabled = false;
        }
    }

    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levelSequence.Count)
        {
            nextLevelIndexToLoad = nextIndex;
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.Log("LevelManager: All levels completed!");
            // Consider loading a main menu or credits scene here
            // SceneManager.LoadScene("CreditsScene"); // Example scene name
        }
    }

    public void RestartLevel()
    {
        nextLevelIndexToLoad = currentLevelIndex;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadLevelByIndex(int index)
    {
        if (index >= 0 && index < levelSequence.Count)
        {
            nextLevelIndexToLoad = index;
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError($"LevelManager: Invalid level index requested: {index}");
        }
    }
}
