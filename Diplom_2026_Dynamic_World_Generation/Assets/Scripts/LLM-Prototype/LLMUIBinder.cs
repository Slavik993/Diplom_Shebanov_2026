using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LLMUIBinder : MonoBehaviour
{
    [Header("Основные связи")]
    public UIDynamicBuilder builder;
    public LLMPrototypeController controller;

    // === События ===
    public Action<string> onGenerateDialogue; // NPC
    public Action<string> onGenerateStory;    // Сказитель историй
    public Action<string> onGenerateIcon;     // Генератор икон

    void Start()
    {
        // === Привязки ===
        if (builder == null) builder = FindObjectOfType<UIDynamicBuilder>();
        if (controller == null) controller = FindObjectOfType<LLMPrototypeController>();

        if (builder == null || controller == null)
        {
            Debug.LogError("❌ Не найден UIDynamicBuilder или LLMPrototypeController!");
            return;
        }

        BindUI();
    }

    public void BindUI()
    {
        // --- NPC Панель ---
        if (builder.npcGenerateButton != null)
            builder.npcGenerateButton.onClick.AddListener(OnGenerateDialogueClicked);

        // --- Сказитель историй ---
        if (builder.storyGenerateButton != null)
            builder.storyGenerateButton.onClick.AddListener(OnGenerateStoryClicked);

        // --- Генератор икон ---
        if (builder.iconGenerateButton != null)
            builder.iconGenerateButton.onClick.AddListener(OnGenerateIconClicked);
    }

    // ==========================================================
    // 🧩 --- Контроллер NPC ---
    // ==========================================================
    void OnGenerateDialogueClicked()
    {
        string name = builder.npcNameField.text;
        string relation = builder.npcRelationDropdown.options[builder.npcRelationDropdown.value].text;
        string emotion = builder.npcEmotionDropdown.options[builder.npcEmotionDropdown.value].text;
        string reaction = builder.npcReactionField.text;

        string inputJson = $@"{{
    ""playerAction"": ""interact"",
    ""npcState"": ""{relation}"",
    ""context"": {{
        ""location"": ""tavern"",
        ""relationship"": ""{relation}""
    }},
    ""emotion"": ""{emotion}"",
    ""reactionLevel"": {reaction}
}}";

        Debug.Log($"📤 [UI] JSON для NPC:\n{inputJson}");
        onGenerateDialogue?.Invoke(inputJson);
        controller.ProcessJsonInput(inputJson);
    }

    // ==========================================================
    // 📖 --- Сказитель историй ---
    // ==========================================================
    void OnGenerateStoryClicked()
    {
        string theme = builder.storyThemeField.text;
        string style = builder.storyStyleDropdown.options[builder.storyStyleDropdown.value].text;
        string length = builder.storyLengthField.text;

        string inputJson = $@"{{
    ""storyTheme"": ""{theme}"",
    ""storyStyle"": ""{style}"",
    ""length"": ""{length}""
}}";

        Debug.Log($"📤 [UI] JSON для Сказителя историй:\n{inputJson}");
        onGenerateStory?.Invoke(inputJson);

        // Отправляем в LLM
        controller.ProcessJsonInput(inputJson);

        // --- Storyteller (генерация истории) ---
        builder.storyGenerateButton.onClick.AddListener(() =>
        {
            var storyTheme = builder.storyThemeField.text;
            var storyStyle = builder.storyStyleDropdown.options[builder.storyStyleDropdown.value].text;
            var storyLength = builder.storyLengthField.text;

            string jsonInput = $@"
            {{
                ""mode"": ""story_generation"",
                ""theme"": ""{storyTheme}"",
                ""style"": ""{storyStyle}"",
                ""length"": ""{storyLength}""
            }}";

            Debug.Log($"[StoryTeller] Отправляю запрос в LLM: {jsonInput}");
            controller.ProcessJsonInput(jsonInput);
        });

    }

    // ==========================================================
    // 🎨 --- Генератор икон ---
    // ==========================================================
    void OnGenerateIconClicked()
    {
        string description = builder.iconDescriptionField.text;
        string style = builder.iconStyleDropdown.options[builder.iconStyleDropdown.value].text;
        string size = builder.iconSizeField.text;

        string inputJson = $@"{{
    ""iconDescription"": ""{description}"",
    ""iconStyle"": ""{style}"",
    ""iconSize"": ""{size}""
}}";

        Debug.Log($"📤 [UI] JSON для генератора икон:\n{inputJson}");
        onGenerateIcon?.Invoke(inputJson);

        controller.ProcessJsonInput(inputJson);
    }

    // ==========================================================
    // 🔹 Универсальный метод вывода результата на UI
    // ==========================================================
    public void DisplayResult(string text)
    {
        if (builder.npcDialogueText != null && builder.npcPanel.activeSelf)
            builder.npcDialogueText.text = text;

        if (builder.storyOutputText != null && builder.storyTellerPanel.activeSelf)
            builder.storyOutputText.text = text;

        if (builder.iconStatusText != null && builder.iconGeneratorPanel.activeSelf)
            builder.iconStatusText.text = text;
    }
}
