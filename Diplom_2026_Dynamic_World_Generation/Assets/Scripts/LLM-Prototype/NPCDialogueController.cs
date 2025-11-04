using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class NPCDialogueController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dialogueText;
    public TMP_Text speakerNameText;
    public Button nextButton;
    public TMP_InputField playerInput;
    public Button sendButton;

    [Header("LLM")]
    public LLMPrototypeController controller;

    private Queue<string> dialogueQueue = new Queue<string>();
    private bool waitingForResponse = false;
    private string lastSpeaker = "NPC1";

    void Start()
    {
        if (controller == null)
            controller = FindObjectOfType<LLMPrototypeController>();

        nextButton.onClick.AddListener(OnNextClicked);
        sendButton.onClick.AddListener(OnPlayerSendClicked);

        dialogueText.text = "👋 Диалог готов к началу!";
    }

    async void OnPlayerSendClicked()
    {
        if (waitingForResponse) return;

        string playerMessage = playerInput.text;
        if (string.IsNullOrWhiteSpace(playerMessage)) return;

        AddDialogue("Player", playerMessage);
        playerInput.text = "";

        waitingForResponse = true;
        dialogueText.text = "🤔 NPC думает...";

        string prompt = $"Игрок сказал: \"{playerMessage}\". Ответь от имени NPC1, 1-2 предложения.";
        string npcResponse = await controller.llmCharacter.Chat(prompt);

        AddDialogue("NPC1", npcResponse);
        waitingForResponse = false;
    }

    void OnNextClicked()
    {
        if (dialogueQueue.Count > 0)
        {
            var text = dialogueQueue.Dequeue();
            dialogueText.text = text;
        }
        else
        {
            dialogueText.text = "Диалог завершён.";
        }
    }

    public void AddDialogue(string speaker, string text)
    {
        speakerNameText.text = speaker;
        dialogueQueue.Enqueue($"{speaker}: {text}");

        if (dialogueQueue.Count == 1)
            dialogueText.text = dialogueQueue.Peek();
    }

    // 💬 Диалог между двумя NPC
    public async void StartDialogueBetweenNPCs(string npc1, string npc2, string topic)
    {
        dialogueQueue.Clear();
        dialogueText.text = $"💬 Диалог между {npc1} и {npc2} о теме: {topic}";

        string prompt = $"Сымитируй короткий диалог (поочередные реплики) между {npc1} и {npc2} на тему: {topic}. 4-6 фраз, не длинные.";
        string response = await controller.llmCharacter.Chat(prompt);

        string[] lines = response.Split('\n');
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
                dialogueQueue.Enqueue(line.Trim());
        }

        OnNextClicked();
    }
}
