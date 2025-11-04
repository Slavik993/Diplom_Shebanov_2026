using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ComfyUIManager : MonoBehaviour
{
    [Header("Настройки ComfyUI")]
    public string comfyUrl = "http://127.0.0.1:8188/api/generate";

    public async Task<Texture2D> GenerateImageAsync(string prompt)
    {
        UnityEngine.Debug.Log($"🖼️ Отправка запроса в ComfyUI: {prompt}");

        string json = $"{{\"prompt\":\"{prompt}\"}}";
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(comfyUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            var operation = www.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.Log($"❌ Ошибка запроса в ComfyUI: {www.error}");
                return null;
            }

            UnityEngine.Debug.Log("✅ Ответ от ComfyUI получен!");
            // Здесь нужно обработать ответ — если это изображение, декодировать base64 → Texture2D
            // Для примера просто создаём текстуру-заглушку:
            Texture2D texture = new Texture2D(256, 256);
            texture.Apply();
            return texture;
        }
    }
}
