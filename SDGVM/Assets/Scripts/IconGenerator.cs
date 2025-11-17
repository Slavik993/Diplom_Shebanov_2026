using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;  // ← ОБЯЗАТЕЛЬНО!
using System.Collections;

public class IconGenerator : MonoBehaviour
{
    public InputField promptField;
    public RawImage iconDisplay;
    public Button generateButton;

    void Start()
    {
        if (generateButton != null)
            generateButton.onClick.AddListener(GenerateIcon);
    }

    public void GenerateIcon()
    {
        if (string.IsNullOrEmpty(promptField.text))
        {
            Debug.LogError("Введите промпт!");
            return;
        }
        StartCoroutine(SendToComfyUI(promptField.text));
    }

    IEnumerator SendToComfyUI(string prompt)
    {
        string json = "{\"prompt\": \"" + prompt + "\", \"steps\": 20}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8188/prompt", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ComfyUI ошибка: " + www.error);
            }
            else
            {
                Debug.Log("Запрос отправлен в ComfyUI");
                yield return new WaitForSeconds(8f);
                StartCoroutine(LoadImage());
            }
        }
    }

    IEnumerator LoadImage()
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture("http://127.0.0.1:8188/history/last"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(www);
                iconDisplay.texture = tex;
                Debug.Log("Иконка получена!");
            }
            else
            {
                Debug.LogError("Ошибка загрузки: " + www.error);
            }
        }
    }
}