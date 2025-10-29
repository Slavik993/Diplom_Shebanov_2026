using UnityEngine;
using TMPro;
using Ink.Runtime;

public class QuestController : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;  // ✅ заменили Text на TextMeshProUGUI
    private Story story;

    void Start()
    {
        TextAsset inkJSON = Resources.Load<TextAsset>("Test_Ink"); // Имя без .ink
        if (inkJSON == null)
        {
            Debug.LogError("❌ Не найден файл Test_Ink в папке Resources!");
            return;
        }

        story = new Story(inkJSON.text);
        UpdateDialogue();
    }

    void UpdateDialogue()
    {
        if (story.canContinue)
        {
            dialogueText.text = story.Continue();  // ✅ теперь работает с TMP
        }
        else if (story.currentChoices.Count > 0)
        {
            for (int i = 0; i < story.currentChoices.Count; i++)
                Debug.Log($"🔹 Вариант {i}: {story.currentChoices[i].text}");
        }
        else
        {
            dialogueText.text = "🏁 Конец истории.";
        }
    }

    public void MakeChoice(int choiceIndex)
    {
        story.ChooseChoiceIndex(choiceIndex);
        UpdateDialogue();
    }
}
