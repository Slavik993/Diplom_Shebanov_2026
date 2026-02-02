using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// Анализатор качества и связности текста
/// Определяет "странные" или бессмысленные тексты
/// </summary>
public class TextQualityAnalyzer : MonoBehaviour
{
    [Header("Пороги качества")]
    [Range(0, 1)] public float coherenceThreshold = 0.5f;
    [Range(0, 1)] public float adequacyThreshold = 0.6f;

    // Паттерны бессмыслицы (странные комбинации)
    private static readonly string[] NonsensePatterns = {
        @"башня\s+пиццы", @"пицца\s+башня",
        @"марсиан\w*\s+(?:чудищ|монстр|существ)",
        @"(?:кот|собака|животное)\s+(?:президент|министр|царь)",
        @"(?:дерево|камень|вода)\s+(?:говорил|сказал|кричал)",
        @"(?:телефон|компьютер|интернет)\s+(?:в\s+средневеков|в\s+древн)",
        @"\d{4}\s*год\w*\s+(?:дракон|маг|волшебн)",
    };

    // Слова-маркеры низкого качества
    private static readonly string[] LowQualityMarkers = {
        "бла-бла", "и так далее", "ну типа", "короче",
        "...", "???", "!!!", "хз", "фиг знает"
    };

    // Слова для проверки тематической связности
    private static readonly Dictionary<string, string[]> ThematicClusters = new()
    {
        ["университет"] = new[] { "студент", "учёба", "лекция", "экзамен", "преподаватель", "сессия", "диплом" },
        ["сказка"] = new[] { "герой", "царь", "принцесса", "дракон", "меч", "замок", "волшебн", "магия" },
        ["история"] = new[] { "век", "эпоха", "царь", "война", "государство", "народ", "культура" },
        ["наука"] = new[] { "исследован", "теория", "эксперимент", "гипотез", "метод", "анализ" }
    };

    private static TextQualityAnalyzer _instance;
    public static TextQualityAnalyzer Instance => _instance;

    void Awake()
    {
        if (_instance == null) _instance = this;
    }

    /// <summary>
    /// Анализирует качество текста и возвращает метрики
    /// </summary>
    public TextQualityMetrics AnalyzeText(string text, string originalPrompt = "")
    {
        if (string.IsNullOrEmpty(text))
            return new TextQualityMetrics { IsAdequate = false };

        var metrics = new TextQualityMetrics();
        string lower = text.ToLower();

        // 1. Проверка на явную бессмыслицу
        metrics.NonsenseScore = CalculateNonsenseScore(lower);

        // 2. Проверка грамматической связности
        metrics.GrammarScore = CalculateGrammarScore(text);

        // 3. Проверка тематической связности (если есть промпт)
        metrics.ThematicCoherence = CalculateThematicCoherence(lower, originalPrompt.ToLower());

        // 4. Проверка структуры (начало, развитие, конец)
        metrics.StructureScore = CalculateStructureScore(text);

        // 5. Общий балл качества
        metrics.OverallQuality = (
            (1f - metrics.NonsenseScore) * 0.4f +  // Отсутствие бессмыслицы — важно
            metrics.GrammarScore * 0.2f +
            metrics.ThematicCoherence * 0.2f +
            metrics.StructureScore * 0.2f
        );

        // 6. Определение адекватности
        metrics.IsAdequate = metrics.OverallQuality >= adequacyThreshold && metrics.NonsenseScore < 0.3f;

        // 7. Список найденных проблем
        metrics.Issues = DetectIssues(text, metrics);

        LogMetrics(text, metrics);

        return metrics;
    }

    private float CalculateNonsenseScore(string text)
    {
        int nonsenseCount = 0;

        foreach (var pattern in NonsensePatterns)
        {
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
                nonsenseCount++;
        }

        foreach (var marker in LowQualityMarkers)
        {
            if (text.Contains(marker))
                nonsenseCount++;
        }

        // Проверка на повторы слов подряд (признак галлюцинации)
        var words = Regex.Matches(text, @"\b[а-яё]+\b").Cast<Match>().Select(m => m.Value).ToList();
        for (int i = 1; i < words.Count; i++)
        {
            if (words[i] == words[i - 1] && words[i].Length > 3)
                nonsenseCount++;
        }

        return Mathf.Clamp01(nonsenseCount / 3f);
    }

    private float CalculateGrammarScore(string text)
    {
        float score = 1f;

        // Проверка на заглавную букву в начале
        if (!char.IsUpper(text.TrimStart()[0]))
            score -= 0.2f;

        // Проверка на точку/вопрос/восклицание в конце
        string trimmed = text.TrimEnd();
        if (!trimmed.EndsWith(".") && !trimmed.EndsWith("!") && !trimmed.EndsWith("?"))
            score -= 0.2f;

        // Проверка на слишком длинные предложения (>50 слов без точки)
        var sentences = Regex.Split(text, @"[.!?]");
        foreach (var s in sentences)
        {
            int wordCount = Regex.Matches(s, @"\b\w+\b").Count;
            if (wordCount > 50) score -= 0.1f;
        }

        // Проверка на незаконченные предложения
        if (text.TrimEnd().EndsWith(",") || text.TrimEnd().EndsWith(" и"))
            score -= 0.3f;

        return Mathf.Clamp01(score);
    }

    private float CalculateThematicCoherence(string text, string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
            return 0.5f; // Нейтральный балл без промпта

        // Определяем тему по промпту
        string detectedTheme = null;
        int maxMatches = 0;

        foreach (var cluster in ThematicClusters)
        {
            int matches = cluster.Value.Count(word => prompt.Contains(word) || text.Contains(word));
            if (matches > maxMatches)
            {
                maxMatches = matches;
                detectedTheme = cluster.Key;
            }
        }

        if (detectedTheme == null)
            return 0.5f;

        // Проверяем, использует ли текст слова из той же темы
        int thematicWords = ThematicClusters[detectedTheme].Count(word => text.Contains(word));

        return Mathf.Clamp01(thematicWords / 3f);
    }

    private float CalculateStructureScore(string text)
    {
        float score = 0f;

        // Есть ли несколько предложений?
        int sentenceCount = Regex.Matches(text, @"[.!?]").Count;
        if (sentenceCount >= 2) score += 0.3f;
        if (sentenceCount >= 4) score += 0.2f;

        // Есть ли абзацы или структура?
        if (text.Contains("\n") || text.Length > 200) score += 0.2f;

        // Есть ли вводные слова (показатель связности)?
        string[] connectors = { "поэтому", "однако", "кроме того", "таким образом", "в итоге", "затем", "после этого" };
        if (connectors.Any(c => text.ToLower().Contains(c))) score += 0.3f;

        return Mathf.Clamp01(score);
    }

    private List<string> DetectIssues(string text, TextQualityMetrics metrics)
    {
        var issues = new List<string>();

        if (metrics.NonsenseScore > 0.3f)
            issues.Add("Обнаружены нелогичные сочетания слов");

        if (metrics.GrammarScore < 0.6f)
            issues.Add("Проблемы с грамматикой или структурой предложений");

        if (metrics.ThematicCoherence < 0.4f)
            issues.Add("Текст не соответствует заданной теме");

        if (text.Length < 50)
            issues.Add("Текст слишком короткий");

        // Проверка на обрезанный текст
        if (!text.TrimEnd().EndsWith(".") && !text.TrimEnd().EndsWith("!") && !text.TrimEnd().EndsWith("?"))
            issues.Add("Текст обрезан (не завершён)");

        return issues;
    }

    private void LogMetrics(string text, TextQualityMetrics metrics)
    {
        string preview = text.Length > 60 ? text.Substring(0, 60) + "..." : text;
        string status = metrics.IsAdequate ? "✓ АДЕКВАТЕН" : "✗ ПРОБЛЕМЫ";

        Debug.Log($@"[TextQuality] {status}
Текст: {preview}
Бессмыслица: {metrics.NonsenseScore:P0}
Грамматика: {metrics.GrammarScore:P0}
Тематика: {metrics.ThematicCoherence:P0}
Структура: {metrics.StructureScore:P0}
Общий балл: {metrics.OverallQuality:P0}
Проблемы: {string.Join(", ", metrics.Issues)}");
    }

    /// <summary>
    /// Пытается улучшить текст, убирая явные проблемы
    /// </summary>
    public string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        string result = text;

        // Убираем повторяющиеся слова подряд
        result = Regex.Replace(result, @"\b(\w+)\s+\1\b", "$1", RegexOptions.IgnoreCase);

        // Убираем множественные пробелы
        result = Regex.Replace(result, @"\s+", " ");

        // Добавляем точку в конец, если её нет
        result = result.Trim();
        if (!result.EndsWith(".") && !result.EndsWith("!") && !result.EndsWith("?"))
            result += ".";

        return result;
    }
}

/// <summary>
/// Структура метрик качества текста
/// </summary>
[System.Serializable]
public struct TextQualityMetrics
{
    public float NonsenseScore;      // 0-1: степень бессмыслицы (меньше = лучше)
    public float GrammarScore;       // 0-1: грамматическая корректность
    public float ThematicCoherence;  // 0-1: соответствие теме
    public float StructureScore;     // 0-1: наличие структуры
    public float OverallQuality;     // 0-1: общий балл качества
    public bool IsAdequate;          // Пороговое определение
    public List<string> Issues;      // Список проблем
}
