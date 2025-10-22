using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Создаёт UI-панели для систем: Сказитель, Контроллер NPC, Генератор икон.
/// Работает и в PlayMode, и в Editor.
/// Теперь добавлены кнопки 💾 для сохранения результатов генерации.
/// </summary>
public class UIDynamicBuilder : MonoBehaviour
{
    [Header("Панели")]
    public GameObject mainMenu;
    public GameObject storyTellerPanel;
    public GameObject npcPanel;
    public GameObject iconGeneratorPanel;

    [Header("UI загрузки")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;
    public Image loadingSpinner;

    [Header("Элементы NPC-панели")]
    public TMP_InputField npcNameField;
    public TMP_InputField npcEnvironmentField;
    public TMP_Dropdown npcRelationDropdown;
    public TMP_Dropdown npcEmotionDropdown;
    public TMP_InputField npcReactionField;
    public Button npcGenerateButton;
    public Button npcSaveButton; // 💾 новая кнопка
    public TextMeshProUGUI npcDialogueText;

    [Header("Элементы Storyteller-панели")]
    public TMP_InputField storyThemeField;
    public TMP_Dropdown storyStyleDropdown;
    public TMP_InputField storyLengthField;
    public TMP_Dropdown questTypeDropdown;
    public Button storyGenerateButton;
    public Button storySaveButton; // 💾 новая кнопка
    public TextMeshProUGUI storyOutputText;

    [Header("Элементы Icon Generator-панели")]
    public TMP_InputField iconDescriptionField;
    public TMP_Dropdown iconStyleDropdown;
    public TMP_InputField iconSizeField;
    public Button iconGenerateButton;
    public Button iconSaveButton; // 💾 новая кнопка
    public TextMeshProUGUI iconStatusText;

    private Canvas canvas;

    void Awake() => BuildUI();

    public void CreateUI() => BuildUI();

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

    // === ПАНЕЛИ ===

    void CreateMainMenu()
    {
        mainMenu = CreatePanel("MainMenu");
        CreateLabel(mainMenu.transform, "Главное меню", new Vector2(0, 180));

        CreateButton(mainMenu.transform, "Сказитель историй", new Vector2(0, 100), () => ShowOnly(storyTellerPanel));
        CreateButton(mainMenu.transform, "Контроллер NPC", new Vector2(0, 40), () => ShowOnly(npcPanel));
        CreateButton(mainMenu.transform, "Генератор икон", new Vector2(0, -20), () => ShowOnly(iconGeneratorPanel));
    }

    void CreateNPCPanel()
    {
        npcPanel = CreatePanel("NPCPanel");
        npcPanel.SetActive(false);
        CreateLabel(npcPanel.transform, "Контроллер NPC", new Vector2(0, 200));

        CreateLabel(npcPanel.transform, "Имя персонажа", new Vector2(0, 140));
        npcNameField = CreateInputField(npcPanel.transform, new Vector2(0, 110));

        CreateLabel(npcPanel.transform, "Окружение (место действия)", new Vector2(0, 70));
        npcEnvironmentField = CreateInputField(npcPanel.transform, new Vector2(0, 40));
        npcEnvironmentField.text = "таверна";

        CreateLabel(npcPanel.transform, "Отношение к игроку", new Vector2(0, 0));
        npcRelationDropdown = CreateDropdown(npcPanel.transform,
            new[] { "дружелюбный", "нейтральный", "враждебный" }, new Vector2(0, -30));

        CreateLabel(npcPanel.transform, "Эмоция", new Vector2(0, -70));
        npcEmotionDropdown = CreateDropdown(npcPanel.transform,
            new[] { "спокойный", "сердитый", "радостный", "испуганный" }, new Vector2(0, -100));

        CreateLabel(npcPanel.transform, "Реакция (0–100)", new Vector2(0, -140));
        npcReactionField = CreateInputField(npcPanel.transform, new Vector2(0, -170));

        npcGenerateButton = CreateButton(npcPanel.transform, "Сгенерировать диалог", new Vector2(0, -220), () =>
        {
            npcDialogueText.text = $"NPC {npcNameField.text} отвечает: Пример сгенерированного текста...";
        });

        npcDialogueText = CreateLabel(npcPanel.transform, "Диалог появится здесь", new Vector2(0, -250), 18, FontStyles.Italic);

        npcSaveButton = CreateButton(npcPanel.transform, "💾 Сохранить диалог", new Vector2(0, -280), () =>
        {
            GeneratedContentSaver.SaveDialogue(npcDialogueText.text);
        });

        CreateButton(npcPanel.transform, "Назад", new Vector2(0, -330), () => ShowOnly(mainMenu));
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
            new[] { "постапокалипсис", "фэнтези", "реализм", "приключение", "киберпанк", "стимпанк", "сказочный", "драма" },
            new Vector2(0, 40));

        CreateLabel(storyTellerPanel.transform, "Тип квеста", new Vector2(0, 0));
        questTypeDropdown = CreateDropdown(storyTellerPanel.transform,
            new[] { "Диалоговый", "Поисковый", "Исторический", "Образовательный", "Загадочный", "Исследовательский", "Научный", "Повествовательный", "Социальный", "Ролевой" },
            new Vector2(0, -30));

        CreateLabel(storyTellerPanel.transform, "Длина истории (слов)", new Vector2(0, -70));
        storyLengthField = CreateInputField(storyTellerPanel.transform, new Vector2(0, -100));

        storyGenerateButton = CreateButton(storyTellerPanel.transform, "Сгенерировать историю", new Vector2(0, -150), () =>
        {
            storyOutputText.text = $"История в жанре {storyStyleDropdown.captionText.text}: ...пример текста...";
        });

        storyOutputText = CreateLabel(storyTellerPanel.transform, "Текст истории появится здесь", new Vector2(0, -210), 18, FontStyles.Italic);

        storySaveButton = CreateButton(storyTellerPanel.transform, "💾 Сохранить историю", new Vector2(0, -250), () =>
        {
            GeneratedContentSaver.SaveQuest(storyOutputText.text);
        });

        CreateButton(storyTellerPanel.transform, "Назад", new Vector2(0, -300), () => ShowOnly(mainMenu));
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
            new[] { "2D", "3D", "пиксель-арт", "аниме", "реализм", "векторный", "иллюстрация", "фэнтези", "ретро" },
            new Vector2(0, 40));

        CreateLabel(iconGeneratorPanel.transform, "Размер иконки (px)", new Vector2(0, 0));
        iconSizeField = CreateInputField(iconGeneratorPanel.transform, new Vector2(0, -30));

        iconGenerateButton = CreateButton(iconGeneratorPanel.transform, "Сгенерировать иконку", new Vector2(0, -80), () =>
        {
            iconStatusText.text = "🖼 Сгенерирована тестовая иконка (заглушка).";
        });

        iconStatusText = CreateLabel(iconGeneratorPanel.transform, "Статус: ожидание...", new Vector2(0, -130), 18, FontStyles.Italic);

        iconSaveButton = CreateButton(iconGeneratorPanel.transform, "💾 Сохранить иконку", new Vector2(0, -170), () =>
        {
            // ⚠️ если у тебя есть Texture2D после ComfyUI — передай его сюда
            Texture2D dummy = new Texture2D(64, 64);
            GeneratedContentSaver.SaveVisual(dummy);
        });

        CreateButton(iconGeneratorPanel.transform, "Назад", new Vector2(0, -230), () => ShowOnly(mainMenu));
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
        go.GetComponent<Image>().color = new Color(0.2f, 0.3f, 0.6f);

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
        // === Создаём объект Dropdown ===
        var go = new GameObject("Dropdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 36);
        rect.anchoredPosition = pos;

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.22f, 0.28f);

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.targetGraphic = bg;

        // === Caption Label ===
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
        captionRect.offsetMax = new Vector2(-25, 0);
        dropdown.captionText = caption;

        // === Template ===
        var templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        templateGO.transform.SetParent(go.transform, false);
        var templateRect = templateGO.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.sizeDelta = new Vector2(0, 150);
        templateGO.SetActive(false);

        var templateImage = templateGO.GetComponent<Image>();
        templateImage.color = new Color(0.1f, 0.12f, 0.15f, 1f);

        // === Viewport ===
        var viewportGO = new GameObject("Viewport", typeof(RectMask2D), typeof(RectTransform));
        viewportGO.transform.SetParent(templateGO.transform, false);
        var viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;

        // === Content ===
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;

        var layout = contentGO.GetComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.spacing = 8f;

        var fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // === Item prefab (Toggle) ===
        var itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image), typeof(LayoutElement));
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRect = itemGO.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 32);

        var layoutElement = itemGO.GetComponent<LayoutElement>();
        layoutElement.minHeight = 32;
        layoutElement.preferredHeight = 32;

        var itemBG = itemGO.GetComponent<Image>();
        itemBG.color = new Color(0.25f, 0.3f, 0.35f);

        var itemToggle = itemGO.GetComponent<Toggle>();
        itemToggle.transition = Selectable.Transition.ColorTint;
        itemToggle.interactable = true;
        itemToggle.targetGraphic = itemBG;

        // === Checkmark ===
        var checkmarkGO = new GameObject("Checkmark", typeof(Image));
        checkmarkGO.transform.SetParent(itemGO.transform, false);
        var checkmark = checkmarkGO.GetComponent<Image>();
        checkmark.color = new Color(1, 1, 1, 0.6f);
        var checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0, 0.5f);
        checkmarkRect.anchoredPosition = new Vector2(10, 0);
        checkmarkRect.sizeDelta = new Vector2(15, 15);
        itemToggle.graphic = checkmark;

        // === Label для пункта ===
        var itemLabelGO = new GameObject("Item Label", typeof(TextMeshProUGUI));
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        var itemLabel = itemLabelGO.GetComponent<TextMeshProUGUI>();
        itemLabel.text = "Option";
        itemLabel.fontSize = 18;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;

        var itemLabelRect = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = new Vector2(0, 0);
        itemLabelRect.anchorMax = new Vector2(1, 1);
        itemLabelRect.offsetMin = new Vector2(35, 0);
        itemLabelRect.offsetMax = new Vector2(-10, 0);

        // === ScrollRect и Dropdown binding ===
        var scrollRect = templateGO.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        dropdown.itemImage = itemBG;
        dropdown.options.Clear();

        foreach (var opt in options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(opt));

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
