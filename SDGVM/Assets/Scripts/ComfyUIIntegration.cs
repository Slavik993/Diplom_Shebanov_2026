using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ComfyUIIntegration : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField promptInput;
    public RawImage outputImage;
    public Button generateButton;

    private readonly string serverURL = "http://127.0.0.1:8188";

    void Start()
    {
        generateButton.onClick.AddListener(GenerateImage);
    }

    public void GenerateImage()
    {
        if (string.IsNullOrEmpty(promptInput.text)) return;
        StartCoroutine(SendPrompt(promptInput.text));
    }

    IEnumerator SendPrompt(string prompt)
    {
        // Простой JSON для дефолтного workflow (адаптируй под свой)
        string json = "{\"prompt\": {\"1\": {\"inputs\": {\"text\": \"" + prompt + "\", \"clip\": [\"4\", \"IMAGE\"]}}, \"2\": {\"inputs\": {\"ckpt_name\": \"sd_xl_base_1.0.safetensors\"}}, \"3\": {\"inputs\": {\"seed\": 123, \"steps\": 20, \"cfg\": 8, \"sampler_name\": \"euler\", \"scheduler\": \"normal\", \"denoise\": 1, \"model\": [\"2\", 0], \"positive\": [\"6\", 0], \"negative\": [\"7\", 0], \"latent_image\": [\"5\", 0]}}, \"4\": {\"inputs\": {\"width\": 512, \"height\": 512, \"batch_size\": 1}}, \"5\": {\"inputs\": {\"samples\": [\"3\", 0]}}, \"6\": {\"inputs\": {\"text\": \"positive prompt\", \"clip\": [\"4\", 0]}}, \"7\": {\"inputs\": {\"text\": \"negative prompt\", \"clip\": [\"4\", 0]}}, \"8\": {\"inputs\": {\"filename_prefix\": \"UnityGen\", \"images\": [\"5\", 0]}}}";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(serverURL + "/prompt", json))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ComfyUI error: " + www.error);
                yield break;
            }

            // Ждём генерацию (проверь /history для статуса)
            yield return new WaitForSeconds(10f);  // Адаптируй под сложность
            yield return StartCoroutine(LoadGeneratedImage());
        }
    }

    IEnumerator LoadGeneratedImage()
    {
        // Загружаем последний результат (адаптируй URL по /history)
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(serverURL + "/view?filename=UnityGen_00001_.png&subfolder=output&type=output"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(www);
                outputImage.texture = tex;
                Debug.Log("Изображение загружено!");
            }
            else
            {
                Debug.LogError("Ошибка загрузки: " + www.error);
            }
        }
    }
}