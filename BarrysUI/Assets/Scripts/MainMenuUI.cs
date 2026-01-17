using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    public Button playButton;
    public Button shopButton;
    public Button settingsButton;
    public Button quitButton;
    
    [Header("UI Elements")]
    public TextMeshProUGUI totalCoinsText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI gameTitle;
    
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    
    [Header("Settings")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Button backButton;
    
    void Start()
    {
        SetupButtons();
        UpdateUI();
    }
    
    private void SetupButtons()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        shopButton.onClick.AddListener(OnShopClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        backButton.onClick.AddListener(OnBackClicked);
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }
    
    private void UpdateUI()
    {
        if (totalCoinsText != null)
        {
            totalCoinsText.text = "Coins: " + GameManager.Instance.totalCoins.ToString();
        }
        
        if (highScoreText != null)
        {
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            highScoreText.text = "High Score: " + highScore.ToString() + "m";
        }
    }
    
    private void OnPlayClicked()
    {
        AudioSystem.Instance.PlayButtonClickSound();
        GameManager.Instance.StartGame();
    }
    
    private void OnShopClicked()
    {
        AudioSystem.Instance.PlayButtonClickSound();
        GameManager.Instance.ShowShop();
    }
    
    private void OnSettingsClicked()
    {
        AudioSystem.Instance.PlayButtonClickSound();
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    private void OnQuitClicked()
    {
        AudioSystem.Instance.PlayButtonClickSound();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void OnBackClicked()
    {
        AudioSystem.Instance.PlayButtonClickSound();
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        AudioSystem.Instance.SetMusicVolume(value);
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        AudioSystem.Instance.SetSFXVolume(value);
    }
}
