using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class UIDynamicBuilderEditor
{
    [MenuItem("Tools/Generate/Storytelling UI Layout")]
    public static void BuildUI()
    {
        UIDynamicBuilder builder = Object.FindObjectOfType<UIDynamicBuilder>();
        if (builder == null)
        {
            GameObject go = new GameObject("UIDynamicBuilder");
            builder = go.AddComponent<UIDynamicBuilder>();
        }

        builder.CreateUI();
        EditorUtility.SetDirty(builder);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("✅ UI добавлен в сцену.");
    }
}

