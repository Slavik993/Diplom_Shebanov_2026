using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;   // ← Важно, если используете UnityWebRequest
using TMPro;
using LLMUnity;
using System.Collections;

public class GameAI : MonoBehaviour
{
    [Header("LLM")]
    public LLMCharacter llmCharacter;

    [Header("UI для текста")]
    public TMP_InputField textPrompt;
    public TMP_Text textOutput;
    public Button textButton;

    [Header("UI для картинки")]
    public TMP_InputField imagePrompt; // TMP для единообразия
    public RawImage imageDisplay;
    public Button imageButton;

    void Start()
    {
        if (textButton) textButton.onClick.AddListener(GenerateText);
        if (imageButton) imageButton.onClick.AddListener(GenerateImage);
    }

    // ===================== ТЕКСТ (квесты, NPC) =====================
    public void GenerateText()
    {
        if (llmCharacter == null || string.IsNullOrEmpty(textPrompt.text)) return;

        textOutput.text = "Генерация...";

        // LLMUnity 2.5.2: Chat(prompt, Callback<string>)
        llmCharacter.Chat(textPrompt.text, OnTextGenerated);
    }

    // Коллбек, который вызывается когда LLM вернёт финальный ответ (string)
    private void OnTextGenerated(string full)
    {
        // full — полный сгенерированный текст
        textOutput.text = full;
        Debug.Log("LLM -> " + full);
    }

    // Пример NPC:	(используем ту же сигнатуру — принимаем string)
    public void NPCSpeak()
    {
        if (llmCharacter == null) return;

        llmCharacter.Chat("Ты — старый воин. Поприветствуй игрока.", OnNPCSpeakComplete);
    }

    private void OnNPCSpeakComplete(string reply)
    {
        Debug.Log("NPC говорит: " + reply);
    }

    // ===================== КАРТИНКИ (ComfyUI) =====================
    public void GenerateImage()
    {
        if (string.IsNullOrEmpty(imagePrompt.text)) return;
        StartCoroutine(SendToComfy(imagePrompt.text));
    }

    IEnumerator SendToComfy(string prompt)
    {
        string json = "{\"prompt\": \"" + UnityWebRequest.EscapeURL(prompt) + "\", \"steps\": 20}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8188/prompt", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ComfyUI: " + www.error + " | " + www.downloadHandler.text);
                yield break;
            }
        }

        // Ждём рендера — при необходимости подкорректируйте таймаут
        yield return new WaitForSeconds(6f);

        // Попытка загрузить последний результат (поменяй URL, если в твоём ComfyUI он другой)
        using (UnityWebRequest texRequest = UnityWebRequestTexture.GetTexture("http://127.0.0.1:8188/history/last"))
        {
            yield return texRequest.SendWebRequest();

            if (texRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(texRequest);
                imageDisplay.texture = tex;
            }
            else
            {
                Debug.LogError("Картинка не пришла: " + texRequest.error + " | " + texRequest.downloadHandler.text);
            }
        }
    }

    // Одна кнопка — всё сразу
    [ContextMenu("Сгенерировать мир")]
    public void GenerateWorld()
    {
        GenerateText();
        GenerateImage();
        NPCSpeak();
    }
}
