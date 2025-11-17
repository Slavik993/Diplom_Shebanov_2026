using UnityEngine;
using UnityEditor;                    // ← ДОБАВЛЕНО!
using UnityEditor.SceneManagement;   // ← ДОБАВЛЕНО!

[ExecuteInEditMode]
public class AutoSaveSystem : MonoBehaviour
{
    private float saveInterval = 180f;
    private float timer = 0f;

    void Update()
    {
        if (Application.isPlaying) return;

        timer += Time.deltaTime;
        if (timer >= saveInterval)
        {
            SaveProject();
            timer = 0f;
        }
    }

    [MenuItem("Tools/СДГВМ/Сохранить проект сейчас")]
    public static void SaveProject()
    {
        AssetDatabase.SaveAssets();  // ← Теперь работает!

        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (scene.isDirty)
                EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"[СДГВМ] Автосохранение: {System.DateTime.Now:HH:mm:ss}");
    }

    void OnEnable()
    {
        EditorApplication.update += AutoSaveUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= AutoSaveUpdate;
    }

    void AutoSaveUpdate()
    {
        if (!Application.isPlaying)
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= saveInterval)
            {
                SaveProject();
                timer = 0f;
            }
        }
    }
}