using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

public class LLMPrototypeController : MonoBehaviour
{
    [Header("UI Binder")]
    public LLMUIBinder uiBinder;

    [Header("LLM Connector")]
    public LLMCharacter llmCharacter;

    [Header("Debug Options")]
    public bool useTestJson = false;

    private async void Start()
    {
        // Привязываем UI и подписываемся на кнопку
        if (uiBinder != null)
        {
            uiBinder.BindUI();
            uiBinder.onGenerateDialogue += OnGenerateDialogueFromUI;
        }

        // --- Автогенерация только если тестовый режим включён ---
        if (useTestJson)
        {
            Debug.Log("🧩 Используется тестовый JSON для генерации диалога.");
            string testJson = @"{
                ""playerAction"": ""refuse"",
                ""npcState"": ""neutral"",
                ""context"": {
                    ""location"": ""tavern"",
                    ""relationship"": ""stranger""
                }
            }";
            await SendToLLM(testJson);
        }
        else
        {
            Debug.Log("💡 Система готова. Ожидание пользовательского ввода...");
        }
    }

    private async void OnGenerateDialogueFromUI(string jsonFromUI)
    {
        if (string.IsNullOrWhiteSpace(jsonFromUI))
        {
            Debug.LogWarning("⚠ JSON пуст — ничего не отправлено в LLM.");
            return;
        }

        await SendToLLM(jsonFromUI);
    }

    /// <summary>
    /// Отправляет JSON-запрос в LLM и обрабатывает ответ.
    /// </summary>
    public async Task SendToLLM(string json)
    {
        if (llmCharacter == null)
        {
            Debug.LogError("❌ LLMCharacter не назначен в инспекторе!");
            return;
        }

        Debug.Log($"📤 Отправка в LLM:\n{json}");

        try
        {
            string rawResponse = await llmCharacter.Chat(json);

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                Debug.LogWarning("⚠ Пустой ответ от модели.");
                return;
            }

            // --- Удаляем лишние служебные вставки вроде ```json``` ---
            string cleanResponse = Regex.Replace(rawResponse, @"```json|```|json", "", RegexOptions.IgnoreCase).Trim();

            // --- Попробуем обрезать до чистого JSON, если модель сгенерировала его с мусором ---
            int start = cleanResponse.IndexOf('{');
            int end = cleanResponse.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                cleanResponse = cleanResponse.Substring(start, end - start + 1);
            }

            Debug.Log($"📥 Ответ от LLM:\n{cleanResponse}");

            // --- Отправляем текст на UI ---
            if (uiBinder != null)
            {
                uiBinder.DisplayResult(cleanResponse);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 Ошибка при обращении к LLM: {ex.Message}");
        }
    }
    public string testInputJson = "{}"; // временная заглушка для тестового JSON

    public void ProcessJsonInput(string json)
    {
        Debug.Log($"[LLMPrototypeController] ProcessJsonInput вызван с json: {json}");
        // Здесь позже можно подставить вызов GenerateDialogueFromJSON(json);
    }
}
