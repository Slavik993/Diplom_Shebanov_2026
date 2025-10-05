using UnityEngine;
using LLMUnity;
using System.Threading.Tasks;
using System.IO;

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

    private void Start()
    {
        if (testOnStart)
        {
            ProcessJsonInput(testInputJson);
        }
    }

    public async void ProcessJsonInput(string inputJson)
    {
        Debug.Log("=== Начало обработки ===");
        Debug.Log($"Входной JSON: {inputJson}");

        // Парсим входные данные
        InputData input = JsonUtility.FromJson<InputData>(inputJson);

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

    private async Task<string> SendToLLM(string prompt)
    {
        if (llmCharacter == null)
        {
            Debug.LogError("LLM Character не назначен!");
            return "{\"dialogue\":\"ERROR\",\"action\":\"none\",\"emotion\":\"neutral\",\"animation\":\"idle\"}";
        }

        string response = "";
        bool completed = false;
        
        // Правильная сигнатура для LLMUnity
        await llmCharacter.Chat(
            prompt, 
            (reply) => {
                response += reply; // Собираем ответ по частям
            }, 
            () => {
                completed = true; // Callback завершения без параметров
                Debug.Log("LLM завершил генерацию");
            }
        );

        // Ждём завершения
        while (!completed)
        {
            await Task.Yield();
        }

        return response;
    }

    private OutputData ParseLLMResponse(string llmResponse)
    {
        try
        {
            // Извлекаем JSON из ответа (LLM может добавить лишний текст)
            int jsonStart = llmResponse.IndexOf('{');
            int jsonEnd = llmResponse.LastIndexOf('}') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                string jsonOnly = llmResponse.Substring(jsonStart, jsonEnd - jsonStart);
                return JsonUtility.FromJson<OutputData>(jsonOnly);
            }
            else
            {
                Debug.LogWarning("Не удалось извлечь JSON из ответа LLM");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка парсинга: {e.Message}");
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

    private void SaveToFile(string json)
    {
        string fullPath = Path.Combine(Application.dataPath, outputJsonPath);
        File.WriteAllText(fullPath, json);
        Debug.Log($"Файл сохранён: {fullPath}");
    }

    // Метод для загрузки из файла (опционально)
    public void LoadAndProcessFile()
    {
        string fullPath = Path.Combine(Application.dataPath, inputJsonPath);
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            ProcessJsonInput(json);
        }
        else
        {
            Debug.LogError($"Файл не найден: {fullPath}");
        }
    }
}