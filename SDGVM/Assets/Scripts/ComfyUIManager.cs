using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

public class ComfyUIManager : MonoBehaviour
{
    public string workflowFile = "sd_turbo_workflow.json";
    public string comfyURL = "http://127.0.0.1:8188";
    public int maxWaitTime = 120; // максимальное время ожидания в секундах (2 минуты)
    public float pollInterval = 1f; // интервал проверки (1 секунда для быстрой генерации)
    
    private string availableModel = null;

    void Start()
    {
        StartCoroutine(InitializeComfyUI());
    }

    IEnumerator InitializeComfyUI()
    {
        Debug.Log("🔍 Checking available models...");
        
        UnityWebRequest req = UnityWebRequest.Get($"{comfyURL}/object_info/CheckpointLoaderSimple");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string response = req.downloadHandler.text;
            Debug.Log("📋 Response: " + response);
            
            availableModel = ExtractFirstModel(response);
            
            if (!string.IsNullOrEmpty(availableModel))
            {
                Debug.Log($"✅ Found model: {availableModel}");
            }
            else
            {
                Debug.LogError("❌ No models found! Add models to ComfyUI/models/checkpoints/");
            }
        }
        else
        {
            Debug.LogError($"❌ Failed to connect to ComfyUI: {req.error}");
        }
    }

    public void Generate(string prompt)
    {
        if (string.IsNullOrEmpty(availableModel))
        {
            Debug.LogError("❌ No model available!");
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
            Debug.LogError("❌ Model not loaded yet!");
            yield break;
        }

        string path = Path.Combine(Application.streamingAssetsPath, workflowFile);

        if (!File.Exists(path))
        {
            Debug.LogError("❌ Workflow not found: " + path);
            yield break;
        }

        string template = File.ReadAllText(path);
        
        // Заменяем плейсхолдеры
        template = template.Replace("<PROMPT>", EscapeJson(prompt));
        template = template.Replace("УКАЖИТЕ_ИМЯ_ВАШЕЙ_МОДЕЛИ.safetensors", availableModel);
        template = template.Replace("sd_turbo.safetensors", availableModel);
        template = template.Replace("v1-5-pruned-emaonly.safetensors", availableModel);

        string payload = $"{{\"prompt\":{template},\"client_id\":\"unity\"}}";

        Debug.Log("📨 Sending workflow with model: " + availableModel);
        Debug.Log($"📝 Prompt: {prompt}");

        byte[] body = Encoding.UTF8.GetBytes(payload);

        UnityWebRequest req = new UnityWebRequest($"{comfyURL}/prompt", "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ POST failed: {req.error}\n{req.downloadHandler.text}");
            yield break;
        }

        Debug.Log("✅ PROMPT ACCEPTED: " + req.downloadHandler.text);

        string promptId = ExtractPromptId(req.downloadHandler.text);

        if (string.IsNullOrEmpty(promptId))
        {
            Debug.LogError("❌ Failed to extract prompt_id");
            yield break;
        }

        Debug.Log($"⏳ Waiting for generation (prompt_id: {promptId})...");

        // Улучшенный polling с проверкой очереди
        string imageFilename = null;
        float elapsed = 0f;
        int checkCount = 0;
        
        while (elapsed < maxWaitTime)
        {
            yield return new WaitForSeconds(pollInterval);
            elapsed += pollInterval;
            checkCount++;

            // Проверяем статус очереди
            if (checkCount % 3 == 0) // каждые 3 секунды проверяем очередь
            {
                yield return CheckQueueStatus(promptId);
            }

            string historyUrl = $"{comfyURL}/history/{promptId}";
            UnityWebRequest historyReq = UnityWebRequest.Get(historyUrl);
            
            yield return historyReq.SendWebRequest();

            if (historyReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"⚠️ History check failed: {historyReq.error}");
                continue;
            }

            string historyJson = historyReq.downloadHandler.text;
            
            // Логируем только каждые 5 секунд чтобы не засорять консоль
            if (checkCount % (int)(5f / pollInterval) == 0)
            {
                Debug.Log($"📊 Still processing... ({elapsed:F1}s / {maxWaitTime}s)");
            }

            // Проверяем наличие изображения
            imageFilename = ExtractImageFilename(historyJson);
            
            if (!string.IsNullOrEmpty(imageFilename))
            {
                Debug.Log($"✅ Image ready: {imageFilename} (took {elapsed:F1}s)");
                break;
            }
            
            // Проверяем ошибки более детально
            if (historyJson.Contains("\"error\"") || historyJson.Contains("\"exception\""))
            {
                Debug.LogError($"❌ Generation error detected!");
                Debug.LogError($"History response: {historyJson}");
                yield break;
            }
        }

        if (string.IsNullOrEmpty(imageFilename))
        {
            Debug.LogError($"❌ Timeout: No image generated after {maxWaitTime} seconds");
            Debug.LogError("🔧 Possible causes:");
            Debug.LogError("   1. ComfyUI is not running or crashed");
            Debug.LogError("   2. Model is too slow (use SD Turbo or SDXL Turbo)");
            Debug.LogError("   3. Workflow has errors (check ComfyUI console)");
            Debug.LogError("   4. GPU memory issue (reduce resolution/batch size)");
            yield break;
        }

        string imageUrl = $"{comfyURL}/view?filename={imageFilename}";
        Debug.Log($"📥 Downloading: {imageUrl}");
        
        UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(imageUrl);

        yield return texReq.SendWebRequest();

        if (texReq.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(texReq);
            Debug.Log($"✅ Texture loaded successfully! Size: {tex.width}x{tex.height}");
            callback?.Invoke(tex);
        }
        else
        {
            Debug.LogError($"❌ Texture download failed: {texReq.error}");
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
            
            // Простая проверка позиции в очереди
            int runningCount = Regex.Matches(queueJson, @"""queue_running""").Count;
            int pendingCount = Regex.Matches(queueJson, @"""queue_pending""").Count;
            
            if (runningCount > 0 || pendingCount > 0)
            {
                Debug.Log($"📊 Queue status - Running: {runningCount}, Pending: {pendingCount}");
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
            Debug.LogError("Parse error: " + e.Message);
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
            // Ищем filename в outputs
            Match match = Regex.Match(json, @"""filename""\s*:\s*""([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }
}