using UnityEngine;
using UnityEngine.UI;               // для Slider
using UnityEngine.Networking;
using TMPro;                        // для TMP_Text
using System;                       // ← ОБЯЗАТЕЛЬНО! Action<>, Func<>, etc.
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;            // для Process
using Debug = UnityEngine.Debug;    // чтобы не было конфликта Debug   // ← чтобы не было конфликта Debug

public class ComfyUIManager : MonoBehaviour
{
    [Header("ComfyUI Settings")]
    public string workflowFile = "cpu_optimized_workflow.json";
    public string comfyURL = "http://127.0.0.1:8188";
    public float pollInterval = 1f;
    //public Slider progressBar;        // перетащи Slider из UI
    //public TMP_Text progressText;     // опционально — текст "45%"
    public Slider iconProgressBar;        // Перетащи сюда Slider из UI
    public TMP_Text iconProgressText;     // Перетащи сюда TextMeshPro для процентов (можно оставить пустым)
    private string currentPromptId = "";
    
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

            // Сначала проверяем корень проекта (рекомендуемое расположение — не засоряет Assets плагинами)
            string projectPath = Path.Combine(Application.dataPath, "..", "ComfyUI");
            if (Directory.Exists(projectPath))
            {
                return Path.GetFullPath(projectPath);
            }
            
            #if UNITY_EDITOR
            // Fallback в Assets (legacy)
            string editorPath = Path.Combine(Application.dataPath, "ComfyUI");
            if (Directory.Exists(editorPath))
            {
                return editorPath;
            }
            
            string portablePath = Path.Combine(Application.dataPath, "..", "..", "ComfyUI_windows_portable");
            if (Directory.Exists(portablePath))
            {
                return Path.GetFullPath(portablePath);
            }
            
            return Path.GetFullPath(projectPath); // default
            #else
            // В билде ищем ComfyUI рядом с exe
            string buildPath = Path.Combine(Application.dataPath, "..", "ComfyUI");
            if (Directory.Exists(buildPath))
                return Path.GetFullPath(buildPath);
            
            string buildPath2 = Path.Combine(Application.dataPath, "..", "ComfyUI_Portable");
            if (Directory.Exists(buildPath2))
                return Path.GetFullPath(buildPath2);
            
            return Path.GetFullPath(buildPath);
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
            if (comfyProcess != null)
            {
                // Принудительно убиваем старый процесс, если он остался
                try
                {
                    foreach (var process in Process.GetProcessesByName("python"))
                    {
                        if (process.MainModule.FileName.Contains("ComfyUI"))
                        {
                            process.Kill();
                            process.WaitForExit(3000);
                        }
                    }
                }
                catch { }
            }
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
                UnityEngine.Debug.Log($"📄 Raw JSON response: {response.Substring(0, Math.Min(response.Length, 200))}..."); // Логируем начало JSON для проверки
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
        // Сброс прогресс-бара в начало
        ResetProgressBar();

        if (string.IsNullOrEmpty(availableModel))
        {
            UnityEngine.Debug.LogError("Модель не найдена в checkpoints!");
            callback?.Invoke(null);
            yield break;
        }

        string path = Path.Combine(Application.streamingAssetsPath, workflowFile);
        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError("Workflow не найден: " + path);
            callback?.Invoke(null);
            yield break;
        }

        // Подготовка JSON-шаблона
        string template = File.ReadAllText(path);
        int newSeed = UnityEngine.Random.Range(1, int.MaxValue);
        template = template.Replace("<PROMPT>", EscapeJson(prompt));
        template = Regex.Replace(template, @"""seed""\s*:\s*-?\d+", $"\"seed\": {newSeed}");
        
        UnityEngine.Debug.Log($"🔍 Using model for generation: '{availableModel}'"); // ЛОГ МОДЕЛИ
        template = template.Replace("PLACEHOLDER_MODEL_NAME", availableModel);

        string payload = $"{{\"prompt\": {template}, \"client_id\": \"unity_{UnityEngine.Random.Range(100000,999999)}\"}}";

        // Ждём готовности сервера
        bool serverReady = false;
        float waitTime = 0f;
        while (!serverReady && waitTime < 30f)
        {
            var testReq = UnityWebRequest.Get($"{comfyURL}/prompt");
            yield return testReq.SendWebRequest();

            if (testReq.result == UnityWebRequest.Result.Success)
                serverReady = true;
            else
            {
                yield return new WaitForSeconds(1f);
                waitTime += 1f;
            }
        }

        if (!serverReady)
        {
            UnityEngine.Debug.LogError("ComfyUI не отвечает! Запустите сервер вручную.");
            callback?.Invoke(null);
            yield break;
        }

        // Отправляем промт
        byte[] body = Encoding.UTF8.GetBytes(payload);
        using (var req = new UnityWebRequest($"{comfyURL}/prompt", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError($"Ошибка отправки промта: {req.error}");
                callback?.Invoke(null);
                yield break;
            }

            currentPromptId = ExtractPromptId(req.downloadHandler.text);
            if (string.IsNullOrEmpty(currentPromptId))
            {
                UnityEngine.Debug.LogError("Не удалось получить prompt_id");
                callback?.Invoke(null);
                yield break;
            UnityEngine.Debug.Log($"Генерация иконки началась (prompt_id: {currentPromptId})");
        }

        // ОЖИДАНИЕ + ПРОГРЕСС-БАР (БЕСКОНЕЧНОЕ, ПОКА НЕ СДЕЛАЕТ)
        string imageFilename = null;
        float elapsed = 0f;
        
        // Убрали таймаут по просьбе пользователя
        // float timeout = 600f; 

        while (string.IsNullOrEmpty(imageFilename))
        {
            if (iconProgressBar != null)
            {
                // Асимптотическое приближение к 0.99
                float targetLimit = 0.99f; 
                // Замедляем прогресс со временем
                float currentLimit = Mathf.Lerp(0f, targetLimit, 1f - Mathf.Exp(-elapsed * 0.01f));
                float speed = 0.1f;
                
                float newVal = Mathf.MoveTowards(iconProgressBar.value, currentLimit, Time.deltaTime * speed);
                iconProgressBar.value = newVal;
            }
                
            if (iconProgressText != null)
            {
                if (elapsed < 10f) iconProgressText.text = "Запуск...";
                else if (elapsed < 60f) iconProgressText.text = "Генерация...";
                else if (elapsed < 300f) iconProgressText.text = "Обработка...";
                else if (elapsed < 600f) iconProgressText.text = "Всё ещё работаем...";
                else iconProgressText.text = $"Ждём результат ({elapsed:F0} сек)...";
            }

            yield return new WaitForSeconds(1f);
            elapsed += 1f;

            // Проверяем историю
            UnityWebRequest historyReq = UnityWebRequest.Get($"{comfyURL}/history/{currentPromptId}");
            yield return historyReq.SendWebRequest();

            if (historyReq.result == UnityWebRequest.Result.Success)
            {
                string json = historyReq.downloadHandler.text;
                imageFilename = ExtractImageFilename(json);
                
                if (!string.IsNullOrEmpty(imageFilename))
                {
                    break; // Нашли!
                }
                
                // Если файла нет, проверяем на явную ошибку
                string errorMsg = ExtractErrorMessage(json);
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    UnityEngine.Debug.LogError($"❌ ComfyUI reported error: {errorMsg}");
                    if (iconProgressText != null) iconProgressText.text = "Ошибка!";
                    callback?.Invoke(null);
                    yield break;
                }
            }
            // Если 404 или просто нет файла — продолжаем ждать БЕСКОНЕЧНО
        }

        // Финал прогресс-бара
        if (iconProgressBar != null) iconProgressBar.value = 1f;
        if (iconProgressText != null) iconProgressText.text = "Готово!";

        if (string.IsNullOrEmpty(imageFilename))
        {
            // Сюда мы теоретически не должны попасть, если цикл бесконечный
            // Но оставим на всякий случай
            UnityEngine.Debug.LogError($"Генерация прервана (неизвестная причина)");
            callback?.Invoke(null);
            yield break;
        }

        // Скачиваем готовую иконку
        string imageUrl = $"{comfyURL}/view?filename={imageFilename}&type=output&subfolder=";
        var texReq = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return texReq.SendWebRequest();

        if (texReq.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(texReq);
            UnityEngine.Debug.Log($"Иконка загружена: {tex.width}x{tex.height}");
            callback?.Invoke(tex);
        }
        else
        {
            UnityEngine.Debug.LogError("Ошибка загрузки иконки: " + texReq.error);
            callback?.Invoke(null);
        }
    }
    }

    // Сброс прогресс-бара
    private void ResetProgressBar()
    {
        if (iconProgressBar != null) iconProgressBar.value = 0f;
        if (iconProgressText != null) iconProgressText.text = "";
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

    private string ExtractErrorMessage(string json)
    {
        if (json.Contains("\"status_str\": \"error\"") || json.Contains("\"execution_error\""))
        {
            try
            {
                // Простая попытка достать сообщение об ошибке
                Match match = Regex.Match(json, @"""exception_message""\s*:\s*""([^""]+)""");
                if (match.Success) return match.Groups[1].Value;
                return "Unknown execution error (check ComfyUI console)";
            }
            catch { return "Error parsing failed status"; }
        }
        return null;
    }
}