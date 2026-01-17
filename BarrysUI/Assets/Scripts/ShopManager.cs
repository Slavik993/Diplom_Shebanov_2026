using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Panels")]
    public GameObject jetPackShopPanel;
    public GameObject costumeShopPanel;
    public Button jetPackTabButton;
    public Button costumeTabButton;
    
    [Header("JetPack Shop")]
    public Transform jetPackContent;
    public GameObject jetPackItemPrefab;
    public JetPackItem[] jetPackItems;
    
    [Header("Costume Shop")]
    public Transform costumeContent;
    public GameObject costumeItemPrefab;
    public CostumeItem[] costumeItems;
    
    [Header("UI Elements")]
    public TextMeshProUGUI totalCoinsText;
    public Button backButton;
    
    private int currentTab = 0;
    
    void Start()
    {
        InitializeShop();
        SetupButtons();
        UpdateCoinsDisplay();
    }
    
    private void InitializeShop()
    {
        CreateJetPackItems();
        CreateCostumeItems();
        
        ShowJetPackTab();
    }
    
    private void SetupButtons()
    {
        jetPackTabButton.onClick.AddListener(ShowJetPackTab);
        costumeTabButton.onClick.AddListener(ShowCostumeTab);
        backButton.onClick.AddListener(() => GameManager.Instance.ShowMainMenu());
    }
    
    private void ShowJetPackTab()
    {
        currentTab = 0;
        jetPackShopPanel.SetActive(true);
        costumeShopPanel.SetActive(false);
        
        jetPackTabButton.interactable = false;
        costumeTabButton.interactable = true;
    }
    
    private void ShowCostumeTab()
    {
        currentTab = 1;
        jetPackShopPanel.SetActive(false);
        costumeShopPanel.SetActive(true);
        
        jetPackTabButton.interactable = true;
        costumeTabButton.interactable = false;
    }
    
    private void CreateJetPackItems()
    {
        foreach (Transform child in jetPackContent)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var jetPackItem in jetPackItems)
        {
            GameObject itemGO = Instantiate(jetPackItemPrefab, jetPackContent);
            ShopItemUI itemUI = itemGO.GetComponent<ShopItemUI>();
            
            if (itemUI != null)
            {
                bool isUnlocked = GameManager.Instance.IsJetPackUnlocked(jetPackItem.index);
                bool isEquipped = jetPackItem.index == GameManager.Instance.equippedJetPack;
                
                itemUI.SetupJetPackItem(jetPackItem, isUnlocked, isEquipped);
            }
        }
    }
    
    private void CreateCostumeItems()
    {
        foreach (Transform child in costumeContent)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var costumeItem in costumeItems)
        {
            GameObject itemGO = Instantiate(costumeItemPrefab, costumeContent);
            ShopItemUI itemUI = itemGO.GetComponent<ShopItemUI>();
            
            if (itemUI != null)
            {
                bool isUnlocked = GameManager.Instance.IsCostumeUnlocked(costumeItem.index);
                bool isEquipped = costumeItem.index == GameManager.Instance.equippedCostume;
                
                itemUI.SetupCostumeItem(costumeItem, isUnlocked, isEquipped);
            }
        }
    }
    
    public void PurchaseJetPack(int index)
    {
        var jetPackItem = System.Array.Find(jetPackItems, item => item.index == index);
        if (jetPackItem != null)
        {
            if (GameManager.Instance.PurchaseItem(jetPackItem.price))
            {
                GameManager.Instance.UnlockJetPack(index);
                UpdateCoinsDisplay();
                CreateJetPackItems();
                
                AudioSystem.Instance.PlayPurchaseSound();
            }
        }
    }
    
    public void EquipJetPack(int index)
    {
        if (GameManager.Instance.IsJetPackUnlocked(index))
        {
            GameManager.Instance.equippedJetPack = index;
            GameManager.Instance.SavePlayerData();
            CreateJetPackItems();
            
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.EquipJetPack(index);
            }
            
            AudioSystem.Instance.PlayEquipSound();
        }
    }
    
    public void PurchaseCostume(int index)
    {
        var costumeItem = System.Array.Find(costumeItems, item => item.index == index);
        if (costumeItem != null)
        {
            if (GameManager.Instance.PurchaseItem(costumeItem.price))
            {
                GameManager.Instance.UnlockCostume(index);
                UpdateCoinsDisplay();
                CreateCostumeItems();
                
                AudioSystem.Instance.PlayPurchaseSound();
            }
        }
    }
    
    public void EquipCostume(int index)
    {
        if (GameManager.Instance.IsCostumeUnlocked(index))
        {
            GameManager.Instance.equippedCostume = index;
            GameManager.Instance.SavePlayerData();
            CreateCostumeItems();
            
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.ChangeCostume(costumeItems[index].costumeSprite);
            }
            
            AudioSystem.Instance.PlayEquipSound();
        }
    }
    
    public void UpdateCoinsDisplay()
    {
        if (totalCoinsText != null)
        {
            totalCoinsText.text = GameManager.Instance.totalCoins.ToString();
        }
    }
}

[System.Serializable]
public class JetPackItem
{
    public int index;
    public string name;
    public int price;
    public Sprite icon;
    public string description;
    public float flyForce;
}

[System.Serializable]
public class CostumeItem
{
    public int index;
    public string name;
    public int price;
    public Sprite icon;
    public Sprite costumeSprite;
    public string description;
}
