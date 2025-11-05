using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;          // ← добавляет File, Path



public class ComfyUIManager : MonoBehaviour
{
    [Header("Настройки ComfyUI")]
    
    private string comfyUrl = "http://127.0.0.1:8188/api/prompt";

    public async Task<Texture2D> GenerateImageAsync(string prompt)
    {
        string comfyUrl = "http://127.0.0.1:8188/api/prompt";
        Debug.Log($"🖼️ Отправка запроса в ComfyUI: {prompt}");

        // Загружаем шаблон workflow из файла
        string workflowPath = Path.Combine(Application.dataPath, "ComfyWorkflows/text2image_workflow_api.json");
        string workflowJson = File.ReadAllText(workflowPath);

        // Заменяем PROMPT_PLACEHOLDER в JSON на реальный текст
        workflowJson = workflowJson.Replace("PROMPT_PLACEHOLDER", prompt);

        var bodyRaw = Encoding.UTF8.GetBytes(workflowJson);

        using (UnityWebRequest www = new UnityWebRequest(comfyUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Ошибка запроса в ComfyUI: {www.error}\nОтвет: {www.downloadHandler.text}");
                return null;
            }

            string responseText = www.downloadHandler.text;
            Debug.Log($"📥 Ответ от ComfyUI: {responseText}");

            // Можно добавить получение результата (например, изображение из /view)
            return new Texture2D(2, 2);
        }
    }
}
