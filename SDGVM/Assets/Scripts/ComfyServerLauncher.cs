using UnityEngine;
using System.Diagnostics;
using System.IO;

public class ComfyServerLauncher : MonoBehaviour
{
    [Header("ComfyUI Settings")]
    public string batchFileName = "run_cpu.bat"; // Или run_nvidia_gpu.bat
    public bool autoStartOnPlay = false;

    private Process process;

    void Start()
    {
        if (autoStartOnPlay)
        {
            StartServer();
        }
    }

    [ContextMenu("Start ComfyUI Server")]
    public void StartServer()
    {
        string comfyPath = Path.Combine(Application.dataPath, "ComfyUI");
        string batPath = Path.Combine(comfyPath, batchFileName);

        if (!File.Exists(batPath))
        {
            // Попробуем найти в соседней папке, если ComfyUI лежит рядом с Assets, а не внутри
            // Часто проекты структурированы: Root/Assets и Root/ComfyUI
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string altPath = Path.Combine(rootPath, "ComfyUI", batchFileName);
            
            if (File.Exists(altPath))
            {
                batPath = altPath;
                comfyPath = Path.GetDirectoryName(batPath);
            }
            else
            {
                UnityEngine.Debug.LogError($"❌ Не найден файл запуска ComfyUI: {batPath}");
                return;
            }
        }

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = batPath;
        psi.WorkingDirectory = comfyPath;
        psi.UseShellExecute = true; // Открываем в новом окне
        // psi.CreateNoWindow = false; 

        try
        {
            process = Process.Start(psi);
            UnityEngine.Debug.Log($"✅ ComfyUI сервер запущен: {batPath}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"❌ Ошибка запуска ComfyUI: {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        if (process != null && !process.HasExited)
        {
            // Не убиваем ComfyUI при остановке игры, так как он грузится долго.
            // Но если нужно - можно раскомментировать
            // process.Kill();
        }
    }
}
