using System.Threading.Tasks;
using UnityEngine;
using System.Net.Http;
using System.Text;
using System;

public class PythonImageGenerator : MonoBehaviour
{
    public string serverUrl = "http://127.0.0.1:5000/generate";
    private static readonly HttpClient client = new HttpClient();

    public async Task<Texture2D> GenerateImageAsync(string prompt)
    {
        try
        {
            var json = "{\"prompt\": \"" + prompt.Replace("\"", "'") + "\"}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Debug.Log("📤 [PythonImageGenerator] Запрос: " + json);

            HttpResponseMessage response = await client.PostAsync(serverUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError("❌ Сервер вернул ошибку: " + response.StatusCode);
                return null;
            }

            // Сервер возвращает PNG в бинарном виде
            byte[] pngBytes = await response.Content.ReadAsByteArrayAsync();

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(pngBytes);

            Debug.Log("✅ [PythonImageGenerator] Изображение получено!");

            return tex;
        }
        catch (Exception ex)
        {
            Debug.LogError("💥 Ошибка PythonImageGenerator: " + ex.Message);
            return null;
        }
    }
}
