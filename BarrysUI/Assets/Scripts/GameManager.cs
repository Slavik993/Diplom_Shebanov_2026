using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    public bool isGameActive = false;
    public int coins = 0;
    public int distance = 0;
    public float gameSpeed = 5f;
    
    [Header("UI References")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI distanceText;
    public GameObject gameOverPanel;
    public GameObject mainMenuPanel;
    public GameObject gamePanel;
    public GameObject shopPanel;
    
    [Header("Player Data")]
    public int totalCoins = 1000;
    public int[] unlockedJetPacks = {0};
    public int[] unlockedCostumes = {0};
    public int equippedJetPack = 0;
    public int equippedCostume = 0;
    
    private PlayerController player;
    private Spawner spawner;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = null;
            DontDestroyOnLoad(gameObject);
            LoadPlayerData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        spawner = FindObjectOfType<Spawner>();
        
        ShowMainMenu();
    }
    
    void Update()
    {
        if (isGameActive)
        {
            distance = Mathf.FloorToInt(Time.time * gameSpeed);
            UpdateUI();
        }
    }
    
    public void StartGame()
    {
        isGameActive = true;
        coins = 0;
        distance = 0;
        
        mainMenuPanel.SetActive(false);
        shopPanel.SetActive(false);
        gamePanel.SetActive(true);
        gameOverPanel.SetActive(false);
        
        if (player != null)
        {
            player.gameObject.SetActive(true);
            player.transform.position = Vector3.zero;
        }
        
        if (spawner != null)
        {
            spawner.StartSpawning();
        }
    }
    
    public void GameOver()
    {
        isGameActive = false;
        totalCoins += coins;
        SavePlayerData();
        
        gameOverPanel.SetActive(true);
        
        if (spawner != null)
        {
            spawner.StopSpawning();
        }
    }
    
    public void ShowMainMenu()
    {
        isGameActive = false;
        mainMenuPanel.SetActive(true);
        shopPanel.SetActive(false);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        if (player != null)
        {
            player.gameObject.SetActive(false);
        }
    }
    
    public void ShowShop()
    {
        mainMenuPanel.SetActive(false);
        shopPanel.SetActive(true);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }
    
    public void AddCoin()
    {
        coins++;
        totalCoins++;
        UpdateUI();
    }
    
    public void UpdateUI()
    {
        if (coinsText != null)
            coinsText.text = coins.ToString();
        
        if (distanceText != null)
            distanceText.text = distance.ToString() + "m";
    }
    
    public bool PurchaseItem(int cost)
    {
        if (totalCoins >= cost)
        {
            totalCoins -= cost;
            SavePlayerData();
            return true;
        }
        return false;
    }
    
    public void UnlockJetPack(int index)
    {
        if (!IsJetPackUnlocked(index))
        {
            var newUnlocked = new int[unlockedJetPacks.Length + 1];
            unlockedJetPacks.CopyTo(newUnlocked, 0);
            newUnlocked[unlockedJetPacks.Length] = index;
            unlockedJetPacks = newUnlocked;
            SavePlayerData();
        }
    }
    
    public void UnlockCostume(int index)
    {
        if (!IsCostumeUnlocked(index))
        {
            var newUnlocked = new int[unlockedCostumes.Length + 1];
            unlockedCostumes.CopyTo(newUnlocked, 0);
            newUnlocked[unlockedCostumes.Length] = index;
            unlockedCostumes = newUnlocked;
            SavePlayerData();
        }
    }
    
    public bool IsJetPackUnlocked(int index)
    {
        foreach (int unlocked in unlockedJetPacks)
        {
            if (unlocked == index) return true;
        }
        return false;
    }
    
    public bool IsCostumeUnlocked(int index)
    {
        foreach (int unlocked in unlockedCostumes)
        {
            if (unlocked == index) return true;
        }
        return false;
    }
    
    public void SavePlayerData()
    {
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.SetInt("EquippedJetPack", equippedJetPack);
        PlayerPrefs.SetInt("EquippedCostume", equippedCostume);
        
        PlayerPrefs.SetString("UnlockedJetPacks", string.Join(",", unlockedJetPacks));
        PlayerPrefs.SetString("UnlockedCostumes", string.Join(",", unlockedCostumes));
        
        PlayerPrefs.Save();
    }
    
    public void LoadPlayerData()
    {
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 1000);
        equippedJetPack = PlayerPrefs.GetInt("EquippedJetPack", 0);
        equippedCostume = PlayerPrefs.GetInt("EquippedCostume", 0);
        
        string jetPacksString = PlayerPrefs.GetString("UnlockedJetPacks", "0");
        string costumesString = PlayerPrefs.GetString("UnlockedCostumes", "0");
        
        if (!string.IsNullOrEmpty(jetPacksString))
        {
            var jetPackStrings = jetPacksString.Split(',');
            unlockedJetPacks = new int[jetPackStrings.Length];
            for (int i = 0; i < jetPackStrings.Length; i++)
            {
                int.TryParse(jetPackStrings[i], out unlockedJetPacks[i]);
            }
        }
        
        if (!string.IsNullOrEmpty(costumesString))
        {
            var costumeStrings = costumesString.Split(',');
            unlockedCostumes = new int[costumeStrings.Length];
            for (int i = 0; i < costumeStrings.Length; i++)
            {
                int.TryParse(costumeStrings[i], out unlockedCostumes[i]);
            }
        }
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
