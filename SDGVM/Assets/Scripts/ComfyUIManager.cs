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
    public int maxWaitTime = 60; // максимальное время ожидания в секундах
    
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

        // Polling: проверяем статус каждые 2 секунды
        string imageFilename = null;
        float elapsed = 0f;
        
        while (elapsed < maxWaitTime)
        {
            yield return new WaitForSeconds(2f);
            elapsed += 2f;

            string historyUrl = $"{comfyURL}/history/{promptId}";
            UnityWebRequest historyReq = UnityWebRequest.Get(historyUrl);
            
            yield return historyReq.SendWebRequest();

            if (historyReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ History check failed: {historyReq.error}");
                continue;
            }

            string historyJson = historyReq.downloadHandler.text;
            Debug.Log($"📊 History response ({elapsed}s): {historyJson}");

            // Проверяем наличие изображения
            imageFilename = ExtractImageFilename(historyJson);
            
            if (!string.IsNullOrEmpty(imageFilename))
            {
                Debug.Log($"✅ Image ready: {imageFilename}");
                break;
            }
            
            // Проверяем ошибки
            if (historyJson.Contains("\"status\"") && historyJson.Contains("error"))
            {
                Debug.LogError($"❌ Generation error detected in history: {historyJson}");
                yield break;
            }

            Debug.Log($"⏳ Still processing... ({elapsed}s / {maxWaitTime}s)");
        }

        if (string.IsNullOrEmpty(imageFilename))
        {
            Debug.LogError($"❌ Timeout: No image generated after {maxWaitTime} seconds");
            Debug.LogError("Check ComfyUI console for errors!");
            yield break;
        }

        string imageUrl = $"{comfyURL}/view?filename={imageFilename}";
        Debug.Log($"📥 Downloading: {imageUrl}");
        
        UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(imageUrl);

        yield return texReq.SendWebRequest();

        if (texReq.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(texReq);
            Debug.Log("✅ Texture loaded successfully!");
            callback?.Invoke(tex);
        }
        else
        {
            Debug.LogError($"❌ Texture download failed: {texReq.error}");
            callback?.Invoke(null);
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