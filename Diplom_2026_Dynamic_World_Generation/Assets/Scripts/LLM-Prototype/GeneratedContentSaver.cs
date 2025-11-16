using UnityEngine;
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Вспомогательный класс для сохранения сгенерированного контента:
/// Диалоги NPC, Квесты (истории), Визуалы (иконки)
/// </summary>
public static class GeneratedContentSaver
{
    private static string basePath = Path.Combine(Application.dataPath, "Exports");

    /// <summary>
    /// Общий метод сохранения текстовых данных.
    /// </summary>
    private static void SaveText(string content, string category)
    {
        if (string.IsNullOrEmpty(content))
        {
            Debug.LogWarning($"⚠️ Нет данных для сохранения ({category})!");
            return;
        }

        string folder = Path.Combine(basePath, category);
        Directory.CreateDirectory(folder);

        string filename = Path.Combine(folder, $"{category}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(filename, content);

        Debug.Log($"💾 [{category}] Сохранено: {filename}");

#if UNITY_EDITOR
        // Обновляем окно проекта, чтобы файл появился
        AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// Сохраняет двоичный файл (например PNG).
    /// </summary>
    private static void SaveBytes(byte[] bytes, string category, string ext)
    {
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogWarning($"⚠️ Нет данных для сохранения ({category})!");
            return;
        }

        string folder = Path.Combine(basePath, category);
        Directory.CreateDirectory(folder);

        string filename = Path.Combine(folder, $"{category}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}");
        File.WriteAllBytes(filename, bytes);

        Debug.Log($"💾 [{category}] Сохранено: {filename}");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    // === Обёртки для разных модулей ===

    /// <summary>Сохраняет диалог NPC (текст)</summary>
    public static void SaveDialogue(string dialogueText)
    {
        SaveText(dialogueText, "NPC_Dialogues");
    }

    /// <summary>Сохраняет сгенерированный квест или историю (текст)</summary>
    public static void SaveQuest(string storyText)
    {
        SaveText(storyText, "Quests_Stories");
    }

    /// <summary>Сохраняет описание визуала (или путь к иконке) — оставляем для обратной совместимости</summary>
    public static void SaveVisual(string visualText)
    {
        SaveText(visualText, "Generated_Visuals");
    }

    /// <summary>Сохраняет Texture2D как PNG в папку Generated_Visuals</summary>
    public static void SaveVisual(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogWarning("⚠️ SaveVisual(Texture2D): texture == null");
            return;
        }

        try
        {
            byte[] png = texture.EncodeToPNG();
            SaveBytes(png, "Generated_Visuals", "png");
        }
        catch (Exception ex)
        {
            Debug.LogError($"💥 SaveVisual(Texture2D) error: {ex.Message}");
        }
    }

    /// <summary>Если надо, можно ещё добавить SaveVisual(byte[] pngBytes) — по желанию.</summary>
    public static void SaveVisual(byte[] pngBytes)
    {
        SaveBytes(pngBytes, "Generated_Visuals", "png");
    }
}
