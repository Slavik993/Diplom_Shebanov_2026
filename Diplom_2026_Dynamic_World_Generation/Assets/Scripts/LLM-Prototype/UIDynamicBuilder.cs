using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Создаёт UI-панели для систем: Сказитель, Контроллер NPC, Генератор икон.
/// Работает и в PlayMode, и в Editor.
/// </summary>
public class UIDynamicBuilder : MonoBehaviour
{
    [Header("Панели")]
    public GameObject mainMenu;
    public GameObject storyTellerPanel;
    public GameObject npcPanel;
    public GameObject iconGeneratorPanel;

    [Header("Элементы NPC-панели (для LLMUIBinder)")]
    public TMP_InputField npcNameField;
    public TMP_InputField npcEnvironmentField; // 🆕 Окружение
    public TMP_Dropdown npcRelationDropdown;
    public TMP_Dropdown npcEmotionDropdown;
    public TMP_InputField npcReactionField;
    public Button npcGenerateButton;
    public TextMeshProUGUI npcDialogueText;

    private Canvas canvas;

    [Header("Элеменыты Storyteller-панели")]

    public TMP_InputField storyThemeField;
    public TMP_Dropdown storyStyleDropdown;
    public TMP_InputField storyLengthField;
    public Button storyGenerateButton;
    public TextMeshProUGUI storyOutputText;

    [Header("Элементы Icon Generator-панели")]

    public TMP_InputField iconDescriptionField;
    public TMP_Dropdown iconStyleDropdown;
    public TMP_InputField iconSizeField;
    public Button iconGenerateButton;
    public TextMeshProUGUI iconStatusText;

    void Awake()
    {
        BuildUI();
    }

    public void CreateUI() => BuildUI(); // 👈 для вызова из редактора

    void BuildUI()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CreateMainMenu();
        CreateNPCPanel();
        CreateStoryTellerPanel();
        CreateIconPanel();

        ShowOnly(mainMenu);
    }

    // === Панели ===

    void CreateMainMenu()
    {
        mainMenu = CreatePanel("MainMenu");

        CreateLabel(mainMenu.transform, "Главное меню", new Vector2(0, 180));

        CreateButton(mainMenu.transform, "Сказитель историй", new Vector2(0, 100),
            () => ShowOnly(storyTellerPanel));

        CreateButton(mainMenu.transform, "Контроллер NPC", new Vector2(0, 40),
            () => ShowOnly(npcPanel));

        CreateButton(mainMenu.transform, "Генератор икон", new Vector2(0, -20),
            () => ShowOnly(iconGeneratorPanel));
    }

    void CreateNPCPanel()
    {
        npcPanel = CreatePanel("NPCPanel");
        npcPanel.SetActive(false);

        CreateLabel(npcPanel.transform, "Контроллер NPC", new Vector2(0, 200));

        // Имя
        CreateLabel(npcPanel.transform, "Имя персонажа", new Vector2(0, 140));
        npcNameField = CreateInputField(npcPanel.transform, new Vector2(0, 110));

        // 🆕 Окружение
        CreateLabel(npcPanel.transform, "Окружение (место действия)", new Vector2(0, 70));
        npcEnvironmentField = CreateInputField(npcPanel.transform, new Vector2(0, 40));
        npcEnvironmentField.text = "таверна"; // дефолт

        // Отношения
        CreateLabel(npcPanel.transform, "Отношение к игроку", new Vector2(0, 0));
        npcRelationDropdown = CreateDropdown(npcPanel.transform,
            new string[] { "дружелюбный", "нейтральный", "враждебный" }, new Vector2(0, -30));

        // Эмоция
        CreateLabel(npcPanel.transform, "Эмоция", new Vector2(0, -70));
        npcEmotionDropdown = CreateDropdown(npcPanel.transform,
            new string[] { "спокойный", "сердитый", "радостный", "испуганный" }, new Vector2(0, -100));

        // Реакция
        CreateLabel(npcPanel.transform, "Реакция (0–100)", new Vector2(0, -140));
        npcReactionField = CreateInputField(npcPanel.transform, new Vector2(0, -170));

        npcGenerateButton = CreateButton(npcPanel.transform, "Сгенерировать диалог", new Vector2(0, -220), null);

        npcDialogueText = CreateLabel(npcPanel.transform, "Диалог появится здесь", new Vector2(0, -260), 18, FontStyles.Italic);

        CreateButton(npcPanel.transform, "Назад", new Vector2(0, -310), () => ShowOnly(mainMenu));
    }

    void CreateStoryTellerPanel()
    {
        storyTellerPanel = CreatePanel("StoryTellerPanel");
        storyTellerPanel.SetActive(false);

        CreateLabel(storyTellerPanel.transform, "Сказитель историй", new Vector2(0, 200));

        CreateLabel(storyTellerPanel.transform, "Тема истории", new Vector2(0, 140));
        storyThemeField = CreateInputField(storyTellerPanel.transform, new Vector2(0, 110));

        CreateLabel(storyTellerPanel.transform, "Стиль повествования", new Vector2(0, 70));
        storyStyleDropdown = CreateDropdown(storyTellerPanel.transform,
            new string[] { "сказочный", "драматический", "приключенческий" }, new Vector2(0, 40));

        CreateLabel(storyTellerPanel.transform, "Длина истории (слов)", new Vector2(0, 0));
        storyLengthField = CreateInputField(storyTellerPanel.transform, new Vector2(0, -30));

        storyGenerateButton = CreateButton(storyTellerPanel.transform, "Сгенерировать историю", new Vector2(0, -80), null);

        storyOutputText = CreateLabel(storyTellerPanel.transform, "Текст истории появится здесь", new Vector2(0, -140), 18, FontStyles.Italic);

        CreateButton(storyTellerPanel.transform, "Назад", new Vector2(0, -220), () => ShowOnly(mainMenu));
    }

    void CreateIconPanel()
    {
        iconGeneratorPanel = CreatePanel("IconGeneratorPanel");
        iconGeneratorPanel.SetActive(false);

        CreateLabel(iconGeneratorPanel.transform, "Генератор икон", new Vector2(0, 200));

        CreateLabel(iconGeneratorPanel.transform, "Описание иконки", new Vector2(0, 140));
        iconDescriptionField = CreateInputField(iconGeneratorPanel.transform, new Vector2(0, 110));

        CreateLabel(iconGeneratorPanel.transform, "Стиль иконки", new Vector2(0, 70));
        iconStyleDropdown = CreateDropdown(iconGeneratorPanel.transform,
            new string[] { "2D", "3D", "пиксель-арт" }, new Vector2(0, 40));

        CreateLabel(iconGeneratorPanel.transform, "Размер иконки (px)", new Vector2(0, 0));
        iconSizeField = CreateInputField(iconGeneratorPanel.transform, new Vector2(0, -30));

        iconGenerateButton = CreateButton(iconGeneratorPanel.transform, "Сгенерировать иконку", new Vector2(0, -80), null);

        iconStatusText = CreateLabel(iconGeneratorPanel.transform, "Статус: ожидание...", new Vector2(0, -140), 18, FontStyles.Italic);

        CreateButton(iconGeneratorPanel.transform, "Назад", new Vector2(0, -220), () => ShowOnly(mainMenu));
    }

    // === Вспомогательные ===

    GameObject CreatePanel(string name)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 550);
        rect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);
        panel.GetComponent<RectTransform>().SetAsLastSibling();
        return panel;
    }

    TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 pos, int size = 20, FontStyles style = FontStyles.Bold)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = size;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = style;
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 40);
        return txt;
    }

    TMP_InputField CreateInputField(Transform parent, Vector2 pos)
    {
        var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 36);
        rect.anchoredPosition = pos;
        go.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f);

        var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(go.transform, false);
        var tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 18;
        tmp.color = Color.white;

        var field = go.GetComponent<TMP_InputField>();
        field.textComponent = tmp;
        return field;
    }

    Button CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220, 40);
        rect.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.2f, 0.3f, 0.6f);

        var txtObj = new GameObject("Text", typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(go.transform, false);
        var txt = txtObj.GetComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 18;
        txt.color = Color.white;

        var btn = go.GetComponent<Button>();
        if (onClick != null)
            btn.onClick.AddListener(onClick);

        return btn;
    }

    TMP_Dropdown CreateDropdown(Transform parent, string[] options, Vector2 pos)
    {
        var go = new GameObject("Dropdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 36);
        rect.anchoredPosition = pos;

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.25f, 0.3f);

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.targetGraphic = bg;

        var captionGO = new GameObject("Label", typeof(TextMeshProUGUI));
        captionGO.transform.SetParent(go.transform, false);
        var caption = captionGO.GetComponent<TextMeshProUGUI>();
        caption.text = options.Length > 0 ? options[0] : "";
        caption.fontSize = 18;
        caption.color = Color.white;
        caption.alignment = TextAlignmentOptions.MidlineLeft;

        var captionRect = captionGO.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(10, 0);
        captionRect.offsetMax = new Vector2(-10, 0);

        dropdown.captionText = caption;

        dropdown.options.Clear();
        foreach (var opt in options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(opt));
        dropdown.RefreshShownValue();

        return dropdown;
    }

    public void ShowOnly(GameObject target)
    {
        mainMenu?.SetActive(false);
        storyTellerPanel?.SetActive(false);
        npcPanel?.SetActive(false);
        iconGeneratorPanel?.SetActive(false);
        target?.SetActive(true);
    }
}
