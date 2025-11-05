using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

public class LLMPrototypeController : MonoBehaviour
{
    [Header("ComfyUI Генератор")]
    public ComfyUIManager comfyUIManager;   // Ссылка на менеджер ComfyUI

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
        Debug.Log($"[LLMPrototypeController] ProcessJsonInput вызван с json: {json}");

        var binder = FindObjectOfType<LLMUIBinder>();
        if (binder != null)
            binder.ShowLoading("🧠 Нейросеть генерирует иконку...");

        // 🔹 Парсим JSON
        IconRequest data = JsonUtility.FromJson<IconRequest>(json);
        Debug.Log($"🧠 [LLMController] Получен JSON:\n{json}");

        // 🔹 Пошаговый визуальный прогресс (фишка)
        for (int i = 0; i <= 100; i += 20)
        {
            Debug.Log($"⏳ Прогресс генерации: {i}%");
            await System.Threading.Tasks.Task.Delay(300);
        }

        // 🔹 Проверяем наличие ComfyUIManager
        if (comfyUIManager == null)
        {
            Debug.LogError("❌ ComfyUIManager не назначен в инспекторе!");
            binder?.HideLoading();
            binder?.DisplayResult("❌ Ошибка: ComfyUIManager не найден!");
            return;
        }

        try
        {
            // 🔹 Формируем prompt
            string prompt = $"{data.iconDescription}, стиль {data.iconStyle}, размер {data.iconSize}";
            Debug.Log($"🎨 [ComfyUI] Отправляем запрос: {prompt}");

            // 🔹 Отправляем запрос на генерацию изображения
            Texture2D texture = await comfyUIManager.GenerateImageAsync(prompt);

            // 🔹 Проверяем результат
            if (texture != null)
            {
                Debug.Log("✅ Картинка успешно получена от ComfyUI!");

                // Отображаем изображение в UI
                if (iconDisplay != null)
                {
                    iconDisplay.texture = texture;
                    Debug.Log("🖼️ Изображение показано в RawImage!");
                }
                else
                {
                    Debug.LogWarning("⚠ RawImage (iconDisplay) не назначен в инспекторе!");
                }

                binder?.HideLoading();
                binder?.DisplayResult("✅ Иконка успешно сгенерирована!");
            }
            else
            {
                Debug.LogError("❌ ComfyUI не вернул изображение.");
                binder?.HideLoading();
                binder?.DisplayResult("❌ Не удалось получить изображение от ComfyUI.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"💥 Ошибка при генерации изображения: {ex.Message}");
            binder?.HideLoading();
            binder?.DisplayResult($"💥 Ошибка при генерации изображения:\n{ex.Message}");
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

