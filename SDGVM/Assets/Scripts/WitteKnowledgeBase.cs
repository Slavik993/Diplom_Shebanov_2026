using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Загружает witte_dataset.jsonl и предоставляет RAG-подобный поиск по ключевым словам.
/// Вместо дообучения модели — вставляем релевантные фрагменты прямо в промпт.
/// </summary>
public class WitteKnowledgeBase : MonoBehaviour
{
    public static WitteKnowledgeBase Instance { get; private set; }

    [Header("Путь к датасету")]
    [Tooltip("Файл witte_dataset.jsonl в StreamingAssets")]
    public string datasetFileName = "witte_dataset.jsonl";

    [Header("Настройки RAG")]
    [Tooltip("Максимум фрагментов для вставки в промпт")]
    public int maxFragments = 3;
    [Tooltip("Максимум символов на один фрагмент")]
    public int maxCharsPerFragment = 500;

    private List<WitteEntry> entries = new List<WitteEntry>();
    private bool isLoaded = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        LoadDataset();
    }

    /// <summary>
    /// Загрузить датасет из StreamingAssets
    /// </summary>
    void LoadDataset()
    {
        string path = Path.Combine(Application.streamingAssetsPath, datasetFileName);

        if (!File.Exists(path))
        {
            // Попробуем из корня проекта
            path = Path.Combine(Application.dataPath, "..", datasetFileName);
        }

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[WitteKnowledgeBase] Датасет не найден: {datasetFileName}. RAG отключён.");
            return;
        }

        try
        {
            int loaded = 0;
            using (var reader = new StreamReader(path, System.Text.Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var entry = JsonUtility.FromJson<WitteEntry>(line);
                    if (entry != null && !string.IsNullOrEmpty(entry.output))
                    {
                        entries.Add(entry);
                        loaded++;
                    }

                    // Ограничиваем загрузку для экономии RAM
                    if (loaded >= 500) break;
                }
            }

            isLoaded = true;
            Debug.Log($"[WitteKnowledgeBase] Загружено {loaded} записей из датасета Витте.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WitteKnowledgeBase] Ошибка загрузки: {e.Message}");
        }
    }

    /// <summary>
    /// Найти релевантные фрагменты по ключевым словам (простой TF поиск)
    /// </summary>
    public string FindRelevantKnowledge(string query)
    {
        if (!isLoaded || entries.Count == 0)
            return "";

        // Разбиваем запрос на ключевые слова (убираем короткие и стоп-слова)
        var keywords = query.ToLower()
            .Split(new char[] { ' ', ',', '.', '!', '?', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Where(w => !IsStopWord(w))
            .ToList();

        if (keywords.Count == 0)
            return "";

        // Ранжируем записи по количеству совпадений ключевых слов
        var scored = entries.Select(e => new
        {
            Entry = e,
            Score = keywords.Count(kw =>
                e.output.ToLower().Contains(kw) ||
                e.instruction.ToLower().Contains(kw))
        })
        .Where(x => x.Score > 0)
        .OrderByDescending(x => x.Score)
        .Take(maxFragments)
        .ToList();

        if (scored.Count == 0)
            return "";

        // Собираем фрагменты
        var fragments = new List<string>();
        foreach (var item in scored)
        {
            string text = item.Entry.output;
            if (text.Length > maxCharsPerFragment)
                text = text.Substring(0, maxCharsPerFragment) + "...";

            fragments.Add($"• {text}");
        }

        return "\n\nДОПОЛНИТЕЛЬНЫЕ ИСТОРИЧЕСКИЕ ФАКТЫ ИЗ МЕМУАРОВ С.Ю. ВИТТЕ:\n" +
               string.Join("\n", fragments);
    }

    /// <summary>
    /// Расширяет промпт релевантными знаниями из базы Витте
    /// </summary>
    public string EnrichPrompt(string originalPrompt)
    {
        string knowledge = FindRelevantKnowledge(originalPrompt);
        if (string.IsNullOrEmpty(knowledge))
            return originalPrompt;

        return originalPrompt + knowledge;
    }

    /// <summary>
    /// Проверяет, является ли слово стоп-словом (предлоги, местоимения)
    /// </summary>
    private bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "этот", "этом", "этой", "этих", "того", "тому", "тоже",
            "было", "были", "быль", "будет", "будут", "буду",
            "когда", "тогда", "здесь", "потом", "после", "затем",
            "который", "которая", "которые", "которого", "которой",
            "свой", "своей", "своего", "свою", "своих",
            "всех", "весь", "всей", "всего", "более", "менее",
            "него", "неё", "нему", "ними", "ними",
            "может", "можно", "нужно", "надо", "должен",
            "очень", "также", "только", "между", "через",
            "себя", "себе", "ещё", "даже", "чтобы", "потому",
            "если", "хотя", "однако", "каких", "какой", "какие",
            "есть", "имеет", "имел", "имели", "стал", "стали",
            "текст", "история", "расскажи", "опиши", "напиши"
        };
        return stopWords.Contains(word);
    }
}

/// <summary>
/// Структура одной записи из witte_dataset.jsonl
/// </summary>
[System.Serializable]
public class WitteEntry
{
    public string instruction;
    public string input;
    public string output;
}
