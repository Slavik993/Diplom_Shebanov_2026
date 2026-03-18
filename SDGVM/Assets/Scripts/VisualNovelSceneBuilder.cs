using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Программно строит всю UI-сцену визуальной новеллы в одном Canvas.
/// Создаёт: форму студента, панель VN, панель результатов, EventSystem и экран загрузки.
/// Все элементы создаются из кода — никаких префабов не нужно.
/// </summary>
public class VisualNovelSceneBuilder : MonoBehaviour
{
    // ═══════════════════════════════════════
    // Ссылки на созданные UI элементы
    // ═══════════════════════════════════════

    [HideInInspector] public Canvas mainCanvas;

    // Форма студента
    [HideInInspector] public GameObject studentFormPanel;
    [HideInInspector] public TMP_InputField inputSurname;
    [HideInInspector] public TMP_InputField inputFirstName;
    [HideInInspector] public TMP_InputField inputPatronymic;
    [HideInInspector] public TMP_InputField inputGroup;
    [HideInInspector] public Button btnStart;

    // VN Player
    [HideInInspector] public GameObject vnPanel;
    [HideInInspector] public RawImage backgroundImage;
    [HideInInspector] public RawImage characterLeft;
    [HideInInspector] public RawImage characterCenter;
    [HideInInspector] public RawImage characterRight;
    [HideInInspector] public GameObject dialoguePanel;
    [HideInInspector] public TMP_Text speakerNameText;
    [HideInInspector] public TMP_Text dialogueText;
    [HideInInspector] public GameObject choicesContainer;
    [HideInInspector] public Button btnNext;
    [HideInInspector] public TMP_Text counterCorrectText;
    [HideInInspector] public TMP_Text counterWrongText;
    [HideInInspector] public TMP_Text titleText;

    // Результаты
    [HideInInspector] public GameObject resultsPanel;
    [HideInInspector] public TMP_Text resultsText;
    [HideInInspector] public Button btnRestart;

    // Экран загрузки
    [HideInInspector] public GameObject loadingPanel;
    [HideInInspector] public TMP_Text loadingText;
    [HideInInspector] public Slider loadingProgress;

    // Динамические кнопки
    [HideInInspector] public List<GameObject> choiceButtons = new List<GameObject>();

    // ═══════════════════════════════════════
    // ЦВЕТА И СТИЛИ
    // ═══════════════════════════════════════

    // Основные цвета
    private static readonly Color BG_DARK = new Color(0.05f, 0.05f, 0.08f, 1f);
    private static readonly Color DIALOGUE_BG = new Color(0.0f, 0.0f, 0.0f, 0.65f);
    private static readonly Color CHOICE_TEAL = new Color(0.33f, 0.73f, 0.73f, 0.92f);
    private static readonly Color CHOICE_HOVER = new Color(0.40f, 0.80f, 0.80f, 1f);
    private static readonly Color CHOICE_PRESSED = new Color(0.25f, 0.60f, 0.60f, 1f);
    private static readonly Color TEXT_WHITE = Color.white;
    private static readonly Color TEXT_YELLOW = new Color(1f, 0.92f, 0.45f, 1f);
    private static readonly Color COUNTER_GREEN = new Color(0.47f, 0.87f, 0.47f, 1f);
    private static readonly Color COUNTER_RED = new Color(1f, 0.40f, 0.40f, 1f);
    private static readonly Color INPUT_BG = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color BUTTON_LIGHT = new Color(0.85f, 0.85f, 0.85f, 1f);

    /// <summary>
    /// Строит всю сцену программно
    /// </summary>
    public void BuildScene()
    {
        BuildEventSystem();
        BuildCanvas();
        BuildStudentForm();
        BuildVNPanel();
        BuildResultsPanel();
        BuildLoadingPanel();

        // По умолчанию показываем форму студента
        ShowPanel(studentFormPanel);
    }

    public void ShowPanel(GameObject panel)
    {
        if (studentFormPanel != null) studentFormPanel.SetActive(panel == studentFormPanel);
        if (vnPanel != null) vnPanel.SetActive(panel == vnPanel);
        if (resultsPanel != null) resultsPanel.SetActive(panel == resultsPanel);
        if (loadingPanel != null) loadingPanel.SetActive(panel == loadingPanel);
    }

    // ═══════════════════════════════════════
    // EVENT SYSTEM
    // ═══════════════════════════════════════

    private void BuildEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    // ═══════════════════════════════════════
    // CANVAS
    // ═══════════════════════════════════════

    private void BuildCanvas()
    {
        GameObject canvasObj = new GameObject("VN_Canvas");
        canvasObj.transform.SetParent(transform, false);

        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // ═══════════════════════════════════════
    // ФОРМА СТУДЕНТА (Скриншот 3)
    // ═══════════════════════════════════════

    private void BuildStudentForm()
    {
        studentFormPanel = CreatePanel("StudentFormPanel", mainCanvas.transform);
        var formBg = studentFormPanel.GetComponent<Image>();
        formBg.color = BG_DARK;

        // Центральный контейнер
        var container = CreateVerticalGroup("FormContainer", studentFormPanel.transform, 30f);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.25f, 0.15f);
        containerRect.anchorMax = new Vector2(0.75f, 0.85f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Заголовок
        titleText = CreateText("Визуальная новелла МУИВ", container.transform, 36, TEXT_WHITE, TextAlignmentOptions.Center);

        // Поля ввода
        inputSurname = CreateInputField("Введите Фамилию", container.transform);
        inputFirstName = CreateInputField("Введите Имя", container.transform);
        inputPatronymic = CreateInputField("Введите Отчество", container.transform);
        inputGroup = CreateInputField("Введите свою группу", container.transform);

        // Кнопка Старт
        btnStart = CreateButton("Старт", container.transform, BUTTON_LIGHT, new Color(0.3f, 0.3f, 0.3f, 1f));
        var btnRect = btnStart.GetComponent<RectTransform>();
        var btnLE = btnStart.gameObject.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 55f;
        btnLE.preferredWidth = 250f;

        // Центрируем кнопку
        var btnLayout = btnStart.transform.parent;
    }

    // ═══════════════════════════════════════
    // VN PLAYER PANEL (Скриншоты 1, 2, 4)
    // ═══════════════════════════════════════

    private void BuildVNPanel()
    {
        vnPanel = CreatePanel("VNPanel", mainCanvas.transform);

        // === Фон ===
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(vnPanel.transform, false);
        backgroundImage = bgObj.AddComponent<RawImage>();
        backgroundImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        StretchToParent(bgRect);
        backgroundImage.raycastTarget = false;

        // === Слоты персонажей ===
        characterLeft = CreateCharacterSlot("CharLeft", vnPanel.transform, 0.02f, 0.12f, 0.35f, 0.88f);
        characterCenter = CreateCharacterSlot("CharCenter", vnPanel.transform, 0.30f, 0.10f, 0.70f, 0.90f);
        characterRight = CreateCharacterSlot("CharRight", vnPanel.transform, 0.65f, 0.12f, 0.98f, 0.88f);

        // === Счётчики ответов (верхний правый угол) ===
        var counterPanel = new GameObject("CounterPanel");
        counterPanel.transform.SetParent(vnPanel.transform, false);
        var counterBg = counterPanel.AddComponent<Image>();
        counterBg.color = new Color(0, 0, 0, 0.5f);
        var counterRect = counterPanel.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0.82f, 0.92f);
        counterRect.anchorMax = new Vector2(0.99f, 0.99f);
        counterRect.offsetMin = Vector2.zero;
        counterRect.offsetMax = Vector2.zero;

        var counterHGroup = counterPanel.AddComponent<HorizontalLayoutGroup>();
        counterHGroup.spacing = 15f;
        counterHGroup.padding = new RectOffset(15, 15, 5, 5);
        counterHGroup.childAlignment = TextAnchor.MiddleCenter;
        counterHGroup.childControlWidth = true;
        counterHGroup.childControlHeight = true;

        counterCorrectText = CreateText("✓ 0", counterPanel.transform, 28, COUNTER_GREEN, TextAlignmentOptions.Center);
        counterWrongText = CreateText("✗ 0", counterPanel.transform, 28, COUNTER_RED, TextAlignmentOptions.Center);

        // === Диалоговая панель (нижняя часть экрана) ===
        dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(vnPanel.transform, false);
        var dialogueBg = dialoguePanel.AddComponent<Image>();
        dialogueBg.color = DIALOGUE_BG;
        var dialogueRect = dialoguePanel.GetComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0.02f, 0.02f);
        dialogueRect.anchorMax = new Vector2(0.98f, 0.35f);
        dialogueRect.offsetMin = Vector2.zero;
        dialogueRect.offsetMax = Vector2.zero;

        var dialogueVGroup = dialoguePanel.AddComponent<VerticalLayoutGroup>();
        dialogueVGroup.spacing = 8f;
        dialogueVGroup.padding = new RectOffset(25, 25, 15, 15);
        dialogueVGroup.childControlWidth = true;
        dialogueVGroup.childControlHeight = false;
        dialogueVGroup.childForceExpandWidth = true;
        dialogueVGroup.childForceExpandHeight = false;

        // Имя говорящего (курсив, жёлтый)
        speakerNameText = CreateText("", dialoguePanel.transform, 24, TEXT_YELLOW, TextAlignmentOptions.TopLeft);
        speakerNameText.fontStyle = FontStyles.Italic;
        var speakerLE = speakerNameText.gameObject.AddComponent<LayoutElement>();
        speakerLE.preferredHeight = 35f;

        // Текст диалога
        dialogueText = CreateText("", dialoguePanel.transform, 22, TEXT_WHITE, TextAlignmentOptions.TopLeft);
        dialogueText.enableWordWrapping = true;
        var textLE = dialogueText.gameObject.AddComponent<LayoutElement>();
        textLE.flexibleHeight = 1f;
        textLE.minHeight = 60f;

        // === Контейнер для кнопок выбора (центр экрана) ===
        choicesContainer = new GameObject("ChoicesContainer");
        choicesContainer.transform.SetParent(vnPanel.transform, false);
        var choicesRect = choicesContainer.GetComponent<RectTransform>();
        if (choicesRect == null) choicesRect = choicesContainer.AddComponent<RectTransform>();
        choicesRect.anchorMin = new Vector2(0.15f, 0.36f);
        choicesRect.anchorMax = new Vector2(0.85f, 0.85f);
        choicesRect.offsetMin = Vector2.zero;
        choicesRect.offsetMax = Vector2.zero;

        var choicesVGroup = choicesContainer.AddComponent<VerticalLayoutGroup>();
        choicesVGroup.spacing = 12f;
        choicesVGroup.padding = new RectOffset(20, 20, 10, 10);
        choicesVGroup.childControlWidth = true;
        choicesVGroup.childControlHeight = false;
        choicesVGroup.childForceExpandWidth = true;
        choicesVGroup.childForceExpandHeight = false;
        choicesVGroup.childAlignment = TextAnchor.MiddleCenter;

        var choicesFitter = choicesContainer.AddComponent<ContentSizeFitter>();
        choicesFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // === Кнопка "Далее" (для текстовых страниц без выбора) ===
        var nextBtnObj = new GameObject("BtnNext");
        nextBtnObj.transform.SetParent(vnPanel.transform, false);
        var nextRect = nextBtnObj.AddComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.38f, 0.38f);
        nextRect.anchorMax = new Vector2(0.62f, 0.44f);
        nextRect.offsetMin = Vector2.zero;
        nextRect.offsetMax = Vector2.zero;

        var nextBg = nextBtnObj.AddComponent<Image>();
        nextBg.color = CHOICE_TEAL;
        btnNext = nextBtnObj.AddComponent<Button>();
        var nextColors = btnNext.colors;
        nextColors.normalColor = CHOICE_TEAL;
        nextColors.highlightedColor = CHOICE_HOVER;
        nextColors.pressedColor = CHOICE_PRESSED;
        btnNext.colors = nextColors;
        btnNext.targetGraphic = nextBg;

        var nextTextObj = new GameObject("Label");
        nextTextObj.transform.SetParent(nextBtnObj.transform, false);
        var nextText = nextTextObj.AddComponent<TextMeshProUGUI>();
        nextText.text = "Далее ▶";
        nextText.fontSize = 24;
        nextText.color = TEXT_WHITE;
        nextText.alignment = TextAlignmentOptions.Center;
        var nextTextRect = nextTextObj.GetComponent<RectTransform>();
        StretchToParent(nextTextRect);
    }

    // ═══════════════════════════════════════
    // ПАНЕЛЬ РЕЗУЛЬТАТОВ
    // ═══════════════════════════════════════

    private void BuildResultsPanel()
    {
        resultsPanel = CreatePanel("ResultsPanel", mainCanvas.transform);
        var resultsBg = resultsPanel.GetComponent<Image>();
        resultsBg.color = BG_DARK;

        var container = CreateVerticalGroup("ResultsContainer", resultsPanel.transform, 25f);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.15f, 0.15f);
        containerRect.anchorMax = new Vector2(0.85f, 0.85f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        CreateText("Результаты", container.transform, 42, TEXT_YELLOW, TextAlignmentOptions.Center);

        resultsText = CreateText("", container.transform, 26, TEXT_WHITE, TextAlignmentOptions.Center);
        resultsText.enableWordWrapping = true;
        // Убираем фиксированную высоту, чтобы многострочный текст (результаты) не слипался
        var resLE = resultsText.GetComponent<LayoutElement>();
        if (resLE != null) {
            resLE.preferredHeight = -1;
            resLE.minHeight = 120f;
            resLE.flexibleHeight = 1f;
        }

        btnRestart = CreateButton("Начать заново", container.transform, CHOICE_TEAL, TEXT_WHITE);
        var restartLE = btnRestart.gameObject.AddComponent<LayoutElement>();
        restartLE.preferredHeight = 55f;
        restartLE.preferredWidth = 300f;
    }

    // ═══════════════════════════════════════
    // ЭКРАН ЗАГРУЗКИ / ГЕНЕРАЦИИ
    // ═══════════════════════════════════════

    private void BuildLoadingPanel()
    {
        loadingPanel = CreatePanel("LoadingPanel", mainCanvas.transform);
        var bg = loadingPanel.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Почти непрозрачный тёмный фон

        var container = CreateVerticalGroup("LoadingContainer", loadingPanel.transform, 20f);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.2f, 0.3f);
        containerRect.anchorMax = new Vector2(0.8f, 0.7f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        CreateText("Генерация сцены", container.transform, 32, TEXT_YELLOW, TextAlignmentOptions.Center);
        
        loadingText = CreateText("Инициализация...", container.transform, 22, TEXT_WHITE, TextAlignmentOptions.Center);

        // Прогресс бар
        var sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(container.transform, false);
        var sliderLE = sliderObj.AddComponent<LayoutElement>();
        sliderLE.preferredHeight = 20f;
        sliderLE.flexibleWidth = 1f;

        var sliderBgObj = new GameObject("Background");
        sliderBgObj.transform.SetParent(sliderObj.transform, false);
        var sliderBgImage = sliderBgObj.AddComponent<Image>();
        sliderBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var sliderBgRect = sliderBgObj.GetComponent<RectTransform>();
        StretchToParent(sliderBgRect);

        var fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        StretchToParent(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        var fillImage = fillObj.AddComponent<Image>();
        fillImage.color = CHOICE_TEAL;
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        loadingProgress = sliderObj.AddComponent<Slider>();
        loadingProgress.fillRect = fillRect;
        loadingProgress.minValue = 0f;
        loadingProgress.maxValue = 1f;
        loadingProgress.value = 0f;
        loadingProgress.interactable = false;

        loadingPanel.SetActive(false); // Скрыт по умолчанию
    }

    // ═══════════════════════════════════════
    // ДИНАМИЧЕСКИЕ КНОПКИ ВЫБОРА
    // ═══════════════════════════════════════

    /// <summary>
    /// Создаёт кнопку выбора в контейнере (стиль как на скриншоте 1 — бирюзовая)
    /// </summary>
    public Button CreateChoiceButton(string text)
    {
        var btnObj = new GameObject("ChoiceBtn");
        btnObj.transform.SetParent(choicesContainer.transform, false);

        var btnBg = btnObj.AddComponent<Image>();
        btnBg.color = CHOICE_TEAL;

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = CHOICE_TEAL;
        colors.highlightedColor = CHOICE_HOVER;
        colors.pressedColor = CHOICE_PRESSED;
        btn.colors = colors;
        btn.targetGraphic = btnBg;

        var le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = 65f;
        le.minHeight = 50f;
        le.flexibleWidth = 1f;

        // Текст кнопки
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        var tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 20;
        tmpText.color = TEXT_WHITE;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enableWordWrapping = true;
        tmpText.raycastTarget = false;
        var textRect = textObj.GetComponent<RectTransform>();
        StretchToParent(textRect);
        textRect.offsetMin = new Vector2(15, 5);
        textRect.offsetMax = new Vector2(-15, -5);

        choiceButtons.Add(btnObj);
        return btn;
    }

    /// <summary>
    /// Очищает все кнопки выбора
    /// </summary>
    public void ClearChoiceButtons()
    {
        foreach (var btn in choiceButtons)
        {
            if (btn != null) Destroy(btn);
        }
        choiceButtons.Clear();
    }

    // ═══════════════════════════════════════
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ═══════════════════════════════════════

    private GameObject CreatePanel(string name, Transform parent)
    {
        var panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        var img = panelObj.AddComponent<Image>();
        img.color = BG_DARK;
        var rect = panelObj.GetComponent<RectTransform>();
        StretchToParent(rect);
        return panelObj;
    }

    private GameObject CreateVerticalGroup(string name, Transform parent, float spacing)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        StretchToParent(rect);

        var vlg = obj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        return obj;
    }

    private TMP_Text CreateText(string text, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var obj = new GameObject("Text_" + text.Substring(0, Mathf.Min(text.Length, 10)));
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize * 1.8f;
        le.flexibleWidth = 1f;

        return tmp;
    }

    private TMP_InputField CreateInputField(string placeholder, Transform parent)
    {
        var obj = new GameObject("Input_" + placeholder);
        obj.transform.SetParent(parent, false);

        var bg = obj.AddComponent<Image>();
        bg.color = INPUT_BG;

        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 55f;
        le.flexibleWidth = 1f;

        // Текстовая область
        var textAreaObj = new GameObject("TextArea");
        textAreaObj.transform.SetParent(obj.transform, false);
        var textAreaRect = textAreaObj.AddComponent<RectTransform>();
        StretchToParent(textAreaRect);
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        // Placeholder
        var placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textAreaObj.transform, false);
        var placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = placeholder;
        placeholderTmp.fontSize = 22;
        placeholderTmp.fontStyle = FontStyles.Italic;
        placeholderTmp.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
        placeholderTmp.enableWordWrapping = false;
        placeholderTmp.raycastTarget = false;
        var phRect = placeholderObj.GetComponent<RectTransform>();
        StretchToParent(phRect);

        // Input text
        var inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(textAreaObj.transform, false);
        var inputTmp = inputTextObj.AddComponent<TextMeshProUGUI>();
        inputTmp.fontSize = 22;
        inputTmp.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        inputTmp.alignment = TextAlignmentOptions.MidlineLeft;
        inputTmp.enableWordWrapping = false;
        var itRect = inputTextObj.GetComponent<RectTransform>();
        StretchToParent(itRect);

        // InputField
        var inputField = obj.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputTmp;
        inputField.placeholder = placeholderTmp;
        inputField.fontAsset = inputTmp.font;

        return inputField;
    }

    private Button CreateButton(string text, Transform parent, Color bgColor, Color textColor)
    {
        var obj = new GameObject("Btn_" + text);
        obj.transform.SetParent(parent, false);

        var bg = obj.AddComponent<Image>();
        bg.color = bgColor;

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = bg;

        // Текст
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var textRect = textObj.GetComponent<RectTransform>();
        StretchToParent(textRect);

        return btn;
    }

    private RawImage CreateCharacterSlot(string name, Transform parent, float xMin, float yMin, float xMax, float yMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var raw = obj.AddComponent<RawImage>();
        raw.color = new Color(1, 1, 1, 0); // Скрыт по умолчанию
        raw.raycastTarget = false;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return raw;
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
