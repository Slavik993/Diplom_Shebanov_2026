using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestGeneratorUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField questPromptInput;
    public TMP_Dropdown questTypeDropdown;
    public TMP_InputField questLengthInput;
    public Button generateButton;
    public TMP_Text outputText;

    [Header("Dependencies")]
    public LLMPrototypeController llmController;

    public async void OnGenerateQuestClicked()
    {
        if (llmController == null)
        {
            Debug.LogError("❌ Не назначен LLMPrototypeController!");
            return;
        }

        string prompt = questPromptInput != null ? questPromptInput.text : "Сгенерируй квест.";
        string questType = questTypeDropdown != null ? questTypeDropdown.options[questTypeDropdown.value].text : "Фэнтези";
        string questLength = questLengthInput != null ? questLengthInput.text : "средний";

        string finalPrompt = $"Создай {questLength} квест в жанре {questType}. {prompt}";

        Debug.Log($"🧠 Отправляю запрос в LLM: {finalPrompt}");

        string response = await llmController.GenerateResponse(finalPrompt);

        if (outputText != null)
            outputText.text = response;
        else
            Debug.Log($"📜 Ответ модели: {response}");
    }
}
