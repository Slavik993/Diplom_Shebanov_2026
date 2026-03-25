#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

/// <summary>
/// Автоматически ИСКЛЮЧАЕТ тяжёлые AI-модели из билда (LLMUnity, ComfyUI workflows),
/// чтобы билд весил 50-100 МБ, а не 11 ГБ.
/// </summary>
public class ExcludeAIFromBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    private string streamingAssetsPath = "Assets/StreamingAssets";
    private string tempBackupPath = "Temp/StreamingAssetsBackup";

    // Папки внутри StreamingAssets, которые НЕ НУЖНО брать в билд
    private string[] foldersToExclude = {
        "undreamai-v1.2.6-llamacpp"
    };
    
    // Файлы внутри StreamingAssets, которые НЕ НУЖНО брать в билд
    private string[] filesToExclude = {
        "awesome_rpg_icon_workflow.json",
        "cpu_optimized_workflow.json",
        "sd_turbo_workflow.json",
        "Mistral-7B-Instruct-v0.3-Q8_0.gguf",
        "mistral-7b-instruct-v0.2.Q4_K_M.gguf"
    };

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("[ExcludeAIFromBuild] ВРЕМЕННО СТРЫВАЕМ ТЯЖЁЛЫЕ ФАЙЛЫ ИЗ БИЛДА (добавляем ~)...");

        // Прячем папки
        foreach (var folder in foldersToExclude)
        {
            string src = Path.Combine(streamingAssetsPath, folder);
            string dest = src + "~";
            if (Directory.Exists(src))
            {
                try
                {
                    Debug.Log($"[ExcludeAIFromBuild] Скрываем папку: {folder}");
                    Directory.Move(src, dest);
                    if (File.Exists(src + ".meta")) File.Move(src + ".meta", dest + ".meta");
                }
                catch (IOException e)
                {
                    Debug.LogError($"[ExcludeAIFromBuild] ОШИБКА: Папка занята другим процессом. Закройте запущенную игру/сервер перед билдом! {e.Message}");
                }
            }
        }
        
        // Прячем файлы
        foreach (var file in filesToExclude)
        {
            string src = Path.Combine(streamingAssetsPath, file);
            string dest = src + "~";
            if (File.Exists(src))
            {
                Debug.Log($"[ExcludeAIFromBuild] Скрываем файл: {file}");
                File.Move(src, dest);
                if (File.Exists(src + ".meta")) File.Move(src + ".meta", dest + ".meta");
            }
        }
        
        // Прячем mp4 видео (любые)
        string[] mp4Files = Directory.GetFiles(streamingAssetsPath, "*.mp4");
        foreach (var src in mp4Files)
        {
            string dest = src + "~";
            Debug.Log($"[ExcludeAIFromBuild] Скрываем видео: {Path.GetFileName(src)}");
            File.Move(src, dest);
            if (File.Exists(src + ".meta")) File.Move(src + ".meta", dest + ".meta");
        }

        AssetDatabase.Refresh();
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        Debug.Log("[ExcludeAIFromBuild] ВОЗВРАЩАЕМ ТЯЖЁЛЫЕ ФАЙЛЫ ЗАМЕТНЫМИ ДЛЯ UNITY (убираем ~)...");

        // Возвращаем папки
        foreach (var folder in foldersToExclude)
        {
            string dest = Path.Combine(streamingAssetsPath, folder);
            string src = dest + "~";
            if (Directory.Exists(src))
            {
                Debug.Log($"[ExcludeAIFromBuild] Восстанавливаем папку: {folder}");
                Directory.Move(src, dest);
                if (File.Exists(src + ".meta")) File.Move(src + ".meta", dest + ".meta");
            }
        }
        
        // Возвращаем исходные файлы
        foreach (var file in filesToExclude)
        {
            string dest = Path.Combine(streamingAssetsPath, file);
            string src = dest + "~";
            if (File.Exists(src))
            {
                Debug.Log($"[ExcludeAIFromBuild] Восстанавливаем файл: {file}");
                File.Move(src, dest);
                if (File.Exists(src + ".meta")) File.Move(src + ".meta", dest + ".meta");
            }
        }

        // Возвращаем mp4 файлы
        string[] hiddenMp4Files = Directory.GetFiles(streamingAssetsPath, "*.mp4~");
        foreach (var src in hiddenMp4Files)
        {
            string dest = src.Substring(0, src.Length - 1); // убираем ~
            Debug.Log($"[ExcludeAIFromBuild] Восстанавливаем видео: {Path.GetFileName(dest)}");
            File.Move(src, dest);
            if (File.Exists(src + ".meta")) File.Move(src + ".meta", dest + ".meta");
        }

        AssetDatabase.Refresh();
    }
}
#endif
