using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

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
        if (builder == null) builder = FindObjectOfType<UIDynamicBuilder>();
        if (controller == null) controller = FindObjectOfType<LLMPrototypeController>();

        if (builder == null || controller == null)
        {
            Debug.LogError("❌ Не найден UIDynamicBuilder или LLMPrototypeController!");
            return;
        }

        BindUI();
    }

    public void ShowLoading(string message = "Генерация...")
    {
        if (builder.loadingPanel != null)
        {
            builder.loadingPanel.SetActive(true);
            if (builder.loadingText != null)
                builder.loadingText.text = message;
        }
    }

    public void HideLoading()
    {
        if (builder.loadingPanel != null)
            builder.loadingPanel.SetActive(false);
    }


    public void BindUI()
    {
        // --- NPC ---
        if (builder.npcGenerateButton != null)
            builder.npcGenerateButton.onClick.AddListener(OnGenerateDialogueClicked);

        if (builder.npcSaveButton != null)
            builder.npcSaveButton.onClick.AddListener(SaveNPCDialogue);

        // --- StoryTeller ---
        if (builder.storyGenerateButton != null)
            builder.storyGenerateButton.onClick.AddListener(OnGenerateStoryClicked);

        if (builder.storySaveButton != null)
            builder.storySaveButton.onClick.AddListener(SaveStory);

        // --- Icon Generator ---
        if (builder.iconGenerateButton != null)
            builder.iconGenerateButton.onClick.AddListener(OnGenerateIconClicked);

        if (builder.iconSaveButton != null)
            builder.iconSaveButton.onClick.AddListener(SaveIcon);
    }

    // ==========================================================
    // 🧩 --- NPC ---
    // ==========================================================
    void OnGenerateDialogueClicked()
    {
        Debug.Log("🧩 [UI] Кнопка 'Сгенерировать диалог' нажата");

        string name = builder.npcNameField.text;
        string environment = builder.npcEnvironmentField.text;
        string relation = builder.npcRelationDropdown.options[builder.npcRelationDropdown.value].text;
        string emotion = builder.npcEmotionDropdown.options[builder.npcEmotionDropdown.value].text;
        string reaction = builder.npcReactionField.text;

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
        string text = builder.npcDialogueText.text;
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
        builder.npcDialogueText.text = $"✅ Сохранено: {Path.GetFileName(filename)}";
    }

    // ==========================================================
    // 📖 --- StoryTeller ---
    // ==========================================================
    void OnGenerateStoryClicked()
    {
        string theme = builder.storyThemeField.text;
        string style = builder.storyStyleDropdown.options[builder.storyStyleDropdown.value].text;
        string length = builder.storyLengthField.text;
        string questType = builder.questTypeDropdown.options[builder.questTypeDropdown.value].text;

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

        controller.ProcessJsonInput(inputJson);
    }

    void SaveStory()
    {
        string text = builder.storyOutputText.text;
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
        builder.storyOutputText.text = $"✅ Сохранено: {Path.GetFileName(filename)}";
    }

    // ==========================================================
    // 🎨 --- Icon Generator ---
    // ==========================================================
    void OnGenerateIconClicked()
    {
        Debug.Log("🎨 [UI] Кнопка 'Сгенерировать иконку' нажата");

        string description = builder.iconDescriptionField.text;
        string style = builder.iconStyleDropdown.options[builder.iconStyleDropdown.value].text;
        string size = builder.iconSizeField.text;

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
        string text = builder.iconStatusText.text;
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
        builder.iconStatusText.text = $"✅ Сохранено: {Path.GetFileName(filename)}";
    }

    // ==========================================================
    // 🔹 Вывод результата
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
