using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class LLMUIBinder : MonoBehaviour
{
    public UIDynamicBuilder ui;
    public LLMPrototypeController llmController;

    private const string PREF_PREFIX = "LLMUI_";

    private void Start()
    {
        if (ui == null) ui = FindObjectOfType<UIDynamicBuilder>();
        if (llmController == null) llmController = FindObjectOfType<LLMPrototypeController>();

        if (ui == null) return;

        // Восстановление сохранённых данных
        ui.npcNameField.text = PlayerPrefs.GetString(PREF_PREFIX + "NPCName", "Безымянный");
        ui.npcRelationDropdown.value = PlayerPrefs.GetInt(PREF_PREFIX + "NPCRelation", 1);
        ui.npcEmotionDropdown.value = PlayerPrefs.GetInt(PREF_PREFIX + "NPCEmotion", 0);
        ui.npcReactionField.text = PlayerPrefs.GetString(PREF_PREFIX + "NPCReaction", "50");

        // Подписка на кнопку
        ui.npcGenerateButton.onClick.AddListener(OnGenerateDialogue);
    }

    private void OnGenerateDialogue()
    {
        if (llmController == null)
        {
            Debug.LogError("LLMPrototypeController не найден!");
            return;
        }

        string relation = ui.npcRelationDropdown.options[ui.npcRelationDropdown.value].text;
        string emotion = ui.npcEmotionDropdown.options[ui.npcEmotionDropdown.value].text;

        string json = $@"{{
            ""playerAction"": ""interact"",
            ""npcState"": ""{relation}"",
            ""context"": {{
                ""location"": ""tavern"",
                ""relationship"": ""{relation}""
            }}
        }}";

        llmController.ProcessJsonInput(json);
        ui.npcDialogueText.text = "⏳ Генерация...";
    }

    private void OnDisable()
    {
        if (ui == null) return;

        // Сохраняем данные
        PlayerPrefs.SetString(PREF_PREFIX + "NPCName", ui.npcNameField.text);
        PlayerPrefs.SetInt(PREF_PREFIX + "NPCRelation", ui.npcRelationDropdown.value);
        PlayerPrefs.SetInt(PREF_PREFIX + "NPCEmotion", ui.npcEmotionDropdown.value);
        PlayerPrefs.SetString(PREF_PREFIX + "NPCReaction", ui.npcReactionField.text);
        PlayerPrefs.Save();
    }
}

