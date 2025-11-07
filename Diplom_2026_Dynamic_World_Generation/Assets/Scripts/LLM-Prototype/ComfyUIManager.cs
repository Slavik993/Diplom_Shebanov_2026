using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using Newtonsoft.Json.Linq;

public class ComfyUIManager : MonoBehaviour
{
    public string comfyServerUrl = "http://127.0.0.1:8188";
    public string workflowPath = "Assets/ComfyWorkflows/text2image_workflow_api.json";

    public async Task<Texture2D> GenerateImageAsync(string prompt)
    {
        Debug.Log($"🖼️ Отправка запроса в ComfyUI: {prompt}");

        if (!File.Exists(workflowPath))
        {
            Debug.LogError($"❌ Не найден workflow файл: {workflowPath}");
            return null;
        }

        string workflowJson = File.ReadAllText(workflowPath);
        var workflow = JObject.Parse(workflowJson);
        workflow["prompt"]["5"]["inputs"]["text"] = prompt;

        // === 1️⃣ Отправляем prompt ===
        using (var req = new UnityWebRequest($"{comfyServerUrl}/prompt", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(workflow.ToString());
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Ошибка при отправке запроса: {req.error}\n{req.downloadHandler.text}");
                return null;
            }

            Debug.Log($"✅ Prompt успешно отправлен: {req.downloadHandler.text}");

            JObject response = JObject.Parse(req.downloadHandler.text);
            string promptId = response["prompt_id"]?.ToString();

            if (string.IsNullOrEmpty(promptId))
            {
                Debug.LogError("❌ Не удалось получить prompt_id от ComfyUI!");
                return null;
            }

            // === 2️⃣ Ждём генерации ===
            Debug.Log($"⏳ Ожидание выполнения ComfyUI задачи {promptId} ...");
            Texture2D resultTexture = null;
            bool completed = false;

            for (int attempt = 0; attempt < 120; attempt++) // до ~120 сек ожидания
            {
                await Task.Delay(2000); // опрашиваем каждые 2 сек

                using (var historyReq = UnityWebRequest.Get($"{comfyServerUrl}/history/{promptId}"))
                {
                    await historyReq.SendWebRequest();

                    if (historyReq.result == UnityWebRequest.Result.Success)
                    {
                        string histJson = historyReq.downloadHandler.text;
                        JObject hist = JObject.Parse(histJson);

                        var images = hist.SelectTokens("$..images[0].filename");
                        foreach (var img in images)
                        {
                            string filename = img.ToString();
                            string imageUrl = $"{comfyServerUrl}/view?filename={filename}&subfolder=&type=output";

                            Debug.Log($"✅ Найдено изображение: {filename}");

                            using (var imgReq = UnityWebRequestTexture.GetTexture(imageUrl))
                            {
                                await imgReq.SendWebRequest();
                                if (imgReq.result == UnityWebRequest.Result.Success)
                                {
                                    resultTexture = DownloadHandlerTexture.GetContent(imgReq);
                                    completed = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (completed) break;
                }
            }

            if (!completed)
            {
                Debug.LogError("❌ ComfyUI не успел сгенерировать изображение вовремя.");
                return null;
            }

            Debug.Log("✅ Изображение успешно получено из ComfyUI!");
            return resultTexture;
        }
    }
}
