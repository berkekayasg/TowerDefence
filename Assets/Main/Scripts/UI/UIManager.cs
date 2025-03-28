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
    // [SerializeField] private TextMeshProUGUI selectedTowerNameText; // Optional

    [Header("Build UI Elements")]
    [SerializeField] private Button standardTowerButton;
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

    public void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {lives}";
        }
    }

    public void UpdateCurrency(int currency)
    {
        if (currencyText != null)
        {
            currencyText.text = $"Coins: {currency}";
        }
    }

    public void UpdateWave(int waveNumber, int totalWaves)
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

        if (selectedTower == null)
        {
            upgradePanel.SetActive(false);
        }
        else
        {
            upgradePanel.SetActive(true);
            // if (selectedTowerNameText != null) selectedTowerNameText.text = selectedTower.name; // Optional

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

    public void SelectStandardTower()
    {
        if (towerSelectionPanel != null && towerSelectionPanel.activeSelf && BuildManager.Instance != null && BuildManager.Instance.standardTowerPrefab != null)
        {
            BuildManager.Instance.SelectTowerToBuild(BuildManager.Instance.standardTowerPrefab);
            ShowBuildStatus($"Selected: {BuildManager.Instance.standardTowerPrefab.name}");
        }
    }

    public void ShowBuildStatus(string message) // Added public modifier
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
        if (GameManager.Instance != null)
        {
            UpdateLives(GameManager.Instance.CurrentLives);
            UpdateCurrency(GameManager.Instance.CurrentCurrency);
        }
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (buildStatusText != null) buildStatusText.text = "";
        UpdateTimer(null);
        ShowUpgradeUI(null);

        if (standardTowerButton != null)
        {
            standardTowerButton.onClick.AddListener(SelectStandardTower);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonPressed);
        }
        else
        {
             Debug.LogWarning("Upgrade Button not assigned in UIManager Inspector.");
        }
    }

    private void OnUpgradeButtonPressed()
    {
        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.UpgradeSelectedTower();
        }
    }
}
