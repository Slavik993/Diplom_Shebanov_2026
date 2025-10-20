using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ComfyUIClient : MonoBehaviour
{
    [Header("ComfyUI Settings")]
    public string comfyServerUrl = "http://127.0.0.1:8188/prompt";

    [Header("UI")]
    public RawImage previewImage; // сюда покажем спрайт
    public Text statusText;

    [Header("Prompt")]
    [TextArea(3, 5)]
    public string positivePrompt = "cute fantasy icon, 2d game sprite, colorful";
    public string negativePrompt = "blurry, distorted, ugly";

    public void GenerateIcon()
    {
        StartCoroutine(SendComfyRequest());
    }

    private IEnumerator SendComfyRequest()
    {
        statusText.text = "⏳ Генерация...";
        
        string json = $@"{{
            ""prompt"": {{
                ""3"": {{
                    ""inputs"": {{
                        ""seed"": 12345,
                        ""steps"": 20,
                        ""cfg"": 8,
                        ""sampler_name"": ""euler"",
                        ""positive"": ""{positivePrompt}"",
                        ""negative"": ""{negativePrompt}"",
                        ""model"": ""turbo-sd.safetensors""
                    }},
                    ""class_type"": ""KSampler""
                }}
            }}
        }}";

        using (UnityWebRequest req = new UnityWebRequest(comfyServerUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                statusText.text = $"❌ Ошибка: {req.error}";
                yield break;
            }

            // 🔍 Ответ ComfyUI
            string response = req.downloadHandler.text;
            Debug.Log("Ответ ComfyUI: " + response);
            statusText.text = "✅ Получен ответ от сервера";

            // 🧩 Найдём путь к изображению
            string path = ParseImagePath(response);
            if (string.IsNullOrEmpty(path))
            {
                statusText.text = "⚠ Не удалось найти путь к изображению.";
                yield break;
            }

            string imageUrl = "http://127.0.0.1:8188/view?filename=" + Path.GetFileName(path);
            yield return LoadAndShowImage(imageUrl);
        }
    }

    private string ParseImagePath(string json)
    {
        int idx = json.IndexOf("output");
        if (idx < 0) return null;
        int start = json.IndexOf(":", idx) + 1;
        int end = json.IndexOf("}", start);
        return json.Substring(start, end - start).Trim(' ', '"');
    }

    private IEnumerator LoadAndShowImage(string url)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                statusText.text = $"❌ Ошибка загрузки изображения: {req.error}";
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            previewImage.texture = tex;
            statusText.text = "🖼 Иконка готова!";
        }
    }
}
