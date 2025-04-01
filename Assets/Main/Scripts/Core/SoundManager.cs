using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // Sound limiting
    private Dictionary<string, float> _lastPlayedTime = new Dictionary<string, float>();
    [Header("Sound Limiting")]
    [Tooltip("Minimum time in seconds before the same sound effect can be played again.")]
    [SerializeField] private float minTimeBetweenSameSound = 0.05f;

    [Header("Setup")]
    [SerializeField] private SoundClipStorage soundClipStorage;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource musicSource; // Optional

    [Header("Volume Controls")]
    [Range(0f, 1f)]
    [SerializeField] private float effectsVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f; // Optional


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (effectsSource == null)
        {
            effectsSource = gameObject.AddComponent<AudioSource>();
            Debug.LogWarning("SoundManager: Effects AudioSource not assigned, creating one dynamically.");
        }

        if (soundClipStorage == null)
        {
            Debug.LogError("SoundManager: SoundClipStorage asset is not assigned in the Inspector!", this);
            enabled = false;
            return;
        }
        SetEffectsVolume(effectsVolume);
        SetMusicVolume(musicVolume);
    }

    // --- Event Subscription ---
    void OnEnable()
    {
        GameManager.OnStateChanged += HandleGameStateChanged;
        BuildManager.OnTowerPlaced += HandleTowerPlaced;
        BuildManager.OnTowerSold += HandleTowerSold;
        BuildManager.OnTowerUpgraded += HandleTowerUpgraded;
    }

    void OnDisable()
    {
        // Check if instances exist before unsubscribing
        if (GameManager.Instance != null)
        {
            GameManager.OnStateChanged -= HandleGameStateChanged;
        }
        if (BuildManager.Instance != null)
        {
            BuildManager.OnTowerPlaced -= HandleTowerPlaced;
            BuildManager.OnTowerSold -= HandleTowerSold;
            BuildManager.OnTowerUpgraded -= HandleTowerUpgraded;
        }
    }
    // --- End Event Subscription ---

    // --- Event Handlers ---
    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Wave:
                PlayEffect("WaveStart");
                break;
            case GameManager.GameState.GameOver:
                PlayEffect("GameOver");
                break;
            case GameManager.GameState.Victory:
                PlayEffect("Victory");
                break;
                 // No sounds needed for Build or None states currently
        }
    }

    private void HandleTowerPlaced(Tower tower, Tile tile)
    {
        PlayEffect("TowerPlace");
    }

    private void HandleTowerSold(Tower tower, Tile tile)
    {
        PlayEffect("TowerSell");
    }

    private void HandleTowerUpgraded(Tower tower)
    {
        PlayEffect("TowerUpgrade");
    }
    // --- End Event Handlers ---

    public void PlayEffect(string soundName)
    {
        Debug.Log(soundName);
        if (string.IsNullOrEmpty(soundName))
        {
            Debug.LogWarning("SoundManager: Attempted to play effect with null or empty name.");
            return;
        }

        if (soundClipStorage == null || effectsSource == null)
        {
            Debug.LogError($"SoundManager: Cannot play effect '{soundName}'. Storage or Effects Source is not set up correctly.", this);
            return;
        }

        // --- Sound Limiting Check ---
        if (_lastPlayedTime.TryGetValue(soundName, out float lastTime))
        {
            if (Time.time - lastTime < minTimeBetweenSameSound)
            {
                return; // Don't play the sound, too soon
            }
        }
        // --- End Sound Limiting Check ---

        SoundClipData clipData = soundClipStorage.GetSoundClipData(soundName);

        if (clipData != null && clipData.clip != null)
        {
            // Update last played time *before* playing
            _lastPlayedTime[soundName] = Time.time;

            // Apply specific clip settings
            effectsSource.pitch = clipData.pitch;
            effectsSource.PlayOneShot(clipData.clip, effectsVolume * clipData.volumeScale);
        }
        else
        {
            Debug.LogWarning($"SoundManager: Sound clip '{soundName}' not found in storage or clip is null.", this);
        }
    }

    public void SetEffectsVolume(float volume)
    {
        effectsVolume = Mathf.Clamp01(volume);
        if (effectsSource != null) effectsSource.volume = effectsVolume;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            if (musicSource.isPlaying && musicSource.clip != null)
            {
                 musicSource.volume = musicVolume;
            }
        }
    }
}
