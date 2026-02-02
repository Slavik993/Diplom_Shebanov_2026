using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// Автоматический анализатор личности NPC
/// Определяет признаки "становления личности" через метрики
/// </summary>
public class PersonalityAnalyzer : MonoBehaviour
{
    [Header("UI для отображения результатов")]
    public TMP_Text analysisResultText;
    public Slider consistencySlider;
    public Slider emotionalitySlider;
    public Slider personalityScoreSlider;

    [Header("Настройки анализа")]
    [Range(0, 1)] public float personalityThreshold = 0.6f;

    // Ключевые слова для анализа
    private static readonly string[] EmotionalWords = {
        "хочу", "хотел", "желаю", "мечтаю", "люблю", "ненавижу",
        "боюсь", "страшно", "тревожно", "волнуюсь",
        "рад", "счастлив", "грустно", "печально", "обидно",
        "злюсь", "раздражает", "бесит",
        "надеюсь", "верю", "сомневаюсь",
        "должен", "обязан", "нельзя", "не могу", "не получается"
    };

    private static readonly string[] ConflictIndicators = {
        "но", "однако", "хотя", "несмотря на", "с другой стороны",
        "хочу, но", "но не могу", "должен, но", "хотел бы, но",
        "вместо", "приходится", "заставляют"
    };

    private static readonly string[] AvoidancePatterns = {
        "не будем об этом", "давай сменим тему", "это не важно",
        "не знаю", "сложно сказать", "может быть"
    };

    private List<string> dialogueHistory = new List<string>();
    private string currentPersonalityDescription = "";

    private static PersonalityAnalyzer _instance;
    public static PersonalityAnalyzer Instance => _instance;

    void Awake()
    {
        if (_instance == null) _instance = this;
    }

    /// <summary>
    /// Устанавливает текущее описание личности для анализа
    /// </summary>
    public void SetPersonalityDescription(string description)
    {
        currentPersonalityDescription = description ?? "";
        dialogueHistory.Clear();
    }

    /// <summary>
    /// Анализирует ответ NPC и возвращает метрики
    /// </summary>
    public PersonalityMetrics AnalyzeResponse(string npcResponse)
    {
        if (string.IsNullOrEmpty(npcResponse))
            return new PersonalityMetrics();

        dialogueHistory.Add(npcResponse);

        var metrics = new PersonalityMetrics();
        
        // 1. Анализ эмоциональности
        metrics.Emotionality = CalculateEmotionality(npcResponse);
        
        // 2. Анализ консистентности (упоминание конфликта)
        metrics.Consistency = CalculateConsistency(npcResponse);
        
        // 3. Анализ избегания/разрешения
        metrics.ConflictEngagement = CalculateConflictEngagement(npcResponse);
        
        // 4. Общий балл личности
        metrics.PersonalityScore = (metrics.Emotionality + metrics.Consistency + metrics.ConflictEngagement) / 3f;
        
        // 5. Определение наличия личности
        metrics.HasPersonality = metrics.PersonalityScore >= personalityThreshold;

        // Обновляем UI
        UpdateUI(metrics);
        
        // Логируем
        LogMetrics(npcResponse, metrics);

        return metrics;
    }

    private float CalculateEmotionality(string text)
    {
        string lower = text.ToLower();
        int emotionalCount = EmotionalWords.Count(word => lower.Contains(word));
        
        // Нормализуем: 0 слов = 0, 3+ слов = 1
        return Mathf.Clamp01(emotionalCount / 3f);
    }

    private float CalculateConsistency(string text)
    {
        if (string.IsNullOrEmpty(currentPersonalityDescription))
            return 0f;

        string lower = text.ToLower();
        string descLower = currentPersonalityDescription.ToLower();

        // Извлекаем ключевые слова из описания личности
        var keywords = ExtractKeywords(descLower);
        
        int matches = keywords.Count(kw => lower.Contains(kw));
        
        // Нормализуем
        return keywords.Count > 0 ? Mathf.Clamp01((float)matches / keywords.Count) : 0f;
    }

    private float CalculateConflictEngagement(string text)
    {
        string lower = text.ToLower();

        // Проверяем наличие конфликтных конструкций
        int conflictCount = ConflictIndicators.Count(ci => lower.Contains(ci));
        
        // Проверяем избегание
        int avoidanceCount = AvoidancePatterns.Count(ap => lower.Contains(ap));

        // Конфликт = хорошо (показывает личность), избегание = плохо
        float conflictScore = Mathf.Clamp01(conflictCount / 2f);
        float avoidancePenalty = Mathf.Clamp01(avoidanceCount / 2f) * 0.5f;

        return Mathf.Clamp01(conflictScore - avoidancePenalty);
    }

    private List<string> ExtractKeywords(string text)
    {
        // Простое извлечение: слова длиннее 4 букв
        return Regex.Matches(text, @"\b[а-яё]{4,}\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(w => !IsStopWord(w))
            .Distinct()
            .Take(10)
            .ToList();
    }

    private bool IsStopWord(string word)
    {
        string[] stopWords = { "этот", "который", "потому", "такой", "очень", "сейчас", "тоже", "можно" };
        return stopWords.Contains(word);
    }

    private void UpdateUI(PersonalityMetrics metrics)
    {
        if (consistencySlider != null)
            consistencySlider.value = metrics.Consistency;

        if (emotionalitySlider != null)
            emotionalitySlider.value = metrics.Emotionality;

        if (personalityScoreSlider != null)
            personalityScoreSlider.value = metrics.PersonalityScore;

        if (analysisResultText != null)
        {
            string status = metrics.HasPersonality ? 
                "<color=green>✓ ЛИЧНОСТЬ ОБНАРУЖЕНА</color>" : 
                "<color=orange>○ Личность не выражена</color>";

            analysisResultText.text = $@"{status}

Консистентность: {metrics.Consistency:P0}
Эмоциональность: {metrics.Emotionality:P0}
Вовлечённость в конфликт: {metrics.ConflictEngagement:P0}

Общий балл: {metrics.PersonalityScore:P0}";
        }
    }

    private void LogMetrics(string response, PersonalityMetrics metrics)
    {
        Debug.Log($@"[PersonalityAnalyzer] 
Ответ: {response.Substring(0, Mathf.Min(50, response.Length))}...
Консистентность: {metrics.Consistency:F2}
Эмоциональность: {metrics.Emotionality:F2}
Конфликт: {metrics.ConflictEngagement:F2}
Личность: {(metrics.HasPersonality ? "ДА" : "НЕТ")}");
    }

    /// <summary>
    /// Получает сводку по всем диалогам сессии
    /// </summary>
    public string GetSessionSummary()
    {
        if (dialogueHistory.Count == 0)
            return "Диалогов пока не было";

        float avgEmotionality = dialogueHistory.Average(d => CalculateEmotionality(d));
        float avgConsistency = dialogueHistory.Average(d => CalculateConsistency(d));

        return $@"Сессия: {dialogueHistory.Count} реплик
Средняя эмоциональность: {avgEmotionality:P0}
Средняя консистентность: {avgConsistency:P0}";
    }
}

/// <summary>
/// Структура метрик личности
/// </summary>
[System.Serializable]
public struct PersonalityMetrics
{
    public float Consistency;        // 0-1: соответствие заданному конфликту
    public float Emotionality;       // 0-1: эмоциональная выраженность
    public float ConflictEngagement; // 0-1: вовлечённость в конфликт
    public float PersonalityScore;   // 0-1: общий балл личности
    public bool HasPersonality;      // Пороговое определение
}
