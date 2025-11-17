using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;  // Это исправляет TMP_InputField!

public class IconGenerator : MonoBehaviour
{
    [Header("UI Элементы")]
    public TMP_InputField promptField;  // Теперь TMP_InputField найдётся
    public RawImage iconDisplay;
    public Button generateIconBtn;

    void Start()
    {
        if (generateIconBtn != null)
            generateIconBtn.onClick.AddListener(GenerateIcon);
    }

    public void GenerateIcon()
    {
        if (string.IsNullOrEmpty(promptField.text))
        {
            Debug.LogError("[Икон Генератор] Введите промпт!");
            return;
        }
        StartCoroutine(SendToComfyUI(promptField.text));
    }

    private IEnumerator SendToComfyUI(string prompt)
    {
        // JSON для ComfyUI (по ТЗ: SD Turbo/SD3 Medium)
        string jsonPayload = $"{{\"prompt\": \"{prompt}\", \"steps\": 20, \"model\": \"sd-turbo\"}}";
        
        using (UnityWebRequest www = UnityWebRequest.Post("http://127.0.0.1:8188/prompt", jsonPayload, "application/json"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ComfyUI] Ошибка: {www.error}");
            }
            else
            {
                // Ждём генерацию (ComfyUI асинхронный)
                yield return new WaitForSeconds(5f);  // Адаптируй под время генерации
                yield return StartCoroutine(LoadGeneratedImage());
            }
        }
    }

    private IEnumerator LoadGeneratedImage()
    {
        // Получаем историю/изображение из ComfyUI
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture("http://127.0.0.1:8188/history/last_image.png"))  // Адаптируй URL
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(www);
                if (iconDisplay != null) iconDisplay.texture = tex;
                SaveIconToAssets(tex);
            }
            else
            {
                Debug.LogError($"[ComfyUI] Не удалось загрузить изображение: {www.error}");
            }
        }
    }

    private void SaveIconToAssets(Texture2D tex)
    {
        if (tex == null) return;

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/Generated/Icons/" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        System.IO.File.WriteAllBytes(path, bytes);

        // Рефреш ассетов (только в редакторе)
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        Debug.Log($"[Икон Генератор] Сохранено: {path}");
    }
}