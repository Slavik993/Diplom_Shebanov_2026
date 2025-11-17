using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public class ComfyUIUpdater : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Путь к папке ComfyUI (например: C:/ComfyUI_windows_portable)")]
    public string comfyUIPath = "C:/ComfyUI_windows_portable";
    
    [Tooltip("Проверять обновления при старте")]
    public bool checkOnStart = true;
    
    [Tooltip("Автоматически устанавливать обновления")]
    public bool autoInstall = false;

    private string pythonPath;
    private string comfyUIMainPath;

    void Start()
    {
        if (checkOnStart)
        {
            CheckAndUpdate();
        }
    }

    [ContextMenu("Проверить обновления ComfyUI")]
    public void CheckAndUpdate()
    {
        if (!Directory.Exists(comfyUIPath))
        {
            Debug.LogError($"ComfyUI не найден по пути: {comfyUIPath}");
            return;
        }

        // Определяем пути
        pythonPath = Path.Combine(comfyUIPath, "python_embeded", "python.exe");
        comfyUIMainPath = Path.Combine(comfyUIPath, "ComfyUI");

        if (!File.Exists(pythonPath))
        {
            Debug.LogError($"Python не найден: {pythonPath}");
            return;
        }

        StartCoroutine(UpdateProcess());
    }

    IEnumerator UpdateProcess()
    {
        Debug.Log("🔍 Проверка обновлений ComfyUI...");

        // 1. Обновляем сам ComfyUI
        yield return StartCoroutine(RunCommand(
            "git",
            $"pull",
            comfyUIMainPath,
            "ComfyUI"
        ));

        // 2. Обновляем custom nodes
        string customNodesPath = Path.Combine(comfyUIMainPath, "custom_nodes");
        if (Directory.Exists(customNodesPath))
        {
            foreach (string nodeDir in Directory.GetDirectories(customNodesPath))
            {
                if (Directory.Exists(Path.Combine(nodeDir, ".git")))
                {
                    string nodeName = Path.GetFileName(nodeDir);
                    yield return StartCoroutine(RunCommand(
                        "git",
                        "pull",
                        nodeDir,
                        $"Node: {nodeName}"
                    ));
                }
            }
        }

        // 3. Обновляем зависимости Python
        yield return StartCoroutine(RunCommand(
            pythonPath,
            "-m pip install --upgrade pip",
            comfyUIMainPath,
            "pip"
        ));

        yield return StartCoroutine(RunCommand(
            pythonPath,
            "-m pip install -r requirements.txt --upgrade",
            comfyUIMainPath,
            "dependencies"
        ));

        Debug.Log("✅ Обновление завершено!");
    }

    IEnumerator RunCommand(string program, string arguments, string workingDir, string taskName)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = program,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = startInfo };
        
        try
        {
            process.Start();
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Debug.Log($"✅ {taskName}: успешно\n{output}");
            }
            else
            {
                Debug.LogWarning($"⚠️ {taskName}: {error}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Ошибка {taskName}: {e.Message}");
        }

        yield return null;
    }

    [ContextMenu("Установить популярные ноды")]
    public void InstallPopularNodes()
    {
        StartCoroutine(InstallNodesProcess());
    }

    IEnumerator InstallNodesProcess()
    {
        string customNodesPath = Path.Combine(comfyUIMainPath, "custom_nodes");
        
        string[] popularNodes = new string[]
        {
            "https://github.com/ltdrdata/ComfyUI-Manager.git",
            "https://github.com/Kosinkadink/ComfyUI-VideoHelperSuite.git",
            "https://github.com/pythongosssss/ComfyUI-Custom-Scripts.git"
        };

        foreach (string repo in popularNodes)
        {
            string repoName = Path.GetFileNameWithoutExtension(repo);
            yield return StartCoroutine(RunCommand(
                "git",
                $"clone {repo}",
                customNodesPath,
                $"Installing {repoName}"
            ));
        }

        Debug.Log("✅ Ноды установлены! Перезапустите ComfyUI.");
    }

    [ContextMenu("Проверить статус сервера")]
    public void CheckServerStatus()
    {
        StartCoroutine(PingServer());
    }

    IEnumerator PingServer()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("http://127.0.0.1:8188/system_stats"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ ComfyUI сервер работает!\n" + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("❌ ComfyUI не отвечает. Запустите его!");
            }
        }
    }
}