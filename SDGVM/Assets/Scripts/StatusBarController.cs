using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Глобальный статус-бар для отображения прогресса и состояния системы.
/// Показывает: текущую операцию, прогресс, время, статус подсистем.
/// </summary>
public class StatusBarController : MonoBehaviour
{
    public static StatusBarController Instance { get; private set; }

    [Header("UI элементы")]
    public TMP_Text statusText;           // "Генерация истории... 65%"
    public Slider progressBar;            // Прогресс-бар
    public TMP_Text systemStatusText;     // "✅ LLM готов | ✅ ComfyUI готов"
    public Image progressFill;            // Заливка прогресс-бара (для цвета)
    public GameObject spinnerIcon;        // Анимированный спиннер (вкл/выкл)

    [Header("Цвета")]
    public Color colorIdle = new Color(0.3f, 0.8f, 0.3f);     // Зелёный
    public Color colorWorking = new Color(0.2f, 0.6f, 1f);     // Синий
    public Color colorError = new Color(1f, 0.3f, 0.3f);       // Красный
    public Color colorWarning = new Color(1f, 0.8f, 0.2f);     // Жёлтый

    private float operationStartTime;
    private bool isWorking = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetIdle();
        UpdateSystemStatus();
    }

    /// <summary>
    /// Показать что система готова к работе
    /// </summary>
    public void SetIdle()
    {
        isWorking = false;
        if (statusText != null) statusText.text = "Готов к работе";
        if (progressBar != null) progressBar.value = 0;
        if (spinnerIcon != null) spinnerIcon.SetActive(false);
        if (progressFill != null) progressFill.color = colorIdle;
    }

    /// <summary>
    /// Начать отслеживание операции
    /// </summary>
    public void StartOperation(string operationName)
    {
        isWorking = true;
        operationStartTime = Time.time;
        if (statusText != null) statusText.text = operationName + "...";
        if (progressBar != null) progressBar.value = 0;
        if (spinnerIcon != null) spinnerIcon.SetActive(true);
        if (progressFill != null) progressFill.color = colorWorking;
    }

    /// <summary>
    /// Обновить прогресс (0-1)
    /// </summary>
    public void UpdateProgress(float progress, string details = null)
    {
        if (progressBar != null) progressBar.value = Mathf.Clamp01(progress);
        
        if (statusText != null && details != null)
        {
            float elapsed = Time.time - operationStartTime;
            statusText.text = $"{details} ({progress:P0}) — {elapsed:F0}с";
        }
    }

    /// <summary>
    /// Операция завершена успешно
    /// </summary>
    public void CompleteOperation(string message = null)
    {
        isWorking = false;
        float elapsed = Time.time - operationStartTime;
        
        if (progressBar != null) progressBar.value = 1f;
        if (spinnerIcon != null) spinnerIcon.SetActive(false);
        if (progressFill != null) progressFill.color = colorIdle;
        
        if (statusText != null)
            statusText.text = message ?? $"Готово ({elapsed:F1}с)";
        
        // Автоматически вернуться к "Готов" через 5 секунд
        StartCoroutine(ResetAfterDelay(5f));
    }

    /// <summary>
    /// Показать ошибку
    /// </summary>
    public void ShowError(string errorMessage)
    {
        isWorking = false;
        if (statusText != null) statusText.text = "❌ " + errorMessage;
        if (progressFill != null) progressFill.color = colorError;
        if (spinnerIcon != null) spinnerIcon.SetActive(false);
        
        StartCoroutine(ResetAfterDelay(10f));
    }

    /// <summary>
    /// Показать предупреждение
    /// </summary>
    public void ShowWarning(string warningMessage)
    {
        if (statusText != null) statusText.text = "⚠️ " + warningMessage;
        if (progressFill != null) progressFill.color = colorWarning;
        
        StartCoroutine(ResetAfterDelay(8f));
    }

    /// <summary>
    /// Обновить статус подсистем (LLM, ComfyUI)
    /// </summary>
    public void UpdateSystemStatus()
    {
        if (systemStatusText == null) return;

        string llmStatus = FindObjectOfType<LLMUnity.LLMCharacter>() != null ? "✅ LLM" : "❌ LLM";
        string comfyStatus = FindObjectOfType<ComfyUIManager>() != null ? "✅ ComfyUI" : "❌ ComfyUI";
        
        systemStatusText.text = $"{llmStatus} | {comfyStatus}";
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isWorking)
        {
            SetIdle();
            UpdateSystemStatus();
        }
    }
}
