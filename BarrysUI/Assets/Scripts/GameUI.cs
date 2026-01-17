using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Game HUD")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI speedText;
    
    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalCoinsText;
    public TextMeshProUGUI finalDistanceText;
    public TextMeshProUGUI newHighScoreText;
    public Button restartButton;
    public Button mainMenuButton;
    
    [Header("Pause Menu")]
    public GameObject pausePanel;
    public Button pauseButton;
    public Button resumeButton;
    public Button pauseMainMenuButton;
    
    private bool isPaused = false;
    
    void Start()
    {
        SetupButtons();
        HideGameOverPanel();
        HidePausePanel();
    }
    
    private void SetupButtons()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
        
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        
        if (pauseMainMenuButton != null)
            pauseMainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }
    
    void Update()
    {
        if (GameManager.Instance.isGameActive)
        {
            UpdateGameHUD();
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }
    }
    
    private void UpdateGameHUD()
    {
        if (coinsText != null)
            coinsText.text = GameManager.Instance.coins.ToString();
        
        if (distanceText != null)
            distanceText.text = GameManager.Instance.distance.ToString() + "m";
        
        if (speedText != null)
            speedText.text = "Speed: " + GameManager.Instance.gameSpeed.ToString("F1") + "x";
    }
    
    public void ShowGameOverPanel()
    {
        gameOverPanel.SetActive(true);
        
        if (finalCoinsText != null)
            finalCoinsText.text = "Coins: " + GameManager.Instance.coins.ToString();
        
        if (finalDistanceText != null)
            finalDistanceText.text = "Distance: " + GameManager.Instance.distance.ToString() + "m";
        
        CheckHighScore();
    }
    
    private void HideGameOverPanel()
    {
        gameOverPanel.SetActive(false);
    }
    
    private void CheckHighScore()
    {
        int currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
        
        if (GameManager.Instance.distance > currentHighScore)
        {
            PlayerPrefs.SetInt("HighScore", GameManager.Instance.distance);
            PlayerPrefs.Save();
            
            if (newHighScoreText != null)
            {
                newHighScoreText.gameObject.SetActive(true);
                newHighScoreText.text = "NEW HIGH SCORE!";
            }
        }
        else
        {
            if (newHighScoreText != null)
            {
                newHighScoreText.gameObject.SetActive(false);
            }
        }
    }
    
    private void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        
        AudioSystem.Instance.PlayButtonClickSound();
    }
    
    private void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        
        AudioSystem.Instance.PlayButtonClickSound();
    }
    
    private void HidePausePanel()
    {
        pausePanel.SetActive(false);
    }
    
    private void OnRestartClicked()
    {
        AudioSystem.Instance.PlayButtonClickSound();
        Time.timeScale = 1f;
        GameManager.Instance.RestartGame();
    }
    
    private void OnMainMenuClicked()
    {
        AudioSystem.Instance.PlayButtonClickSound();
        Time.timeScale = 1f;
        GameManager.Instance.ShowMainMenu();
    }
    
    private void OnPauseClicked()
    {
        TogglePause();
    }
    
    private void OnResumeClicked()
    {
        ResumeGame();
    }
}
