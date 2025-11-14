using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using System.IO;
using System.Text;

public class LLMPrototypeController : MonoBehaviour
{
    [Header("ComfyUI Генератор")]
    public PythonImageGenerator pythonImageGenerator;   // Ссылка на менеджер ComfyUI

    [Header("UI для отображения иконки")]
    public UnityEngine.UI.RawImage iconDisplay;  // Поле под RawImage, где показывается картинка
    public LLMUIBinder uiBinder;   // Связь с UI
    private LLMUnity.LLM llm;      // 🔹 ссылка на LLM сервис

    [Header("UI Binder")]

    [Header("LLM Connector")]
    public GameObject llmManagerObject; // Перетащите сюда LLMManager
    public LLMCharacter llmCharacter;

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


    public async Task<string> GenerateTextAsync(string prompt)
    {
        if (llmCharacter == null)
        {
            Debug.LogError("❌ llmCharacter не назначен!");
            return "Ошибка: модель не найдена.";
        }

        string response = await llmCharacter.Chat(prompt);
        Debug.Log($"🧠 Ответ модели: {response}");
        return response;
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

    public async Task ProcessJsonInput(string json)
    {
        Debug.Log($"[LLMPrototypeController] 🚀 Параллельная генерация контента запущена");

        var binder = FindObjectOfType<LLMUIBinder>();
        binder?.ShowLoading("🧠 Генерация квеста, диалога и иконки...");

        // 📁 Папка для этой сессии
        string sessionFolder = Path.Combine(Application.dataPath, "SavedContent",
            "Session_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        Directory.CreateDirectory(sessionFolder);

        try
        {
            // 🧠 Генерация квеста
            Task<string> questTask = llmCharacter.Chat(json);

            // 💬 Генерация диалога — параллельно, но после текста
            Task<string> dialogTask = questTask.ContinueWith(async t =>
            {
                string storyText = t.Result ?? "⚠ Пустой текст квеста.";
                string dialogPrompt = $"Создай короткий диалог персонажей на основе: {storyText}";
                return await llmCharacter.Chat(dialogPrompt);
            }).Unwrap();

            // 🎨 Генерация иконки — выполняется в главном потоке через UnitySynchronizationContext
            // 🎨 Генерация иконки — выполняется на главном потоке
            Task<Texture2D> iconTask = questTask.ContinueWith(async t =>
            {
                string storyText = t.Result ?? "fantasy object";
                string iconPrompt = $"Изобрази иконку в стиле 2D для темы: {storyText}";

                Texture2D iconResult = null;

                // Переключаемся на главный поток Unity
                await Awaitable.MainThreadAsync();
                iconResult = await pythonImageGenerator.GenerateImageAsync(iconPrompt);

                return iconResult;
            }).Unwrap();

            // ✅ Ждём выполнения всех трёх задач
            await Task.WhenAll(questTask, dialogTask, iconTask);

            string storyText = questTask.Result ?? "⚠ Пустой квест";
            string dialogText = dialogTask.Result ?? "⚠ Пустой диалог";
            Texture2D icon = iconTask.Result;

            // 💾 Сохраняем результаты
            File.WriteAllText(Path.Combine(sessionFolder, "quest.txt"), storyText, Encoding.UTF8);
            File.WriteAllText(Path.Combine(sessionFolder, "dialog.txt"), dialogText, Encoding.UTF8);

            if (icon != null)
            {
                byte[] bytes = icon.EncodeToPNG();
                string iconPath = Path.Combine(sessionFolder, "icon.png");
                File.WriteAllBytes(iconPath, bytes);
                Debug.Log($"🖼️ Иконка сохранена: {iconPath}");

                await Awaitable.MainThreadAsync();
                if (iconDisplay != null)
                    iconDisplay.texture = icon;
            }

            Debug.Log($"✅ Всё готово! Контент сохранён в: {sessionFolder}");
            binder?.DisplayResult("✅ Квест, диалог и иконка успешно сгенерированы и сохранены!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"💥 Ошибка при генерации: {ex.Message}\n{ex.StackTrace}");
            binder?.DisplayResult("❌ Ошибка при генерации. Подробности в консоли.");
        }
        finally
        {
            binder?.HideLoading();
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



    public async Task<string> GenerateResponse(string prompt)
    {
        if (llmCharacter == null)
        {
            Debug.LogError("❌ LLMCharacter не назначен в LLMPrototypeController!");
            return "Ошибка: LLMCharacter не найден.";
        }

        Debug.Log($"📨 Отправляю запрос в модель: {prompt}");

        try
        {
            // ✅ правильный метод общения с LLM — Chat()
            string response = await llmCharacter.Chat(prompt);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogWarning("⚠️ Модель вернула пустой ответ.");
                return "⚠️ Модель не ответила.";
            }

            Debug.Log($"📜 Ответ от модели: {response}");
            return response;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 Ошибка при обращении к LLM: {ex.Message}");
            return $"Ошибка при генерации: {ex.Message}";
        }
    }


}

[System.Serializable]
public class IconRequest
{
    public string iconDescription;
    public string iconStyle;
    public string iconSize;
}

