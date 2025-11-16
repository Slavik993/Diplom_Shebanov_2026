using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class NPCDialogueController : MonoBehaviour
{
    public LLMPrototypeController llmController;

    [Header("UI Элементы")]
    public TMP_Text npcNameText;
    public TMP_Text npcDialogueText;
    public TMP_Text dialogueHistoryText;
    public Button nextButton;
    public Button generateButton;

    [Header("NPC Персонажи")]
    public NPCCharacter npcA;
    public NPCCharacter npcB;

    private NPCCharacter currentSpeaker;
    private string dialogueHistory = "";

    void Start()
    {
        currentSpeaker = npcA;

        nextButton.onClick.AddListener(OnNextClicked);
        generateButton.onClick.AddListener(OnGenerateClicked);
    }

    async void OnGenerateClicked()
    {
        await GenerateNextLine();
    }

    async void OnNextClicked()
    {
        SwitchSpeaker();
        await GenerateNextLine();
    }

    async Task GenerateNextLine()
    {
        string prompt = $"Диалог между {npcA.npcName} ({npcA.role}, {npcA.personality}) и {npcB.npcName} ({npcB.role}, {npcB.personality}). " +
                        $"Текущий контекст: {dialogueHistory}\n" +
                        $"Следующая реплика от {currentSpeaker.npcName}:";

        string reply = await llmController.GenerateTextAsync(prompt);

        npcNameText.text = currentSpeaker.npcName;
        npcDialogueText.text = reply;

        dialogueHistory += $"{currentSpeaker.npcName}: {reply}\n";
        dialogueHistoryText.text = dialogueHistory;
    }

    void SwitchSpeaker()
    {
        currentSpeaker = (currentSpeaker == npcA) ? npcB : npcA;
    }
}
