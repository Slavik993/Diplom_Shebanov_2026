using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class ComfyUIManager : MonoBehaviour
{
    public string apiUrl = "http://127.0.0.1:8188";
    public string workflowFile = "sd_turbo_workflow.json"; // имя файла в StreamingAssets

    public IEnumerator GenerateTexture(string prompt, Action<Texture2D> callback)
    {
        string workflowPath = Path.Combine(Application.streamingAssetsPath, workflowFile);

        if (!File.Exists(workflowPath))
        {
            Debug.LogError("Workflow not found: " + workflowPath);
            callback?.Invoke(null);
            yield break;
        }

        string json = File.ReadAllText(workflowPath);

        // 🔹 Заменяем PROMPT внутри workflow
        json = json.Replace("{{prompt}}", Escape(prompt));

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(apiUrl + "/prompt", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("POST failed: " + req.error + " | " + req.downloadHandler.text);
                callback?.Invoke(null);
                yield break;
            }
        }

        // Ждём рендера
        yield return new WaitForSeconds(5f);

        using (UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(apiUrl + "/history/last"))
        {
            yield return texReq.SendWebRequest();

            if (texReq.result == UnityWebRequest.Result.Success)
                callback?.Invoke(DownloadHandlerTexture.GetContent(texReq));
            else
                callback?.Invoke(null);
        }
    }

    private string Escape(string s)
    {
        return s.Replace("\"", "\\\"");
    }
}
