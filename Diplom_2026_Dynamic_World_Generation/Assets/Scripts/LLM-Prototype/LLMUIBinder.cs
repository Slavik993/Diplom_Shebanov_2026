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

        // Кнопка генерации диалога
        builder.npcGenerateButton.onClick.AddListener(OnGenerateClicked);
    }

    void OnGenerateClicked()
    {
        string name = builder.npcNameField.text;
        string relation = builder.npcRelationDropdown.options[builder.npcRelationDropdown.value].text;
        string emotion = builder.npcEmotionDropdown.options[builder.npcEmotionDropdown.value].text;
        string reaction = builder.npcReactionField.text;

        // Создаём JSON из данных UI
        string inputJson = $@"{{
    ""playerAction"": ""refuse"",
    ""npcState"": ""{relation}"",
    ""context"": {{
        ""location"": ""tavern"",
        ""relationship"": ""{relation}""
    }},
    ""emotion"": ""{emotion}"",
    ""reactionLevel"": {reaction}
}}";

        string dialogue = controller.GenerateDialogueFromJSON(inputJson);

        builder.npcDialogueText.text = dialogue;
    }
}
