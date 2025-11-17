using UnityEngine;

public class SDGVManager : MonoBehaviour
{
    public StoryTeller storyTeller;
    public IconGenerator iconGen;
    public NPCController npc;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            Debug.Log("[СДГВМ] F1 — обучающий режим");
    }

    [ContextMenu("Генерировать мир")]
    public void GenerateFullWorld()
    {
        if (storyTeller != null)
            storyTeller.GenerateQuest();  // ← РАБОТАЕТ!

        if (iconGen != null)
            iconGen.GenerateIcon();

        if (npc != null)
            Debug.Log($"NPC: {npc.GetReaction(0.7f, 0.3f)}");
    }
}