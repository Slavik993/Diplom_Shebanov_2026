using System.Threading.Tasks;
using UnityEngine;

public class LLMIconGenerator : MonoBehaviour
{
    public ComfyUIManager comfyUI;

    public async Task<Texture2D> GenerateIcon(string description, string style, string size)
    {
        string prompt = $"Создай изображение '{description}' в стиле '{style}' размером {size}.";
        Debug.Log($"🎨 [IconGenerator] Prompt: {prompt}");

        var texture = await comfyUI.GenerateImageAsync(prompt);
        return texture;
    }
}
