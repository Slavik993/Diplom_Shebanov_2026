using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;

public static class ComfyUILocalConnector
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<Texture2D> GenerateIcon(string prompt)
    {
        string jsonBody = $@"
        {{
            ""prompt"": ""{prompt}"",
            ""width"": 512,
            ""height"": 512,
            ""steps"": 20
        }}";

        var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("http://127.0.0.1:8188/prompt", content);

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"❌ Ошибка ComfyUI: {response.StatusCode}");
            return null;
        }

        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageBytes);
        return tex;
    }
}
