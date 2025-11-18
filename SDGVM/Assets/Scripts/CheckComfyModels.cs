using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class CheckComfyModels : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(CheckAvailableModels());
    }

    IEnumerator CheckAvailableModels()
    {
        // Получаем список доступных чекпоинтов
        string url = "http://127.0.0.1:8188/object_info/CheckpointLoaderSimple";
        
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("📋 CheckpointLoaderSimple info:\n" + req.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Failed: " + req.error);
        }

        // Также проверим общую информацию
        url = "http://127.0.0.1:8188/object_info";
        req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            // Ищем CheckpointLoaderSimple в общем списке
            string text = req.downloadHandler.text;
            int idx = text.IndexOf("CheckpointLoaderSimple");
            if (idx > 0)
            {
                string excerpt = text.Substring(idx, Mathf.Min(500, text.Length - idx));
                Debug.Log("📋 CheckpointLoaderSimple excerpt:\n" + excerpt);
            }
        }
    }
}