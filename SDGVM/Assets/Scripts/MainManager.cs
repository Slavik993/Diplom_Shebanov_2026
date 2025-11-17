using UnityEngine;

public class MainManager : MonoBehaviour
{
    public SimpleAI ai;
    public IconGenerator icon;

    [ContextMenu("Сгенерировать всё")]
    public void GenerateWorld()
    {
        if (ai != null) ai.Send();
        if (icon != null) icon.GenerateIcon();  // ← теперь метод существует и публичный
        if (ai != null) ai.NPCSayHello();
    }
}