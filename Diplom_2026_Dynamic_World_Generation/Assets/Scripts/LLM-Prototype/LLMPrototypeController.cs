using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

public class LLMPrototypeController : MonoBehaviour
{
    public LLMUIBinder uiBinder;   // Связь с UI
    private LLMUnity.LLM llm;      // 🔹 ссылка на LLM сервис

    [Header("UI Binder")]
    
    [Header("LLM Connector")]
    public GameObject llmManagerObject; // Перетащите сюда LLMManager
    private LLMCharacter llmCharacter;

    [Header("Debug Options")]
    public bool useTestJson = false;

    private async void Start()
    {
        Debug.Log("🚀 [LLMPrototypeController] Старт контроллера...");

        if (llmManagerObject == null)
        {
            Debug.LogError("❌ llmManagerObject не назначен в инспекторе!");
            return;
        }

        Debug.Log($"🔍 Проверяю объект LLMManager: {llmManagerObject.name}");

        //llmCharacter = llmManagerObject.GetComponent<LLMCharacter>();
        llmCharacter = llmManagerObject.GetComponentInChildren<LLMCharacter>();
        if (llmCharacter != null)
        {
            Debug.Log($"✅ LLMCharacter найден: {llmCharacter.name}");
        }
        else
        {
            Debug.LogError("❌ На объекте LLMManager НЕТ компонента LLMCharacter!");
        }

        // Немного подождём, чтобы LLMServer успел подняться
        await Task.Delay(2000);

        if (llmCharacter != null && llmCharacter.llm != null)
        {
            Debug.Log("🧠 LLMCharacter связан с LLMServer — всё готово!");
        }
        else
        {
            Debug.LogWarning("⚠ LLMCharacter найден, но ссылка на LLMServer ещё не установлена!");
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

    public async void ProcessJsonInput(string json)
    {
        Debug.Log($"[LLMPrototypeController] ProcessJsonInput вызван с json: {json}");

        var binder = FindObjectOfType<LLMUIBinder>();
        if (binder != null)
            binder.ShowLoading("🧠 Нейросеть генерирует ответ...");

        // Здесь твоя реальная генерация (через API, ComfyUI и т.п.)
        for (int i = 0; i <= 100; i += 10)
        {
            Debug.Log($"⏳ Прогресс генерации: {i}%");
            await System.Threading.Tasks.Task.Delay(500);
        }

        string fakeResult = "✅ Генерация завершена успешно!";
        if (binder != null)
        {
            binder.HideLoading();
            binder.DisplayResult(fakeResult);
        }
        Debug.Log($"[LLMPrototypeController] ProcessJsonInput вызван с json: {json}");
        Debug.Log($"🧠 [LLMController] Получен JSON:\n{json}");

        // Заглушка ответа
        string fakeResponse = $"🤖 Ответ LLM: обработан запрос\n{json}";
        
        // Показываем результат в UI
        
        if (binder != null)
            binder.DisplayResult(fakeResponse);
        if (llmCharacter == null)
        {
            Debug.LogError("❌ LLMCharacter не назначен!");
            return;
        }

        try
        {
            // 🔹 Отправляем JSON в модель
            string resultText = await llmCharacter.Chat(json);
            if (string.IsNullOrWhiteSpace(resultText))
                resultText = "⚠ Модель не вернула ответ.";

            Debug.Log($"🧠 Ответ от LLM:\n{resultText}");

            // 🔹 Передаём текст на экран
            uiBinder?.DisplayResult(resultText);
        }
        catch (Exception ex)
        {
            Debug.LogError($"💥 Ошибка при обращении к LLM: {ex.Message}");
        }
    }

    // =====================================================
    // 🎨 Автогенерация иконки по тексту квеста
    // =====================================================
    private async void TryAutoGenerateIcon(string storyText)
    {
        Debug.Log("🎨 Автогенерация иконки для квеста...");

        // Пример: извлекаем тему квеста как основу для визуала
        string visualPrompt = $"fantasy icon, {ExtractMainSubject(storyText)}";

        // Отправляем описание в ComfyUI
        Texture2D iconTexture = await ComfyUILocalConnector.GenerateIcon(visualPrompt);

        if (iconTexture != null)
        {
            GeneratedContentSaver.SaveVisual(iconTexture);
            Debug.Log("✅ Иконка успешно создана и сохранена!");
        }
        else
        {
            Debug.LogWarning("⚠️ Не удалось сгенерировать иконку!");
        }
    }

    // Простой анализатор для вытаскивания ключевого объекта из текста
    private string ExtractMainSubject(string storyText)
    {
        if (string.IsNullOrEmpty(storyText))
            return "fantasy object";

        // Примитивно: берем первое существительное / ключевое слово
        if (storyText.Contains("дракон")) return "dragon";
        if (storyText.Contains("меч")) return "sword";
        if (storyText.Contains("маг")) return "wizard";
        if (storyText.Contains("лес")) return "forest artifact";

        return "fantasy artifact";
    }


}
