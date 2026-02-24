using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Глобальный обработчик ошибок — предотвращает краши во время демо.
/// Показывает понятные сообщения вместо падений.
/// </summary>
public class ErrorHandler : MonoBehaviour
{
    public static ErrorHandler Instance { get; private set; }

    [Header("UI Элементы (опционально)")]
    public GameObject errorPanel;          // Панель с ошибкой
    public TMP_Text errorMessageText;      // Текст ошибки
    public Button errorDismissButton;      // Кнопка "ОК"
    public Button errorRetryButton;        // Кнопка "Повторить"

    private System.Action retryAction;

    void Awake()
    {
        Instance = this;
        // Перехватываем все необработанные исключения
        Application.logMessageReceived += HandleLog;
    }

    void Start()
    {
        if (errorPanel != null) errorPanel.SetActive(false);
        if (errorDismissButton != null) errorDismissButton.onClick.AddListener(DismissError);
        if (errorRetryButton != null) errorRetryButton.onClick.AddListener(RetryLastAction);
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    /// <summary>
    /// Перехват логов ошибок Unity
    /// </summary>
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            // Не показываем всё подряд — только критические
            if (logString.Contains("NullReferenceException") ||
                logString.Contains("IndexOutOfRange") ||
                logString.Contains("Connection refused") ||
                logString.Contains("timeout"))
            {
                string friendlyMessage = GetFriendlyMessage(logString);
                ShowError(friendlyMessage);
            }
        }
    }

    /// <summary>
    /// Преобразует техническую ошибку в понятное сообщение
    /// </summary>
    private string GetFriendlyMessage(string technicalError)
    {
        if (technicalError.Contains("Connection refused") || technicalError.Contains("timeout"))
            return "Не удалось подключиться к серверу генерации изображений. Проверьте, запущен ли ComfyUI.";
        
        if (technicalError.Contains("NullReferenceException"))
            return "Произошла внутренняя ошибка. Попробуйте перезапустить приложение.";
        
        if (technicalError.Contains("OutOfMemory"))
            return "Недостаточно памяти. Закройте другие программы и попробуйте снова.";
        
        if (technicalError.Contains("model") || technicalError.Contains("Model"))
            return "Ошибка загрузки модели ИИ. Проверьте наличие файлов модели.";
        
        return "Произошла ошибка. Попробуйте повторить действие.";
    }

    /// <summary>
    /// Показать ошибку пользователю
    /// </summary>
    public void ShowError(string message, System.Action retry = null)
    {
        retryAction = retry;
        
        Debug.LogWarning($"[ErrorHandler] {message}");
        
        if (errorPanel != null && errorMessageText != null)
        {
            errorMessageText.text = message;
            errorPanel.SetActive(true);
            if (errorRetryButton != null)
                errorRetryButton.gameObject.SetActive(retry != null);
        }
        
        // Также обновляем статус-бар
        if (StatusBarController.Instance != null)
            StatusBarController.Instance.ShowError(message);
    }

    /// <summary>
    /// Показать предупреждение (менее критично)
    /// </summary>
    public void ShowWarning(string message)
    {
        Debug.LogWarning($"[Warning] {message}");
        
        if (StatusBarController.Instance != null)
            StatusBarController.Instance.ShowWarning(message);
    }

    /// <summary>
    /// Проверка готовности системы (вызывать перед генерацией)
    /// </summary>
    public bool CheckSystemReady()
    {
        // Проверяем LLM
        var llm = FindObjectOfType<LLMUnity.LLMCharacter>();
        if (llm == null)
        {
            ShowError("LLM модель не загружена. Проверьте настройки LLMCharacter в сцене.");
            return false;
        }

        return true;
    }

    public void DismissError()
    {
        if (errorPanel != null) errorPanel.SetActive(false);
    }

    public void RetryLastAction()
    {
        DismissError();
        retryAction?.Invoke();
    }
}
