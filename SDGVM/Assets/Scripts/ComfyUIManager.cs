using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Diagnostics;

public class ComfyUIManager : MonoBehaviour
{
    [Header("ComfyUI Settings")]
    public string workflowFile = "awesome_rpg_icon_workflow.json";
    public string comfyURL = "http://127.0.0.1:8188";
    public float pollInterval = 1f;
    
    [Header("Server Auto-Start")]
    public bool autoStartServer = true;
    public bool useCPU = true;
    
    [Header("Timeout Settings")]
    [Tooltip("Таймаут запуска сервера в секундах (рекомендуется 180+ для CPU режима)")]
    public int serverStartTimeout = 300; // Увеличен до 3 минут
    
    [Header("Path Settings")]
    [Tooltip("Оставьте пустым для автоматического поиска")]
    public string customComfyUIPath = "";
    
    private string ComfyUIPath
    {
        get
        {
            if (!string.IsNullOrEmpty(customComfyUIPath) && Directory.Exists(customComfyUIPath))
            {
                return customComfyUIPath;
            }

            #if UNITY_EDITOR
            string editorPath = Path.Combine(Application.dataPath, "ComfyUI");
            if (Directory.Exists(editorPath))
            {
                return editorPath;
            }
            
            string projectPath = Path.Combine(Application.dataPath, "..", "ComfyUI");
            if (Directory.Exists(projectPath))
            {
                return projectPath;
            }
            
            string portablePath = Path.Combine(Application.dataPath, "..", "..", "ComfyUI_windows_portable");
            if (Directory.Exists(portablePath))
            {
                return portablePath;
            }
            
            return editorPath;
            #else
            return Path.Combine(Application.dataPath, "..", "ComfyUI_Portable");
            #endif
        }
    }
    
    private string availableModel = null;
    private Process comfyProcess = null;
    private static bool serverAlreadyRunning = false;

    void Start()
    {
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        StartCoroutine(InitializeComfyUI());
    }

    void OnApplicationQuit()
    {
        if (comfyProcess != null && !comfyProcess.HasExited)
        {
            UnityEngine.Debug.Log("🔵 Leaving ComfyUI server running...");
        }
    }

    IEnumerator InitializeComfyUI()
    {
        UnityWebRequest testReq = UnityWebRequest.Get($"{comfyURL}/system_stats");
        yield return testReq.SendWebRequest();
        
        if (testReq.result == UnityWebRequest.Result.Success)
        {
            serverAlreadyRunning = true;
            UnityEngine.Debug.Log("✅ ComfyUI server already running!");
        }
        else
        {
            UnityEngine.Debug.Log("⚠️ ComfyUI server not detected");
            
            if (autoStartServer && !serverAlreadyRunning)
            {
                UnityEngine.Debug.Log("🚀 Starting ComfyUI server...");
                yield return StartComfyUIServer();
                serverAlreadyRunning = true;
            }
            else
            {
                UnityEngine.Debug.LogError("❌ Please start ComfyUI manually or enable autoStartServer!");
                yield break;
            }
        }

        yield return LoadAvailableModels();
    }

    IEnumerator StartComfyUIServer()
    {
        string comfyPath = ComfyUIPath;
        string pythonExe = Path.Combine(comfyPath, "python_embeded", "python.exe");
        string mainScript = Path.Combine(comfyPath, "ComfyUI", "main.py");

        UnityEngine.Debug.Log($"🔍 Looking for ComfyUI at: {comfyPath}");

        if (!Directory.Exists(comfyPath))
        {
            UnityEngine.Debug.LogError($"❌ ComfyUI folder not found: {comfyPath}");
            UnityEngine.Debug.LogError("💡 Make sure ComfyUI folder exists or set customComfyUIPath!");
            yield break;
        }

        if (!File.Exists(pythonExe))
        {
            UnityEngine.Debug.LogError($"❌ Python not found: {pythonExe}");
            UnityEngine.Debug.LogError("💡 Check python_embeded folder in ComfyUI!");
            yield break;
        }

        if (!File.Exists(mainScript))
        {
            UnityEngine.Debug.LogError($"❌ main.py not found: {mainScript}");
            yield break;
        }

        bool processStarted = StartComfyProcess(pythonExe, mainScript, comfyPath);
        
        if (!processStarted)
        {
            UnityEngine.Debug.LogError("❌ Failed to start ComfyUI process");
            yield break;
        }

        UnityEngine.Debug.Log($"⏳ Waiting for ComfyUI to start (timeout: {serverStartTimeout}s)...");
        if (useCPU)
        {
            UnityEngine.Debug.Log("⚠️ CPU mode enabled - startup may take 2-3 minutes");
        }

        float elapsed = 0f;
        bool started = false;
        int checkInterval = 3; // Проверяем каждые 3 секунды вместо 2

        while (elapsed < serverStartTimeout)
        {
            yield return new WaitForSeconds(checkInterval);
            elapsed += checkInterval;

            UnityWebRequest checkReq = UnityWebRequest.Get($"{comfyURL}/system_stats");
            yield return checkReq.SendWebRequest();

            if (checkReq.result == UnityWebRequest.Result.Success)
            {
                started = true;
                UnityEngine.Debug.Log($"✅ ComfyUI server started successfully! (took {elapsed:F1}s)");
                break;
            }

            // Показываем прогресс каждые 15 секунд
            if ((int)elapsed % 15 == 0 || elapsed >= serverStartTimeout - checkInterval)
            {
                float progress = (elapsed / serverStartTimeout) * 100f;
                UnityEngine.Debug.Log($"⏳ Still starting... {elapsed:F0}s / {serverStartTimeout}s ({progress:F0}%)");
            }
        }

        if (!started)
        {
            UnityEngine.Debug.LogError($"❌ Server failed to start within {serverStartTimeout}s!");
            UnityEngine.Debug.LogError("💡 Solutions:");
            UnityEngine.Debug.LogError("   1. Increase 'Server Start Timeout' in Inspector (try 300s)");
            UnityEngine.Debug.LogError("   2. Start ComfyUI manually first, then run Unity");
            UnityEngine.Debug.LogError("   3. Check if ComfyUI console shows any errors");
            UnityEngine.Debug.LogError("   4. Disable 'Use CPU' if you have NVIDIA GPU");
        }
    }

    private bool StartComfyProcess(string pythonExe, string mainScript, string comfyPath)
    {
        try
        {
            string arguments = $"\"{mainScript}\" --listen 127.0.0.1 --port 8188";
            if (useCPU)
            {
                arguments += " --cpu";
                UnityEngine.Debug.Log("🖥️ Starting in CPU mode (slower but works without GPU)");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = arguments,
                WorkingDirectory = Path.Combine(comfyPath, "ComfyUI"),
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            UnityEngine.Debug.Log($"▶️ Launching: {pythonExe}");
            UnityEngine.Debug.Log($"📝 Arguments: {arguments}");
            UnityEngine.Debug.Log($"📁 Working dir: {startInfo.WorkingDirectory}");

            comfyProcess = Process.Start(startInfo);
            return comfyProcess != null;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"❌ Failed to start ComfyUI: {e.Message}");
            return false;
        }
    }

    IEnumerator LoadAvailableModels()
    {
        UnityEngine.Debug.Log("🔍 Checking available models...");
        
        UnityWebRequest req = UnityWebRequest.Get($"{comfyURL}/object_info/CheckpointLoaderSimple");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string response = req.downloadHandler.text;
            availableModel = ExtractFirstModel(response);
            
            if (!string.IsNullOrEmpty(availableModel))
            {
                UnityEngine.Debug.Log($"✅ Found model: {availableModel}");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ No models found! Add models to ComfyUI/models/checkpoints/");
            }
        }
        else
        {
            UnityEngine.Debug.LogError($"❌ Failed to connect to ComfyUI: {req.error}");
        }
    }

    public void Generate(string prompt)
    {
        if (string.IsNullOrEmpty(availableModel))
        {
            UnityEngine.Debug.LogError("❌ No model available!");
            return;
        }
        
        StartCoroutine(GenerateTexture(prompt, (tex) => {
            if (tex != null)
            {
                GetComponent<Renderer>().material.mainTexture = tex;
            }
        }));
    }
    
    public IEnumerator GenerateTexture(string prompt, Action<Texture2D> callback)
    {
        if (string.IsNullOrEmpty(availableModel))
        {
            UnityEngine.Debug.LogError("❌ Model not loaded yet!");
            yield break;
        }

        string path = Path.Combine(Application.streamingAssetsPath, workflowFile);

        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError("❌ Workflow not found: " + path);
            yield break;
        }

        string template = File.ReadAllText(path);
        
        // ✅ ГЕНЕРИРУЕМ НОВЫЙ SEED КАЖДЫЙ РАЗ
        int newSeed = UnityEngine.Random.Range(1, int.MaxValue);
        UnityEngine.Debug.Log($"🎲 Using seed: {newSeed}");

        template = template.Replace("<PROMPT>", EscapeJson(prompt));

        // Безопасная замена seed - только в нужном месте
        template = Regex.Replace(template, 
            @"""seed""\s*:\s*-?\d+", 
            $"\"seed\": {newSeed}", 
            RegexOptions.IgnoreCase);

        template = template.Replace("УКАЖИТЕ_ИМЯ_ВАШЕЙ_МОДЕЛИ.safetensors", availableModel);
        template = template.Replace("sd_turbo.safetensors", availableModel);
        template = template.Replace("v1-5-pruned-emaonly.safetensors", availableModel);

        string payload = $"{{\"prompt\":{template},\"client_id\":\"unity\"}}";

        UnityEngine.Debug.Log("📨 Sending workflow with model: " + availableModel);
        UnityEngine.Debug.Log($"📝 Prompt: {prompt}");

        byte[] body = Encoding.UTF8.GetBytes(payload);

        UnityWebRequest req = new UnityWebRequest($"{comfyURL}/prompt", "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError($"❌ POST failed: {req.error}\n{req.downloadHandler.text}");
            yield break;
        }

        UnityEngine.Debug.Log("✅ PROMPT ACCEPTED: " + req.downloadHandler.text);

        string promptId = ExtractPromptId(req.downloadHandler.text);

        if (string.IsNullOrEmpty(promptId))
        {
            UnityEngine.Debug.LogError("❌ Failed to extract prompt_id");
            yield break;
        }

        UnityEngine.Debug.Log($"⏳ Waiting for generation (prompt_id: {promptId})...");
        UnityEngine.Debug.Log("⚠️ NO TIMEOUT - Will wait indefinitely until image is ready");

        string imageFilename = null;
        int checkCount = 0;
        
        while (true)
        {
            yield return new WaitForSeconds(pollInterval);
            checkCount++;

            if (checkCount % 3 == 0)
            {
                yield return CheckQueueStatus(promptId);
            }

            string historyUrl = $"{comfyURL}/history/{promptId}";
            UnityWebRequest historyReq = UnityWebRequest.Get(historyUrl);
            
            yield return historyReq.SendWebRequest();

            if (historyReq.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogWarning($"⚠️ History check failed: {historyReq.error}");
                continue;
            }

            string historyJson = historyReq.downloadHandler.text;
            
            if (checkCount % (int)(5f / pollInterval) == 0)
            {
                UnityEngine.Debug.Log($"📊 Still processing... ({checkCount * pollInterval:F0}s elapsed)");
            }

            imageFilename = ExtractImageFilename(historyJson);
            
            if (!string.IsNullOrEmpty(imageFilename))
            {
                UnityEngine.Debug.Log($"✅ Image ready: {imageFilename} (took {checkCount * pollInterval:F1}s)");
                break;
            }
            
            if (historyJson.Contains("\"error\"") || historyJson.Contains("\"exception\""))
            {
                UnityEngine.Debug.LogError($"❌ Generation error detected!");
                UnityEngine.Debug.LogError($"History response: {historyJson}");
                yield break;
            }
        }

        string imageUrl = $"{comfyURL}/view?filename={imageFilename}";
        UnityEngine.Debug.Log($"📥 Downloading: {imageUrl}");
        
        UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(imageUrl);

        yield return texReq.SendWebRequest();

        if (texReq.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(texReq);
            UnityEngine.Debug.Log($"✅ Texture loaded successfully! Size: {tex.width}x{tex.height}");
            callback?.Invoke(tex);
        }
        else
        {
            UnityEngine.Debug.LogError($"❌ Texture download failed: {texReq.error}");
            callback?.Invoke(null);
        }
    }

    private IEnumerator CheckQueueStatus(string promptId)
    {
        UnityWebRequest queueReq = UnityWebRequest.Get($"{comfyURL}/queue");
        yield return queueReq.SendWebRequest();

        if (queueReq.result == UnityWebRequest.Result.Success)
        {
            string queueJson = queueReq.downloadHandler.text;
            int runningCount = Regex.Matches(queueJson, @"""queue_running""").Count;
            int pendingCount = Regex.Matches(queueJson, @"""queue_pending""").Count;
            
            if (runningCount > 0 || pendingCount > 0)
            {
                UnityEngine.Debug.Log($"📊 Queue status - Running: {runningCount}, Pending: {pendingCount}");
            }
        }
    }

    private string ExtractFirstModel(string json)
    {
        try
        {
            Match match = Regex.Match(json, @"""ckpt_name"":\s*\[\s*\[(.*?)\]");
            if (match.Success)
            {
                string modelsStr = match.Groups[1].Value;
                Match modelMatch = Regex.Match(modelsStr, @"""([^""]+)""");
                if (modelMatch.Success)
                {
                    return modelMatch.Groups[1].Value;
                }
            }
            return null;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Parse error: " + e.Message);
            return null;
        }
    }

    private string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }

    private string ExtractPromptId(string json)
    {
        try
        {
            Match match = Regex.Match(json, @"""prompt_id""\s*:\s*""([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private string ExtractImageFilename(string json)
    {
        try
        {
            Match match = Regex.Match(json, @"""filename""\s*:\s*""([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }
}