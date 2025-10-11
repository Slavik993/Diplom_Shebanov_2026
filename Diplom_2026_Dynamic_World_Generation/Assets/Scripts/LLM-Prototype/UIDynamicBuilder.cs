using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDynamicBuilder : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenu;
    public GameObject storyTellerPanel;
    public GameObject npcPanel;
    public GameObject iconGeneratorPanel;

    [Header("NPC UI Elements (для LLMUIBinder)")]
    public TMP_InputField npcNameField;
    public TMP_Dropdown npcRelationDropdown;
    public TMP_Dropdown npcEmotionDropdown;
    public TMP_InputField npcReactionField;
    public Button npcGenerateButton;
    public TextMeshProUGUI npcDialogueText;

    private Canvas canvas;

    void Start()
    {
        CreateUI();
    }

    public void CreateUI()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // === MAIN MENU ===
        mainMenu = CreatePanel("MainMenu", canvas.transform);
        CreateMainMenu(mainMenu);

        // === STORYTELLER ===
        storyTellerPanel = CreatePanel("StoryTellerPanel", canvas.transform);
        storyTellerPanel.SetActive(false);
        CreateStoryTellerPanel(storyTellerPanel);

        // === NPC ===
        npcPanel = CreatePanel("NPCPanel", canvas.transform);
        npcPanel.SetActive(false);
        CreateNPCPanel(npcPanel);

        // === ICON GENERATOR ===
        iconGeneratorPanel = CreatePanel("IconGeneratorPanel", canvas.transform);
        iconGeneratorPanel.SetActive(false);
        CreateIconGeneratorPanel(iconGeneratorPanel);
    }

    // ======================= UI HELPERS =======================

    GameObject CreatePanel(string name, Transform parent)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 420);
        rect.anchoredPosition = Vector2.zero;

        var img = panel.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        return panel;
    }

    Button CreateButton(Transform parent, string text, Vector2 pos)
    {
        var btnGO = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        var rect = btnGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 40);
        rect.anchoredPosition = pos;
        btnGO.GetComponent<Image>().color = new Color(0.2f, 0.3f, 0.5f, 1f);
        var button = btnGO.GetComponent<Button>();

        var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(btnGO.transform, false);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        textGO.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;

        return button;
    }

    TextMeshProUGUI CreateTitle(Transform parent, string text)
    {
        var titleGO = new GameObject("Title", typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(parent, false);
        var tmp = titleGO.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.anchoredPosition = new Vector2(0, 160);
        return tmp;
    }

    TMP_InputField CreateInputField(Transform parent, string placeholder, Vector2 pos)
    {
        var go = new GameObject(placeholder, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 36);
        rect.anchoredPosition = pos;
        go.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);

        var input = go.GetComponent<TMP_InputField>();

        var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.fontSize = 20;
        text.color = Color.white;
        input.textComponent = text;

        var placeholderGO = new GameObject("Placeholder", typeof(TextMeshProUGUI));
        placeholderGO.transform.SetParent(go.transform, false);
        var placeholderText = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 18;
        placeholderText.color = new Color(1, 1, 1, 0.5f);
        input.placeholder = placeholderText;

        return input;
    }

    TMP_Dropdown CreateDropdown(Transform parent, string label, string[] options, Vector2 pos)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 36);
        rect.anchoredPosition = pos;

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        foreach (var opt in options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(opt));

        dropdown.RefreshShownValue();
        return dropdown;
    }

    // ======================= PANELS =======================

    void CreateMainMenu(GameObject parent)
    {
        CreateTitle(parent.transform, "Главное меню");

        var b1 = CreateButton(parent.transform, "Сказатель историй", new Vector2(0, 40));
        var b2 = CreateButton(parent.transform, "Контроллер NPC", new Vector2(0, -20));
        var b3 = CreateButton(parent.transform, "Генератор икон", new Vector2(0, -80));

        b1.onClick.AddListener(ShowStoryTellerPanel);
        b2.onClick.AddListener(ShowNPCPanel);
        b3.onClick.AddListener(ShowIconGeneratorPanel);
    }

    void CreateStoryTellerPanel(GameObject parent)
    {
        CreateTitle(parent.transform, "Сказатель историй");
        var backBtn = CreateButton(parent.transform, "Назад", new Vector2(0, -160));
        backBtn.onClick.AddListener(ShowMainMenu);
    }

    void CreateNPCPanel(GameObject parent)
    {
        CreateTitle(parent.transform, "Контроллер NPC");

        npcNameField = CreateInputField(parent.transform, "Имя персонажа", new Vector2(0, 80));
        npcRelationDropdown = CreateDropdown(parent.transform, "Отношение", new[] { "Дружелюбный", "Нейтральный", "Враждебный" }, new Vector2(0, 30));
        npcEmotionDropdown = CreateDropdown(parent.transform, "Эмоция", new[] { "Радость", "Грусть", "Злость", "Нейтрал" }, new Vector2(0, -20));
        npcReactionField = CreateInputField(parent.transform, "Реакция (0–100)", new Vector2(0, -70));

        npcGenerateButton = CreateButton(parent.transform, "Сгенерировать диалог", new Vector2(0, -120));
        npcDialogueText = new GameObject("Диалог", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        npcDialogueText.transform.SetParent(parent.transform, false);
        npcDialogueText.rectTransform.anchoredPosition = new Vector2(0, -160);
        npcDialogueText.fontSize = 20;
        npcDialogueText.alignment = TextAlignmentOptions.Center;
        npcDialogueText.text = "Диалог появится здесь";

        var backBtn = CreateButton(parent.transform, "Назад", new Vector2(0, -200));
        backBtn.onClick.AddListener(ShowMainMenu);
    }

    void CreateIconGeneratorPanel(GameObject parent)
    {
        CreateTitle(parent.transform, "Генератор икон");
        var backBtn = CreateButton(parent.transform, "Назад", new Vector2(0, -150));
        backBtn.onClick.AddListener(ShowMainMenu);
    }

    // ======================= PANEL CONTROL =======================

    public void ShowStoryTellerPanel() => ShowOnly(storyTellerPanel);
    public void ShowNPCPanel() => ShowOnly(npcPanel);
    public void ShowIconGeneratorPanel() => ShowOnly(iconGeneratorPanel);
    public void ShowMainMenu() => ShowOnly(mainMenu);

    void ShowOnly(GameObject target)
    {
        mainMenu?.SetActive(false);
        storyTellerPanel?.SetActive(false);
        npcPanel?.SetActive(false);
        iconGeneratorPanel?.SetActive(false);
        target?.SetActive(true);
    }
}
