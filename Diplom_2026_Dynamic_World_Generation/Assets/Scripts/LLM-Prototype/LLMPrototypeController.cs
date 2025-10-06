using UnityEngine;
using LLMUnity;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Reflection;

public class LLMPrototypeController : MonoBehaviour
{
    [Header("LLM Settings")]
    public LLMCharacter llmCharacter;
    
    [Header("File Paths")]
    public string inputJsonPath = "input.json";
    public string outputJsonPath = "output.json";
    
    [Header("Test Mode")]
    public bool testOnStart = true;
    public string testInputJson = @"{
        ""playerAction"": ""refuse"",
        ""npcState"": ""neutral"",
        ""context"": {
            ""location"": ""tavern"",
            ""relationship"": ""stranger""
        }
    }";

    private async void Start()
    {
        if (testOnStart)
        {
            // Ждём пока llmCharacter назначен и (по возможности) инициализирован
            await WaitForLLMCharacterReady(7000);
            ProcessJsonInput(testInputJson);
        }
    }

    public async void ProcessJsonInput(string inputJson)
    {
        Debug.Log("=== Начало обработки ===");
        Debug.Log($"Входной JSON: {inputJson}");

        // Парсим входные данные
        InputData input = null;
        try
        {
            input = JsonUtility.FromJson<InputData>(inputJson);
            if (input == null)
            {
                Debug.LogError("Не удалось распарсить входной JSON через JsonUtility.");
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка парсинга входного JSON: {e}");
            return;
        }

        // Формируем промпт для LLM
        string prompt = CreatePrompt(input);
        Debug.Log($"Промпт для LLM:\n{prompt}");

        // Отправляем запрос к LLM
        string llmResponse = await SendToLLM(prompt);
        Debug.Log($"Ответ LLM:\n{llmResponse}");

        // Парсим ответ и создаём выходной JSON
        OutputData output = ParseLLMResponse(llmResponse);
        string outputJson = JsonUtility.ToJson(output, true);
        
        Debug.Log($"=== Выходной JSON ===\n{outputJson}");

        // Сохраняем в файл
        SaveToFile(outputJson);
    }

    private string CreatePrompt(InputData input)
    {
        return $@"Ты - система управления NPC в игре. 
Проанализируй ситуацию и создай реакцию персонажа в формате JSON.

ВХОДНЫЕ ДАННЫЕ:
- Действие игрока: {input.playerAction}
- Состояние NPC: {input.npcState}
- Местоположение: {input.context.location}
- Отношения: {input.context.relationship}

ТРЕБОВАНИЯ К ОТВЕТУ:
Верни ТОЛЬКО валидный JSON в таком формате (без дополнительного текста):
{{
    ""dialogue"": ""текст диалога на русском"",
    ""action"": ""действие персонажа"",
    ""emotion"": ""эмоция"",
    ""animation"": ""описание анимации""
}}

Правила:
- Если игрок отказался (refuse), а NPC был нейтральным - NPC может разозлиться
- В таверне конфликты обостряются
- Диалог должен быть естественным и подходить по контексту
- Действие должно логично следовать из ситуации

ОТВЕТ (только JSON):";
    }

    private async Task<string> SendToLLM(string prompt, int timeoutMs = 8000)
    {
        if (llmCharacter == null)
        {
            Debug.LogError("LLM Character не назначен!");
            return "{\"dialogue\":\"ERROR: LLMCharacter not assigned\",\"action\":\"none\",\"emotion\":\"neutral\",\"animation\":\"idle\"}";
        }

        StringBuilder response = new StringBuilder();
        bool completed = false;

        try
        {
            // Обёртка в try — чтобы поймать исключения из Init/Chat (включая ArgumentOutOfRangeException)
            await llmCharacter.Chat(
                prompt,
                (string chunk) => {
                    if (!string.IsNullOrEmpty(chunk))
                        response.Append(chunk);
                },
                () => {
                    completed = true;
                    Debug.Log("LLM завершил генерацию");
                }
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Исключение при вызове llmCharacter.Chat: {ex}");
            return "{\"dialogue\":\"ERROR: LLM exception\",\"action\":\"none\",\"emotion\":\"neutral\",\"animation\":\"idle\"}";
        }

        // Ждём окончания с таймаутом
        int waited = 0;
        int step = 100;
        while (!completed && waited < timeoutMs)
        {
            await Task.Delay(step);
            waited += step;
        }

        if (!completed)
        {
            Debug.LogWarning("Таймаут ожидания ответа LLM (SendToLLM). Вернём частичный результат.");
        }

        return response.ToString();
    }

    private OutputData ParseLLMResponse(string llmResponse)
    {
        try
        {
            string jsonOnly = ExtractFirstJsonObject(llmResponse);
            if (!string.IsNullOrEmpty(jsonOnly))
            {
                OutputData parsed = JsonUtility.FromJson<OutputData>(jsonOnly);
                if (parsed != null)
                {
                    // Нормализуем пустые поля
                    parsed.dialogue = string.IsNullOrEmpty(parsed.dialogue) ? "..." : parsed.dialogue;
                    parsed.action = string.IsNullOrEmpty(parsed.action) ? "idle" : parsed.action;
                    parsed.emotion = string.IsNullOrEmpty(parsed.emotion) ? "neutral" : parsed.emotion;
                    parsed.animation = string.IsNullOrEmpty(parsed.animation) ? "idle" : parsed.animation;
                    return parsed;
                }
                else
                {
                    Debug.LogWarning("JsonUtility.FromJson вернул null.");
                }
            }
            else
            {
                Debug.LogWarning("Не найден JSON в ответе LLM.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка парсинга LLM ответа: {e.Message}\nПолный ответ:\n{llmResponse}");
        }

        // Fallback значения
        return new OutputData
        {
            dialogue = "Хм...",
            action = "idle",
            emotion = "neutral",
            animation = "idle"
        };
    }

    // Извлекает первый корректно сбалансированный JSON-объект { ... }
    private string ExtractFirstJsonObject(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        int start = text.IndexOf('{');
        if (start < 0) return null;

        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{') depth++;
            else if (c == '}') depth--;

            if (depth == 0)
            {
                // i — индекс закрывающей }
                return text.Substring(start, i - start + 1);
            }
        }
        return null;
    }

    private void SaveToFile(string json)
    {
        try
        {
            // safer path for editor & builds
            string fullPath = Path.Combine(Application.persistentDataPath, outputJsonPath);
            File.WriteAllText(fullPath, json, Encoding.UTF8);
            Debug.Log($"Файл сохранён: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка сохранения файла: {e}");
        }
    }

    // Ждём, пока llmCharacter назначен / инициализирован (рефлексивная проверка)
    private async Task WaitForLLMCharacterReady(int timeoutMs = 5000)
    {
        int waited = 0;
        int step = 200;

        while (llmCharacter == null && waited < timeoutMs)
        {
            Debug.Log("Ждём назначения llmCharacter в инспекторе...");
            await Task.Delay(step);
            waited += step;
        }

        if (llmCharacter == null)
        {
            Debug.LogError("LLMCharacter не назначен в инспекторе — процесс продолжён без LLM (ошибка возможна).");
            return;
        }

        // Пытаемся найти свойства/поля готовности через рефлексию
        Type t = llmCharacter.GetType();
        PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        PropertyInfo readyProp = Array.Find(props, p => string.Equals(p.Name, "IsReady", StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(p.Name, "IsInitialized", StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(p.Name, "ready", StringComparison.OrdinalIgnoreCase));
        FieldInfo readyField = Array.Find(fields, f => string.Equals(f.Name, "isReady", StringComparison.OrdinalIgnoreCase)
                                                  || string.Equals(f.Name, "initialized", StringComparison.OrdinalIgnoreCase)
                                                  || string.Equals(f.Name, "serverReady", StringComparison.OrdinalIgnoreCase));

        if (readyProp == null && readyField == null)
        {
            // Попробуем проверить поле llm/server — если оно заполнено, возможно LLM готов
            FieldInfo llmField = Array.Find(fields, f => string.Equals(f.Name, "llm", StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(f.Name, "_llm", StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(f.Name, "server", StringComparison.OrdinalIgnoreCase));
            if (llmField == null)
            {
                Debug.Log("Не удалось определить индикатор готовности llmCharacter (рефлексия не нашла явных полей). Продолжаем без ожидания.");
                return;
            }

            waited = 0;
            while (waited < timeoutMs)
            {
                var val = llmField.GetValue(llmCharacter);
                if (val != null) return;
                await Task.Delay(step);
                waited += step;
            }
            Debug.LogWarning("Таймаут ожидания поля llm/server заполнения.");
            return;
        }

        // Ждём, пока найденное поле/свойство станет true
        waited = 0;
        while (waited < timeoutMs)
        {
            bool ready = false;
            try
            {
                if (readyProp != null)
                {
                    var val = readyProp.GetValue(llmCharacter);
                    if (val is bool b) ready = b;
                }
                else if (readyField != null)
                {
                    var val = readyField.GetValue(llmCharacter);
                    if (val is bool b) ready = b;
                }
            }
            catch { /* ignore */ }

            if (ready) return;

            await Task.Delay(step);
            waited += step;
        }

        Debug.LogWarning("Таймаут ожидания готовности llmCharacter (рефлексивная проверка). Продолжаем выполнение, но возможны ошибки.");
    }

    // Метод для загрузки из файла (опционально)
    public void LoadAndProcessFile()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, inputJsonPath);
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            ProcessJsonInput(json);
        }
        else
        {
            Debug.LogError($"Файл не найден: {fullPath}\n(в редакторе используйте Application.persistentDataPath)");
        }
    }
}
