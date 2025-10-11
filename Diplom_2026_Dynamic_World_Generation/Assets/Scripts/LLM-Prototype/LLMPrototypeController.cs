using UnityEngine;
using LLMUnity;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// Контроллер взаимодействия с LLM NPC + контекстная память
/// </summary>
public class LLMPrototypeController : MonoBehaviour
{
    private List<string> chatHistory = new List<string>(); // 🧠 История диалога
    private string historyFilePath;                        // путь сохранения истории
    private const int maxHistory = 10;                     // ограничение длины памяти

    [Header("LLM Settings")]
    public LLMCharacter llmCharacter;

    [Header("NPC Context")]
    public string currentNPC = "Barman";                   // имя NPC для индивидуальной памяти

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

    private void Awake()
    {
        string basePath = Application.persistentDataPath;
        historyFilePath = Path.Combine(basePath, "chat_history.json");
        Debug.Log($"[LLM] History path: {historyFilePath}");
        LoadChatHistory();
    }

    private async void Start()
    {
        if (testOnStart)
        {
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

        // 🧠 Собираем контекст из истории
        string context = string.Join("\n", chatHistory);

        // Формируем промпт
        string prompt = CreatePrompt(input, context);
        Debug.Log($"Промпт для LLM:\n{prompt}");

        // Отправляем запрос к LLM
        string llmResponse = await SendToLLM(prompt);
        Debug.Log($"Ответ LLM:\n{llmResponse}");

        // Парсим ответ
        OutputData output = ParseLLMResponse(llmResponse);
        string outputJson = JsonUtility.ToJson(output, true);

        // 💾 Добавляем в историю
        AddToChatHistory($"Игрок: {input.playerAction} | NPC: {output.dialogue}");

        Debug.Log($"=== Выходной JSON ===\n{outputJson}");
        SaveToFile(outputJson);
        SaveChatHistory();
    }

    private string CreatePrompt(InputData input, string historyContext)
    {
        return $@"Ты — интеллектуальная система, управляющая поведением и речью персонажей в ролевой игре.  
Твоя задача — генерировать логичный, естественный и контекстуально уместный ответ NPC на действия игрока. 
Проанализируй ситуацию и создай реакцию персонажа в формате JSON.

Контекст предыдущих взаимодействий:
{historyContext}

Текущие входные данные:
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
- Если игрок отказался (refuse), а NPC был нейтральным - NPC может разозлиться.
- В таверне конфликты обостряются.
- Диалог должен быть естественным, связанным и контекстным.
- Действие и эмоция должны логично соответствовать сцене.

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
            await llmCharacter.Chat(
                prompt,
                (string chunk) =>
                {
                    if (!string.IsNullOrEmpty(chunk))
                        response.Append(chunk);
                },
                () =>
                {
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
                    parsed.dialogue = string.IsNullOrEmpty(parsed.dialogue) ? "..." : parsed.dialogue;
                    parsed.action = string.IsNullOrEmpty(parsed.action) ? "idle" : parsed.action;
                    parsed.emotion = string.IsNullOrEmpty(parsed.emotion) ? "neutral" : parsed.emotion;
                    parsed.animation = string.IsNullOrEmpty(parsed.animation) ? "idle" : parsed.animation;
                    return parsed;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка парсинга LLM ответа: {e.Message}\nПолный ответ:\n{llmResponse}");
        }

        return new OutputData
        {
            dialogue = "Хм...",
            action = "idle",
            emotion = "neutral",
            animation = "idle"
        };
    }

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
                return text.Substring(start, i - start + 1);
        }
        return null;
    }

    private void SaveToFile(string json)
    {
        try
        {
            string fullPath = Path.Combine(Application.persistentDataPath, outputJsonPath);
            File.WriteAllText(fullPath, json, Encoding.UTF8);
            Debug.Log($"Файл сохранён: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка сохранения файла: {e}");
        }
    }

    // === 🧠 Управление памятью ===
    private void AddToChatHistory(string record)
    {
        chatHistory.Add(record);
        if (chatHistory.Count > maxHistory)
            chatHistory.RemoveAt(0);
    }

    public void ResetChatMemory(string newNPC = "")
    {
        chatHistory.Clear();
        if (!string.IsNullOrEmpty(newNPC))
            currentNPC = newNPC;

        SaveChatHistory();
        Debug.Log($"[LLM] История очищена. Новый NPC: {currentNPC}");
    }

    private void SaveChatHistory()
    {
        try
        {
            var data = new ChatHistoryData { npcName = currentNPC, history = chatHistory };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(historyFilePath, json, Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка сохранения истории: {e}");
        }
    }

    private void LoadChatHistory()
    {
        if (!File.Exists(historyFilePath)) return;

        try
        {
            string json = File.ReadAllText(historyFilePath, Encoding.UTF8);
            ChatHistoryData data = JsonUtility.FromJson<ChatHistoryData>(json);

            if (data != null && data.npcName == currentNPC)
            {
                chatHistory = data.history ?? new List<string>();
                Debug.Log($"[LLM] История NPC '{currentNPC}' загружена. Кол-во записей: {chatHistory.Count}");
            }
            else
            {
                Debug.Log($"[LLM] История для NPC '{currentNPC}' не найдена, создаём новую.");
                chatHistory = new List<string>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка загрузки истории: {e}");
        }
    }

    private async Task WaitForLLMCharacterReady(int timeoutMs = 5000)
    {
        int waited = 0;
        int step = 200;

        while (llmCharacter == null && waited < timeoutMs)
        {
            Debug.Log("Ждём назначения llmCharacter...");
            await Task.Delay(step);
            waited += step;
        }

        if (llmCharacter == null)
        {
            Debug.LogError("LLMCharacter не назначен!");
            return;
        }
    }

    // === Вспомогательные структуры ===
    [Serializable]
    private class ChatHistoryData
    {
        public string npcName;
        public List<string> history;
    }
    // =======================
    // 🔹 Обработчик JSON-запроса из UI
    // =======================


    // =======================
    // 🔹 Обработчик JSON-запроса из UI (правильный)
    // =======================
    public string GenerateDialogueFromJSON(string jsonInput)
    {
        try
        {
            // 1️⃣ Разбираем входной JSON
            var data = JsonUtility.FromJson<DialogueInput>(jsonInput);
            if (data == null)
            {
                Debug.LogError("Не удалось распарсить входной JSON.");
                return "Ошибка: неверный формат JSON.";
            }

            // 2️⃣ Сериализуем обратно в формат InputData (тот, что использует твой основной метод)
            InputData input = new InputData
            {
                playerAction = data.playerAction,
                npcState = data.npcState,
                context = new InputData.Context
                {
                    location = data.context.location,
                    relationship = data.context.relationship
                }
            };

            // 3️⃣ Запускаем асинхронную обработку
            ProcessJsonInput(JsonUtility.ToJson(input));

            return "Генерация запущена...";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка парсинга JSON или запуска генерации: {ex.Message}");
            return "Ошибка генерации диалога.";
        }
    }

// =======================
// 🔹 Вспомогательные классы для JSON-входа/выхода
// =======================

[System.Serializable]
public class DialogueInput
{
    public string playerAction;
    public string npcState;
    public DialogueContext context;
    public string emotion;
    public int reactionLevel;
}

[System.Serializable]
public class DialogueContext
{
    public string location;
    public string relationship;
}

// 🔹 Формат основного входного JSON (используется ProcessJsonInput)
[System.Serializable]
public class InputData
{
    public string playerAction;
    public string npcState;
    public Context context;

    [System.Serializable]
    public class Context
    {
        public string location;
        public string relationship;
    }
}

// 🔹 Формат ответа от LLM
[System.Serializable]
public class OutputData
{
    public string dialogue;
    public string action;
    public string emotion;
    public string animation;
}


}
