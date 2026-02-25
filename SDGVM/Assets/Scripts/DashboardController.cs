using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управляет Главным экраном (Дашбордом) приложения.
/// Отвечает за маршрутизацию пользователя к нужным функциям и показ статуса системы.
/// </summary>
public class DashboardController : MonoBehaviour
{
    [Header("UI Панели (Куда переходить)")]
    public GameObject dashboardPanel;
    public GameObject storyGenerationPanel;
    public GameObject npcDialoguePanel;
    public GameObject galleryPanel;
    
    [Header("Кнопки Дашборда")]
    public Button btnGoToStory;
    public Button btnGoToDialogue;
    public Button btnGoToGallery;
    public Button btnExit;
    
    [Header("Отображение Дашборда")]
    public TMP_Text textLlmStatusLabel;
    public TMP_Text textLlmStatusIcon;
    public TMP_Text textComfyStatusLabel;
    public TMP_Text textComfyStatusIcon;
    
    [Header("Метрики (демо для комиссии)")]
    public TMP_Text textRunsCount;
    public GameObject metricsPanel;

    void Start()
    {
        // Подключаем кнопки
        if (btnGoToStory) btnGoToStory.onClick.AddListener(() => SwitchToPanel(storyGenerationPanel));
        if (btnGoToDialogue) btnGoToDialogue.onClick.AddListener(() => SwitchToPanel(npcDialoguePanel));
        if (btnGoToGallery) btnGoToGallery.onClick.AddListener(() => SwitchToPanel(galleryPanel));
        if (btnExit) btnExit.onClick.AddListener(ExitApp);
        
        // По умолчанию показываем дашборд
        SwitchToPanel(dashboardPanel);
        
        // Запускаем периодическую проверку статусов
        InvokeRepeating(nameof(UpdateSystemStatus), 0f, 2f);
    }
    
    /// <summary>
    /// Переключает активную панель
    /// </summary>
    public void SwitchToPanel(GameObject targetPanel)
    {
        if (dashboardPanel) dashboardPanel.SetActive(false);
        if (storyGenerationPanel) storyGenerationPanel.SetActive(false);
        if (npcDialoguePanel) npcDialoguePanel.SetActive(false);
        if (galleryPanel) galleryPanel.SetActive(false);
        
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            
            // Если перешли в галерею - обновляем её
            if (targetPanel == galleryPanel)
            {
                var gallery = FindObjectOfType<GalleryController>();
                if (gallery) gallery.RefreshGallery();
            }
        }
    }
    
    /// <summary>
    /// Проверяет, запущен ли LLM и ComfyUI
    /// </summary>
    private void UpdateSystemStatus()
    {
        bool isLlmReady = FindObjectOfType<LLMUnity.LLMCharacter>() != null; // Грубая проверка
        bool isComfyReady = FindObjectOfType<ComfyUIManager>() != null; // Грубая проверка
        
        // Обновляем UI (текст и иконки) - используем "галочки" и "крестики"
        if (textLlmStatusIcon)
        {
            textLlmStatusIcon.text = isLlmReady ? "✅" : "⏳";
            textLlmStatusIcon.color = isLlmReady ? Color.green : Color.yellow;
        }
        
        if (textComfyStatusIcon)
        {
            textComfyStatusIcon.text = isComfyReady ? "✅" : "❌";
            textComfyStatusIcon.color = isComfyReady ? Color.green : Color.red;
        }
        
        // Можно показать кол-во запусков для солидности
        if (textRunsCount)
        {
            int runs = PlayerPrefs.GetInt("AppTotalRuns", 0);
            if (runs == 0) { PlayerPrefs.SetInt("AppTotalRuns", 1); runs = 1; }
            textRunsCount.text = runs.ToString();
        }
    }
    
    private void ExitApp()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
