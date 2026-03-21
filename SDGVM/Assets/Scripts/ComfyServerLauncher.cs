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
        // Сначала проверяем корень проекта (рекомендуемое расположение)
        string rootPath = Directory.GetParent(Application.dataPath).FullName;
        string comfyPath = Path.Combine(rootPath, "ComfyUI");
        string batPath = Path.Combine(comfyPath, batchFileName);

        if (!File.Exists(batPath))
        {
            // Fallback: внутри Assets (legacy)
            comfyPath = Path.Combine(Application.dataPath, "ComfyUI");
            batPath = Path.Combine(comfyPath, batchFileName);
        }

        if (!File.Exists(batPath))
        {
            UnityEngine.Debug.LogError($"❌ Не найден файл запуска ComfyUI: {batPath}");
            UnityEngine.Debug.LogError($"💡 Убедитесь, что папка ComfyUI находится в корне проекта: {rootPath}");
            return;
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
