using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LLMUIBinder : MonoBehaviour
{
    public UIDynamicBuilder builder;
    public LLMPrototypeController controller;

    void Start()
    {
        builder = FindObjectOfType<UIDynamicBuilder>();
        controller = FindObjectOfType<LLMPrototypeController>();

        if (builder == null || controller == null)
        {
            Debug.LogError("❌ Не найден UIDynamicBuilder или LLMPrototypeController!");
            return;
        }

        builder.npcGenerateButton.onClick.AddListener(OnGenerateClicked);
    }

    public void OnStoryGenerateClicked()
{
    string theme = builder.storyThemeField.text;
    string style = builder.storyStyleDropdown.options[builder.storyStyleDropdown.value].text;
    string length = builder.storyLengthField.text;

    string prompt = $"Создай {style} историю на тему '{theme}', примерно {length} слов.";
    builder.storyOutputText.text = "Генерация истории...";

    // Вызов генерации через LLM
    string story = controller.GenerateStory(prompt);
    builder.storyOutputText.text = story;
}

    public void OnIconGenerateClicked()
{
    string desc = builder.iconDescriptionField.text;
    string style = builder.iconStyleDropdown.options[builder.iconStyleDropdown.value].text;
    string size = builder.iconSizeField.text;

    builder.iconStatusText.text = "⏳ Генерация иконки...";

    bool success = controller.GenerateIcon(desc, style, size);
    builder.iconStatusText.text = success ? "✅ Иконка создана!" : "❌ Ошибка при генерации";
}

    void OnGenerateClicked()
    {
        string name = builder.npcNameField.text;
        string environment = builder.npcEnvironmentField.text; // 🆕
        string relation = builder.npcRelationDropdown.options[builder.npcRelationDropdown.value].text;
        string emotion = builder.npcEmotionDropdown.options[builder.npcEmotionDropdown.value].text;
        string reaction = builder.npcReactionField.text;

        // Создаём JSON из данных UI
        string inputJson = $@"{{
    ""playerAction"": ""refuse"",
    ""npcState"": ""{relation}"",
    ""context"": {{
        ""location"": ""{environment}"",
        ""relationship"": ""{relation}""
    }},
    ""emotion"": ""{emotion}"",
    ""reactionLevel"": {reaction}
}}";

        Debug.Log($"[LLMUIBinder] Отправляем JSON в контроллер:\n{inputJson}");

        string dialogue = controller.GenerateDialogueFromJSON(inputJson);
        builder.npcDialogueText.text = dialogue;
    }
}
