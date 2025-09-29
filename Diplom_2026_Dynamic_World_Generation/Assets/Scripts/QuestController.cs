using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;

public class QuestController : MonoBehaviour
{
    public Text dialogueText;
    private Story story;

    void Start()
    {
        TextAsset inkJSON = Resources.Load<TextAsset>("Test_Ink"); // Имя без .ink
        story = new Story(inkJSON.text);
        UpdateDialogue();
    }

    void UpdateDialogue()
    {
        if (story.canContinue) dialogueText.text = story.Continue();
        else if (story.currentChoices.Count > 0)
        {
            for (int i = 0; i < story.currentChoices.Count; i++)
                Debug.Log(story.currentChoices[i].text);
        }
    }

    public void MakeChoice(int choiceIndex)
    {
        story.ChooseChoiceIndex(choiceIndex);
        UpdateDialogue();
    }
}
