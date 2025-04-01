using UnityEngine;
using UnityEngine.UI;
using TMPro; // Added for TextMeshPro
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject towerSelectionPanel;

    [Header("Upgrade UI Elements")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellButtonText;

    [Header("Build UI Elements")]
    [SerializeField] private Transform towerButtonContainer;
    [SerializeField] private GameObject towerButtonPrefab;
    [SerializeField] private TextMeshProUGUI buildStatusText;


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

    // --- Event Subscription ---
    void OnEnable()
    {
        GameManager.OnCurrencyChanged += HandleCurrencyChanged;
        GameManager.OnLivesChanged += HandleLivesChanged;
        GameManager.OnStateChanged += HandleGameStateChanged;
    }

    void OnDisable()
    {
        GameManager.OnCurrencyChanged -= HandleCurrencyChanged;
        GameManager.OnLivesChanged -= HandleLivesChanged;
        GameManager.OnStateChanged -= HandleGameStateChanged;
        
    }
    // --- End Event Subscription ---


    // --- Event Handlers ---
    private void HandleCurrencyChanged(int newCurrencyValue)
    {
        if (currencyText != null)
        {
            currencyText.text = $"Coins: {newCurrencyValue}";
        }
    }

    private void HandleLivesChanged(int newLivesValue)
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {newLivesValue}";
        }
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Build:
                ShowTemporaryMessage("Build Phase");
                upgradePanel.SetActive(false); // Ensure upgrade panel is hidden in build phase
                break;
            case GameManager.GameState.Wave:
                upgradePanel.SetActive(false); // Hide upgrade panel during wave

                // Message showing wave number might be better handled here
                // but requires access to currentWaveNumber from GameManager.
                // Keeping the UpdateWave call in GameManager for now.
                break;
            case GameManager.GameState.GameOver:
                ShowGameOverScreen();
                break;
            case GameManager.GameState.Victory:
                ShowVictoryScreen();
                break;
            case GameManager.GameState.None:
                break;
        }
    }
    // --- End Event Handlers ---


    public void UpdateWave(int waveNumber, int totalWaves) // Keep for now
    {
        if (waveText != null)
         {
             if (waveNumber > totalWaves)
             {
                 waveText.text = $"Wave: {totalWaves}/{totalWaves}";
             }
             else
             {
                 waveText.text = $"Wave: {waveNumber}/{totalWaves}";
             }
        }
    }

    public void UpdateTimer(string timerMessage)
    {
        if (timerText != null)
        {
            if (!string.IsNullOrEmpty(timerMessage))
            {
                timerText.text = timerMessage;
                timerText.gameObject.SetActive(true);
            }
            else
            {
                timerText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowUpgradeUI(Tower selectedTower)
    {
        if (upgradePanel == null) return;

        // Only show upgrade UI during build phase
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Build)
        {
             upgradePanel.SetActive(false);
             return;
        }


        if (selectedTower == null)
        {
            upgradePanel.SetActive(false);
        }
        else
        {
            upgradePanel.SetActive(true);

            if (selectedTower.CanUpgrade())
            {
                if (upgradeCostText != null) upgradeCostText.text = $"Upgrade\n{selectedTower.GetUpgradeCost()} Coins";
                if (upgradeButton != null) upgradeButton.interactable = true;
            }
            else
            {
                if (upgradeCostText != null) upgradeCostText.text = "Max\nLevel";
            if (upgradeButton != null) upgradeButton.interactable = false;
            }

            // Update Sell Button
            if (sellButton != null)
            {
                sellButton.interactable = true;
                if (sellButtonText != null)
                {
                    int sellValue = selectedTower.GetCost() / 2;
                    sellButtonText.text = $"Sell\n{sellValue} Coins";
                }
            }
        }
    }

    public void ShowTemporaryMessage(string message, float duration = 2f)
    {
        if (messageText != null)
        {
            StartCoroutine(ShowMessageCoroutine(message, duration));
        }
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        messageText.text = message;
        yield return new WaitForSeconds(duration);
        messageText.text = "";
    }


    public void ShowGameOverScreen()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }
    }

    public void ShowVictoryScreen()
    {
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
        }
    }

    public void ShowTowerSelection(bool show)
    {
        if (towerSelectionPanel != null)
        {
            towerSelectionPanel.SetActive(show);
        }
    }

    public void ShowBuildStatus(string message)
    {
        if (buildStatusText != null)
        {
            buildStatusText.text = message;
            StartCoroutine(ClearBuildStatus());
        }
    }

    private IEnumerator ClearBuildStatus()
    {
        yield return new WaitForSeconds(2f);
        if (buildStatusText != null)
        {
            buildStatusText.text = "";
        }
    }

    void Start()
    {
        // Initial state is set by GameManager triggering events on Start
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (buildStatusText != null) buildStatusText.text = "";
        UpdateTimer(null); // Keep initial timer clear
        ShowUpgradeUI(null); // Ensure upgrade UI is hidden initially

        PopulateTowerButtons();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonPressed);
        }
        else
        {
             Debug.LogWarning("Upgrade Button not assigned in UIManager Inspector.");
        }

        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellButtonPressed);
        }
        else
        {
             Debug.LogWarning("Sell Button not assigned in UIManager Inspector.");
        }
    }

    private void OnUpgradeButtonPressed()
    {
        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEffect("UIClick"); // Using hardcoded name
        }

        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.UpgradeSelectedTower();
        }
    }

    private void OnSellButtonPressed()
    {
        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEffect("UIClick"); // Using hardcoded name
        }

        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.SellSelectedTower();
        }
    }

    private void PopulateTowerButtons()
    {
        if (towerButtonContainer == null || towerButtonPrefab == null || BuildManager.Instance == null || BuildManager.Instance.availableTowers == null)
        {
            Debug.LogError("UIManager cannot populate tower buttons. Check references in Inspector (towerButtonContainer, towerButtonPrefab) and ensure BuildManager has availableTowers list.", this);
            return;
        }

        foreach (Transform child in towerButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // Create a button for each available tower type
        foreach (TowerData towerData in BuildManager.Instance.availableTowers)
        {
            if (towerData == null || towerData.towerPrefab == null)
            {
                Debug.LogWarning("Skipping null TowerData or TowerData with null prefab in availableTowers list.");
                continue;
            }

            GameObject buttonGO = Instantiate(towerButtonPrefab, towerButtonContainer);
            Button button = buttonGO.GetComponentInChildren<Button>(true);
            Image iconImage = buttonGO.GetComponentInChildren<Image>(); // Assuming icon is on a child Image
            TextMeshProUGUI nameText = buttonGO.GetComponentInChildren<TextMeshProUGUI>(); // Assuming text is on a child TMP
            buttonGO.SetActive(true);
            if (button != null)
            {
                // Use a local variable capture to ensure the correct towerData is passed to the listener
                TowerData currentTowerData = towerData;
                button.onClick.AddListener(() => {
                    // Play sound
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayEffect("UIClick"); // Using hardcoded name
                    }
                    BuildManager.Instance.SelectTowerToBuild(currentTowerData);
                });

                if (iconImage != null && towerData.towerIcon != null)
                {
                    iconImage.sprite = towerData.towerIcon;
                }
                if (nameText != null)
                {
                    nameText.text = $"{towerData.towerName}\n{towerData.cost} Coins";
                }
            }
            else
            {
                Debug.LogError("Tower Button Prefab is missing Button component!", buttonGO);
            }
        }
    }
}
