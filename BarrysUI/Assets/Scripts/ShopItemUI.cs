using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI descriptionText;
    public Button purchaseButton;
    public Button equipButton;
    public GameObject equippedIndicator;
    
    private JetPackItem currentJetPackItem;
    private CostumeItem currentCostumeItem;
    private bool isJetPackItem = false;
    
    public void SetupJetPackItem(JetPackItem jetPackItem, bool isUnlocked, bool isEquipped)
    {
        currentJetPackItem = jetPackItem;
        isJetPackItem = true;
        
        itemIcon.sprite = jetPackItem.icon;
        itemNameText.text = jetPackItem.name;
        descriptionText.text = jetPackItem.description;
        
        UpdateUIState(isUnlocked, isEquipped, jetPackItem.price);
    }
    
    public void SetupCostumeItem(CostumeItem costumeItem, bool isUnlocked, bool isEquipped)
    {
        currentCostumeItem = costumeItem;
        isJetPackItem = false;
        
        itemIcon.sprite = costumeItem.icon;
        itemNameText.text = costumeItem.name;
        descriptionText.text = costumeItem.description;
        
        UpdateUIState(isUnlocked, isEquipped, costumeItem.price);
    }
    
    private void UpdateUIState(bool isUnlocked, bool isEquipped, int price)
    {
        equippedIndicator.SetActive(isEquipped);
        
        if (isEquipped)
        {
            purchaseButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(false);
        }
        else if (isUnlocked)
        {
            purchaseButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(true);
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipClicked);
        }
        else
        {
            purchaseButton.gameObject.SetActive(true);
            equipButton.gameObject.SetActive(false);
            priceText.text = price.ToString() + " coins";
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
    }
    
    private void OnPurchaseClicked()
    {
        var shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            if (isJetPackItem)
            {
                shopManager.PurchaseJetPack(currentJetPackItem.index);
            }
            else
            {
                shopManager.PurchaseCostume(currentCostumeItem.index);
            }
        }
    }
    
    private void OnEquipClicked()
    {
        var shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            if (isJetPackItem)
            {
                shopManager.EquipJetPack(currentJetPackItem.index);
            }
            else
            {
                shopManager.EquipCostume(currentCostumeItem.index);
            }
        }
    }
}
