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
