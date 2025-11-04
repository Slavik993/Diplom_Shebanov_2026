using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

public class LLMUIBinder : MonoBehaviour
{
    [Header("Основные связи")]
    public LLMPrototypeController controller;

    public LLMQuestGenerator questGenerator;

    [Header("Quest Generator Панель")]
    public GameObject questPanel;
    public TMP_InputField inputField;
    public TMP_Dropdown typeDropdown;
    public TMP_Dropdown difficultyDropdown;
    public Button generateButton;
    public TMP_Text outputText;


    [Header("NPC Панель")]
    public GameObject NPCDialoguePanel;
    public TMP_InputField npcNameField;
    public TMP_InputField npcEnvironmentField;
    public TMP_InputField playerInputField;
    public TMP_Dropdown npcRelationDropdown;
    public Button NextButton;
    public TMP_Dropdown npcEmotionDropdown;
    public TMP_InputField npcReactionField;
    public Button npcGenerateButton;
    public Button npcSaveButton;
    public TMP_Text npcDialogueText;

    [Header("StoryTeller Панель")]
    public GameObject storyTellerPanel;
    public TMP_InputField storyThemeField;
    public TMP_Dropdown storyStyleDropdown;
    public TMP_Dropdown questTypeDropdown;
    public TMP_InputField storyLengthField;
    public Button storyGenerateButton;
    public Button storySaveButton;
    public TMP_Text storyOutputText;

    [Header("Icon Generator Панель")]
    public GameObject iconGeneratorPanel;
    public TMP_InputField iconDescriptionField;
    public TMP_Dropdown iconStyleDropdown;
    public TMP_InputField iconSizeField;
    public Button iconGenerateButton;
    public Button iconSaveButton;
    public TMP_Text iconStatusText;

    [Header("Загрузка")]
    public GameObject loadingPanel;
    public TMP_Text loadingText;

    // === События ===
    public Action<string> onGenerateDialogue; 
    public Action<string> onGenerateStory;    
    public Action<string> onGenerateIcon;

    void Start()
    {
        if (controller == null)
            controller = FindObjectOfType<LLMPrototypeController>();

        if (controller == null)
        {
            Debug.LogError("❌ Не найден LLMPrototypeController!");
            return;
        }

        BindUI();
        generateButton.onClick.AddListener(OnGenerateClicked);
    }

    async void OnGenerateClicked()
    {
        string input = inputField.text;
        string questType = typeDropdown.options[typeDropdown.value].text;
        string difficulty = difficultyDropdown.options[difficultyDropdown.value].text;
        string prompt = $"{questType} ({difficulty}): {input}";

        outputText.text = "Генерация...";
        string result = await questGenerator.GenerateQuest(prompt);
        outputText.text = result;
        Debug.Log("🔘 Кнопка генерации нажата");
    }
    
    public void ShowLoading(string message = "Генерация...")
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null)
                loadingText.text = message;
        }
    }

    public void HideLoading()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void BindUI()
    {
        // --- NPC ---
        if (npcGenerateButton != null)
            npcGenerateButton.onClick.AddListener(OnGenerateDialogueClicked);

        if (npcSaveButton != null)
            npcSaveButton.onClick.AddListener(SaveNPCDialogue);

        // --- StoryTeller ---
        if (storyGenerateButton != null)
            storyGenerateButton.onClick.AddListener(OnGenerateStoryClicked);

        if (storySaveButton != null)
            storySaveButton.onClick.AddListener(SaveStory);

        // --- Icon Generator ---
        if (iconGenerateButton != null)
            iconGenerateButton.onClick.AddListener(OnGenerateIconClicked);

        if (iconSaveButton != null)
            iconSaveButton.onClick.AddListener(SaveIcon);
    }

    // ==========================================================
    // 🧩 --- NPC ---
    // ==========================================================
    void OnGenerateDialogueClicked()
    {
        Debug.Log("🧩 [UI] Кнопка 'Сгенерировать диалог' нажата");

        string name = npcNameField.text;
        string environment = npcEnvironmentField.text;
        string relation = npcRelationDropdown.options[npcRelationDropdown.value].text;
        string emotion = npcEmotionDropdown.options[npcEmotionDropdown.value].text;
        string reaction = npcReactionField.text;

        string inputJson = $@"{{
            ""playerAction"": ""interact"",
            ""npcName"": ""{name}"",
            ""npcState"": ""{relation}"",
            ""context"": {{
                ""location"": ""{environment}"",
                ""relationship"": ""{relation}""
            }},
            ""emotion"": ""{emotion}"",
            ""reactionLevel"": {reaction}
        }}";

        Debug.Log($"📤 [UI] JSON отправлен в LLMPrototypeController:\n{inputJson}");

        onGenerateDialogue?.Invoke(inputJson);
        controller.ProcessJsonInput(inputJson);
    }

    void SaveNPCDialogue()
    {
        string text = npcDialogueText.text;
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("⚠️ Нет диалога для сохранения!");
            return;
        }

        string folder = Path.Combine(Application.dataPath, "Exports/NPCDialogues");
        Directory.CreateDirectory(folder);

        string filename = Path.Combine(folder, $"dialogue_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(filename, text);

        Debug.Log($"💾 Диалог сохранён: {filename}");
        npcDialogueText.text = $"✅ Сохранено: {Path.GetFileName(filename)}";
    }

    // ==========================================================
    // 📖 --- StoryTeller ---
    // ==========================================================
    void OnGenerateStoryClicked()
    {
        Debug.Log("📚 [UI] Кнопка 'Сгенерировать историю' нажата");

        string theme = storyThemeField.text;
        string style = storyStyleDropdown.options[storyStyleDropdown.value].text;
        string length = storyLengthField.text;
        string questType = questTypeDropdown.options[questTypeDropdown.value].text;

        string inputJson = $@"{{
    ""storyTheme"": ""{theme}"",
    ""storyStyle"": ""{style}"",
    ""questType"": ""{questType}"",
    ""length"": ""{length}""
    }}";

        Debug.Log($"📤 [UI] JSON для Сказителя историй:\n{inputJson}");

        onGenerateStory?.Invoke(inputJson);
        controller.ProcessJsonInput(inputJson);
        ShowLoading($"📚 Генерация истории ({theme})...");
    }

    void SaveStory()
    {
        string text = storyOutputText.text;
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("⚠️ Нет истории для сохранения!");
            return;
        }

        string folder = Path.Combine(Application.dataPath, "Exports/Stories");
        Directory.CreateDirectory(folder);

        string filename = Path.Combine(folder, $"story_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(filename, text);

        Debug.Log($"💾 История сохранена: {filename}");
        storyOutputText.text = $"✅ Сохранено: {Path.GetFileName(filename)}";
    }

    // ==========================================================
    // 🎨 --- Icon Generator ---
    // ==========================================================
    void OnGenerateIconClicked()
    {
        Debug.Log("🎨 [UI] Кнопка 'Сгенерировать иконку' нажата");

        string description = iconDescriptionField.text;
        string style = iconStyleDropdown.options[iconStyleDropdown.value].text;
        string size = iconSizeField.text;

        string inputJson = $@"{{
            ""iconDescription"": ""{description}"",
            ""iconStyle"": ""{style}"",
            ""iconSize"": ""{size}""
        }}";

        Debug.Log($"📤 [UI] JSON отправлен в LLMPrototypeController:\n{inputJson}");

        onGenerateIcon?.Invoke(inputJson);
        controller.ProcessJsonInput(inputJson);
    }

    void SaveIcon()
    {
        string text = iconStatusText.text;
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("⚠️ Нет результата для сохранения!");
            return;
        }

        string folder = Path.Combine(Application.dataPath, "Exports/Icons");
        Directory.CreateDirectory(folder);

        string filename = Path.Combine(folder, $"icon_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(filename, text);

        Debug.Log($"💾 Информация об иконке сохранена: {filename}");
        iconStatusText.text = $"✅ Сохранено: {Path.GetFileName(filename)}";
    }

    // ==========================================================
    // 🔹 Вывод результата
    // ==========================================================
    public void DisplayResult(string text)
    {
        if (npcDialogueText != null && NPCDialoguePanel != null && NPCDialoguePanel.activeSelf)
            npcDialogueText.text = text;

        if (storyOutputText != null && storyTellerPanel != null && storyTellerPanel.activeSelf)
            storyOutputText.text = text;

        if (iconStatusText != null && iconGeneratorPanel != null && iconGeneratorPanel.activeSelf)
            iconStatusText.text = text;
    }
}
