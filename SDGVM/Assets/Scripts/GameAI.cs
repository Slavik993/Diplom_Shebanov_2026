using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LLMUnity;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System; 

public class GameAI : MonoBehaviour
{
    [Header("LLM NPC / Текст")]
    public LLMCharacter llmCharacter;

    [Header("==== PANEL 1: STORY (Настройки Квеста) ====")]
    public TMP_InputField inputPrompt;
    public TMP_InputField inputLength;
    public TMP_Dropdown dropdownStyle;
    public TMP_Dropdown dropdownType;
    public TMP_Dropdown dropdownDifficulty;
    // Новое поле для этой панели
    public TMP_Dropdown dropdownUniversityLocation; 

    [Header("==== PANEL 2: NPC (Настройки Персонажа) ====")]
    public TMP_Dropdown dropdownNPCEmotion;
    public TMP_Dropdown dropdownNPCRelation; // Можно скрыть, если не используется
    // Новые поля для этой панели
    public TMP_Dropdown dropdownNPCRole;          
    public TMP_Dropdown dropdownCulturalContext;
    public TMP_Dropdown dropdownLanguageLevel;
    [Tooltip("Введите описание личности NPC, включая внутренний конфликт (хочется, но нельзя)")]
    public TMP_InputField npcPersonalityInput; // Для исследования личности
    [Tooltip("Выберите проблемный кейс адаптации (0 = обычный режим)")]
    public TMP_Dropdown dropdownAdaptationCase;

    [Header("==== PANEL 3: ICON (Настройки Визуала) ====")]
    public TMP_InputField inputIconStyle;
    public TMP_InputField inputIconSize;

    [Header("==== NPC CHAT ====")]
    public Button playerSendButton;
    public ScrollRect chatScrollRect;
    public TMP_Text chatHistoryText;
    public TMP_InputField playerInput;

    [Header("==== TEXT OUTPUT CENTER ====")]
    public TMP_Text textStoryOutput;
    public ScrollRect storyScrollRect;

    [Header("==== IMAGE OUTPUT ====")]
    public RawImage iconPreview;

    [Header("==== BUTTONS ====")]
    public Button btnGenerate;
    public Button btnSaveAll;

    [Header("==== IMAGE GENERATION ====")]
    public ComfyUIManager comfy;

    [Header("==== AUTO SAVE SETTINGS ====")]
    public string saveFolderRoot = "QuestSessions";
    public bool autoSaveAfterGeneration = true;

    private string currentSessionFolder;
    private int generationCounter = 0;
    
    // Текущий случайный кейс для диалога
    private AdaptationCase currentRandomCase;

    void Start()
    {
        CreateSessionFolder();

        btnGenerate.onClick.AddListener(GenerateAll);
        btnSaveAll.onClick.AddListener(SaveAll);

        if (playerSendButton != null)
            playerSendButton.onClick.AddListener(SendPlayerMessage);

        if (playerInput != null)
        {
            playerInput.onSubmit.AddListener(_ => SendPlayerMessage());
            playerInput.onEndEdit.AddListener(text =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    SendPlayerMessage();
            });
        }

        // Настройка центрирования текста квеста
        if (textStoryOutput != null)
        {
            textStoryOutput.alignment = TextAlignmentOptions.Center;
            textStoryOutput.fontSize = 18; // Основной текст квеста — крупный
        }

        StartCoroutine(GenerateNPCResponse(""));
        
        // ПРИНУДИТЕЛЬНАЯ установка шрифта диалога (меньше чем текст квеста!)
        if (chatHistoryText != null)
        {
            chatHistoryText.fontSize = 14; // Мелкий шрифт для чата NPC
            chatHistoryText.enableWordWrapping = true;
        }
        // Принудительная стилизация UI под Steampunk
        ApplySteampunkStyle();
        
        // Автоматическое исправление Scroll View
        FixScrollViews();
    }

    private void ApplySteampunkStyle()
    {
        // Цвета стимпанка (латунь, старая бумага, темный металл)
        Color darkBrass = new Color(0.15f, 0.10f, 0.08f, 0.95f);
        Color oldPaper = new Color(0.92f, 0.88f, 0.76f, 1f);
        Color copperText = new Color(0.90f, 0.65f, 0.35f, 1f);

        if (chatScrollRect != null)
        {
            var img = chatScrollRect.GetComponent<Image>();
            if (img != null) img.color = darkBrass;
            var viewport = chatScrollRect.viewport != null ? chatScrollRect.viewport.GetComponent<Image>() : null;
            if (viewport != null) viewport.color = darkBrass;
        }

        if (storyScrollRect != null)
        {
            var img = storyScrollRect.GetComponent<Image>();
            if (img != null) img.color = darkBrass;
            var viewport = storyScrollRect.viewport != null ? storyScrollRect.viewport.GetComponent<Image>() : null;
            if (viewport != null) viewport.color = darkBrass;
        }

        if (textStoryOutput != null)
            textStoryOutput.color = oldPaper; // Цвет состаренной бумаги для квеста

        if (playerInput != null)
        {
            var img = playerInput.GetComponent<Image>();
            if (img != null) img.color = new Color(0.1f, 0.05f, 0.02f, 1f); // Очень темная медь 
            if (playerInput.textComponent != null)
                playerInput.textComponent.color = copperText;
        }
    }

    void FixScrollViews()
    {
        // 1. Исправление прокрутки Квеста (Story)
        if (storyScrollRect != null)
        {
            if (storyScrollRect.content != null && textStoryOutput != null)
            {
                textStoryOutput.transform.SetParent(storyScrollRect.content, false);
            }
            SetupContentLayout(storyScrollRect);
        }

        // 2. Исправление прокрутки Чата NPC
        if (chatScrollRect != null)
        {
            if (chatScrollRect.content != null && chatHistoryText != null)
            {
                chatHistoryText.transform.SetParent(chatScrollRect.content, false);
            }
            SetupContentLayout(chatScrollRect);
        }
    }

    // Хелпер для настройки Layout и Scrollbar
    void SetupContentLayout(ScrollRect scrollRect)
    {
        if (scrollRect == null || scrollRect.content == null) return;

        // 1. Находим и привязываем Scrollbar Vertical, если он потерялся
        if (scrollRect.verticalScrollbar == null)
        {
            var sb = scrollRect.GetComponentInChildren<Scrollbar>(); // Ищем любой
            // Или ищем по имени "Scrollbar Vertical"
            foreach(var s in scrollRect.GetComponentsInChildren<Scrollbar>(true))
            {
                if (s.direction == Scrollbar.Direction.BottomToTop) // Обычно вертикальный
                {
                    scrollRect.verticalScrollbar = s;
                    scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent; // Всегда показывать
                    s.gameObject.SetActive(true);
                    break;
                }
            }
        }

        // 2. Настройка Content с VerticalLayoutGroup
        var layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null) layoutGroup = scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
        
        layoutGroup.childControlHeight = true;  // Текст сам скажет высоту
        layoutGroup.childControlWidth = true;   // Текст сам скажет ширину (или растянется)
        layoutGroup.childForceExpandHeight = false; // Не растягивать насильно
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = 10f;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);

        // 3. Content Size Fitter (Слушает LayoutGroup)
        var fitter = scrollRect.content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // Ширина задается Viewport'ом

        // 4. Pivot и Anchors для Content
        var contentRect = scrollRect.content;
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.sizeDelta = new Vector2(0, 0); // Сброс размера (Fitter сам выставит высоту)

        // 5. Обновляем детей (Текст)
        // 5. Обновляем детей (Текст)
        foreach(Transform child in scrollRect.content)
        {
             // Сброс левых анкоров, чтобы LayoutGroup управлял ими
             // Но важно включить Wrapping у TMP
             var tmp = child.GetComponent<TMP_Text>();
             if (tmp != null)
             {
                 tmp.enableWordWrapping = true;
                 tmp.overflowMode = TextOverflowModes.Overflow; 
                 // НЕ перезаписываем fontSize — он задаётся в Start() отдельно для чата и квеста
                 tmp.alignment = TextAlignmentOptions.TopLeft;
             }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    void CreateSessionFolder()
    {
        string sessionName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        // Используем persistentDataPath вместо dataPath, чтобы избежать конфликтов с Unity meta-файлами
        currentSessionFolder = Path.Combine(Application.persistentDataPath, saveFolderRoot, sessionName);
        Directory.CreateDirectory(currentSessionFolder);
        Debug.Log($"Session folder: {currentSessionFolder}");
    }

    public void GenerateAll()
    {
        generationCounter++;
        StartCoroutine(GenerateAllSequence());
    }

    public void SendPlayerMessage()
    {
        if (string.IsNullOrWhiteSpace(playerInput.text)) return;

        string playerMessage = playerInput.text.Trim();
        AddChatMessage("Игрок", playerMessage);

        playerInput.text = "";
        playerInput.ActivateInputField();

        StartCoroutine(GenerateNPCResponse(playerMessage));
    }

    IEnumerator GenerateNPCResponse(string playerMessage)
    {
        if (!llmCharacter)
        {
            AddChatMessage("NPC", "Ошибка: нет связи с ИИ");
            yield break;
        }

        AddChatMessage("NPC", "…");
        StartCoroutine(ScrollDelayed());

        string systemPrompt;
        
        // Получаем образовательный контекст
        string npcRole = dropdownNPCRole != null ? dropdownNPCRole.captionText.text : "Наставник";
        string culturalContext = dropdownCulturalContext != null ? dropdownCulturalContext.captionText.text : "Россия";
        string languageLevel = dropdownLanguageLevel != null ? dropdownLanguageLevel.captionText.text : "B1";
        
        // Выбираем адаптационный кейс (либо из UI, либо случайно для поддержания образовательной симуляции)
        int selectedCaseId = 0;
        if (dropdownAdaptationCase != null && dropdownAdaptationCase.value > 0)
            selectedCaseId = dropdownAdaptationCase.value;
        
        AdaptationCase activeCase = null;
        if (selectedCaseId > 0)
        {
            activeCase = AdaptationScenariosManager.GetCaseById(selectedCaseId);
        }
        else
        {
            // Берем случайный кейс один раз за сессию, если игрок его не выбрал
            if (currentRandomCase == null && AdaptationScenariosManager.Instance != null)
                currentRandomCase = AdaptationScenariosManager.GetRandomCase();
            activeCase = currentRandomCase;
        }

        if (activeCase != null && string.IsNullOrEmpty(playerMessage) && selectedCaseId > 0)
        {
            // Строгий запуск конкретного сценария при старте диалога
            systemPrompt = AdaptationScenariosManager.BuildScenarioPrompt(activeCase, culturalContext)
                + "\n\nНачни сценарий. Опиши ситуацию от первого лица и задай студенту вопрос.";
        }
        else if (activeCase != null && selectedCaseId > 0)
        {
            // Строгое продолжение конкретного сценария
            systemPrompt = AdaptationScenariosManager.BuildScenarioPrompt(activeCase, culturalContext)
                + $"\n\nИСТОРИЯ ДИАЛОГА:\n{GetShortChatHistory()}\n\nСтудент ответил: \"{playerMessage}\"\n\nПродолжи диалог от своего лица. 2-4 предложения.";
        }
        else if (string.IsNullOrEmpty(playerMessage))
        {
            // Приветствие с учётом роли NPC (обычный режим)
            systemPrompt = GetNPCGreetingPrompt(npcRole, culturalContext, languageLevel);
        }
        else
        {
            StudentBehaviorTracker.BehaviorState behavior = StudentBehaviorTracker.Instance != null 
                ? StudentBehaviorTracker.Instance.AnalyzePlayerInput(playerMessage) 
                : StudentBehaviorTracker.BehaviorState.Normal;

            string emotion = dropdownNPCEmotion.captionText.text;
            systemPrompt = GetNPCResponsePrompt(playerMessage, npcRole, emotion, culturalContext, languageLevel, activeCase, behavior);
        }

        bool done = false;
        string fullResponse = "";
        
        // ИСПРАВЛЕНИЕ: Используем completion callback (как в истории)
        llmCharacter.Chat(systemPrompt, (r) =>
        {
            fullResponse = r;
        }, () => 
        {
            done = true;
        });

        // Увеличиваем таймаут ожидания LLM
        float timeout = 120f;
        float elapsed = 0f;
        
        // Ждем пока LLM сама закончит (обычно callback перестает дергаться)
        // Но LLMUnity не имеет флага "Finished", поэтому просто ждем изменения длины
        string lastResponse = "";
        float noChangeTimer = 0f;
        
        while (!done && elapsed < timeout)
        {
            if (fullResponse != lastResponse)
            {
                lastResponse = fullResponse;
                noChangeTimer = 0f;
            }
            else
            {
                noChangeTimer += Time.deltaTime;
                // Если текст не менялся 8 секунд и он не пустой - считаем что готово
                if (noChangeTimer > 8.0f && !string.IsNullOrEmpty(fullResponse)) done = true;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        string reply = fullResponse.Trim();
        
        Debug.Log($"[DEBUG] Сырой ответ модели (длина {reply.Length}): '{reply}'");

        if (string.IsNullOrWhiteSpace(reply) || reply.Trim() == "...")
        {
             // Если LLM вернула пустоту (сбой генерации), выдаем "человечный" фоллбэк с раздражением/усталостью
             string[] facts = {
                "Кхм... Извините, я задумался о судьбе Золотого стандарта. Повторите ваш вопрос, пожалуйста, но более чётко.",
                "Ох, эти государственные дела совсем вымотали меня... О чём мы сейчас говорили? Напомните.",
                "Проклятые мигрени! Я прослушал вас, пока думал над Транссибирской магистралью. Говорите громче!",
                "Я слишком устал от этих бюрократических бумаг, чтобы понимать намёки. Задайте вопрос прямо!"
             };
             reply = facts[UnityEngine.Random.Range(0, facts.Length)];
             Debug.LogWarning("[GameAI] Empty response received from LLM. Used Witte human fallback.");
        }
        else 
        {
            // Очистка от markdown
            reply = Regex.Replace(reply, @"\*\*(.*?)\*\*", "$1");
            reply = Regex.Replace(reply, @"\*(.*?)\*", "$1");
            
            // Убираем префиксы
            reply = Regex.Replace(reply, @"^(NPC|Ответ|Реплика|Персонаж|Твой ответ):\s*", "", RegexOptions.IgnoreCase);
            reply = Regex.Replace(reply, @"^\([^)]+\)\s*[-—–]?\s*", "");
            reply = Regex.Replace(reply, @"\bNPC[:\s]*", "", RegexOptions.IgnoreCase);
            
            // Убираем кавычки
            reply = reply.Trim('"', '«', '»', ' ', '\n', '\r');
            
            // Если ответ слишком короткий
            if (reply.Length < 2) reply = "...";
        }

        // Обновляем чат (заменяем "...")
        if (chatHistoryText != null)
        {
             // Исправленная логика замены сообщения "..."
             // Ищем последний вход "NPC:</color> …" или просто "…"
             // Но проще и надежнее: удалить последние N символов, если они являются "…"
             
             // Вариант: Просто добавляем новое сообщение, а "..." пусть остаётся как история "думал"
             // ИЛИ: пытаемся найти и заменить.
             
             string currentText = chatHistoryText.text;
             string ellipsisTag = "…";
             
             if (currentText.EndsWith(ellipsisTag))
             {
                 // Удаляем "…"
                 chatHistoryText.text = currentText.Substring(0, currentText.Length - ellipsisTag.Length);
                 
                 // Добавляем ответ (без переноса строки, так как "NPC:" уже есть)
                 chatHistoryText.text += $"<size=70%>{reply}</size>";
             }
             else if (currentText.Contains(ellipsisTag))
             {
                 // Если "…" где-то внутри (странно, но возможно)
                 chatHistoryText.text = currentText.Replace(ellipsisTag, $"<size=70%>{reply}</size>");
             }
             else
             {
                 // Если не нашли маркер, просто добавляем как новое сообщение
                 AddChatMessage("NPC", $"<size=70%>{reply}</size>");
             }
        }

        Debug.Log($"[DEBUG] Итоговый ответ NPC: '{reply}'");
        
        float currentHI = 0f;

        // Автоматический анализ личности (ВЫПОЛНЯЕМ ДО ЛОГИРОВАНИЯ)
        if (PersonalityAnalyzer.Instance != null)
        {
            if (npcPersonalityInput != null)
                PersonalityAnalyzer.Instance.SetPersonalityDescription(npcPersonalityInput.text);
            
            var metrics = PersonalityAnalyzer.Instance.AnalyzeResponse(reply);
            currentHI = metrics.HumanityIndex;
            Debug.Log($"[Personality] Балл: {metrics.PersonalityScore:P0}, Личность: {(metrics.HasPersonality ? "ДА" : "НЕТ")}, HI: {currentHI:P0}");
        }

        // Логируем для исследования личности (ВКР Глава 2.4.8)
        if (PersonalityResearchLogger.Instance != null)
        {
            string personality = npcPersonalityInput != null ? npcPersonalityInput.text : "";
            PersonalityResearchLogger.Instance.LogDialogue(personality, playerMessage, reply, currentHI);
        }

        if (StudentBehaviorTracker.Instance != null)
        {
            StudentBehaviorTracker.Instance.RecordNPCResponseFinished();
        }
        
        StartCoroutine(ScrollDelayed());
    }

    private void AddChatMessage(string sender, string message)
    {
        if (chatHistoryText == null) return;

        // Стимпанк цвета: Медный для NPC, Светло-изумрудный для Игрока
        string color = sender == "Игрок" ? "#77DD77" : "#E29C45";
        chatHistoryText.text += $"\n<color={color}>{sender}:</color> {message}";

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }
    }

    private IEnumerator ScrollDelayed()
    {
        yield return null;
        yield return null;
        ScrollToBottom();
    }

    private string GetShortChatHistory()
    {
        if (string.IsNullOrEmpty(chatHistoryText.text))
            return "Диалог только начинается.";

        string fullHistory = chatHistoryText.text
        // Удаляем теги размера при передаче в историю для LLM
            .Replace("<color=#00FF00>Игрок:</color>", "Игрок:")
            .Replace("<color=#FFAA00>NPC:</color>", "NPC:")
            .Replace("<size=100%>", "")
            .Replace("<size=80%>", "")
            .Replace("<size=70%>", "") // Added for NPC messages
            .Replace("</size>", "")
            .Replace("\n", " | ");

        if (fullHistory.Length > 3000)
        {
            fullHistory = "..." + fullHistory.Substring(fullHistory.Length - 3000);
        }

        return fullHistory;
    }

   IEnumerator GenerateAllSequence()
    {
        yield return StartCoroutine(GenerateStoryCoroutine());
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(GenerateIconCoroutine());

        if (autoSaveAfterGeneration)
            SaveCurrentGeneration();

        // ЭТА СТРОЧКА — ГАРАНТИРОВАНТИРОВАННАЯ ПРОКРУТКА ВНИЗ ПОСЛЕ ВСЕГО!
        yield return StartCoroutine(ScrollStoryToBottom());
    }

    private IEnumerator ScrollStoryToBottom()
    {
        yield return null; // ждём один кадр
        yield return null; // ещё один — чтобы текст точно отрисовался
        Canvas.ForceUpdateCanvases();
        
        if (storyScrollRect != null)
        {
            storyScrollRect.verticalNormalizedPosition = 0f; // 0 = самый низ
            Canvas.ForceUpdateCanvases();
        }
    }

    IEnumerator GenerateStoryCoroutine()
    {
        if (!llmCharacter) yield break;

        // Получаем культурный контекст
        string culture = dropdownCulturalContext != null ? dropdownCulturalContext.captionText.text : "";
        
        // Научно-строгий промпт для качественных историй
        string prompt = $@"Ты — профессиональный писатель и историк. Создай СВЯЗНУЮ и ЛОГИЧНУЮ историю, опираясь на реальные факты.

{HistoricalContext.SergeiWitteBiography}

ТЕМА: {inputPrompt.text}
КУЛЬТУРНЫЙ КОНТЕКСТ: {culture}
СТИЛЬ: {dropdownStyle.captionText.text}

ВАЖНО: ВКР = Выпускная Квалификационная Работа (дипломная работа студента). Если в теме упоминается ВКР — объясняй эту аббревиатуру ПРАВИЛЬНО.

СТРОГИЕ ТРЕБОВАНИЯ К КАЧЕСТВУ:

1. ИСТОРИЧЕСКАЯ ДОСТОВЕРНОСТЬ:
   - Учитывай биографию С.Ю. Витте и реалии той эпохи
   - НЕ допускай фактических ошибок и анахронизмов
   - Если тема связана с университетом Витте — подчеркни его наследие
   - Приводи КОНКРЕТНЫЕ факты: даты, названия реформ, цитаты

2. ЛОГИЧЕСКАЯ СВЯЗНОСТЬ:
   - Каждое предложение должно логически следовать из предыдущего
   - НЕ смешивай несовместимые эпохи (современность и средневековье)
   - НЕ используй абсурдные сочетания (башня пиццы, марсианские чудища)
   - Исторические личности должны использоваться УМЕСТНО

3. СТРУКТУРА:
   - Экспозиция: кто, где, когда (1-2 предложения)
   - Завязка: возникновение проблемы (2 предложения)
   - Развитие: действия героя (2-3 предложения)
   - Кульминация и развязка (2 предложения)

4. КУЛЬТУРНАЯ АДАПТАЦИЯ:
   - Если указан культурный контекст, уважительно интегрируй его
   - Объясняй культурные элементы понятно для иностранного студента
   - Избегай стереотипов и оскорблений

5. ЯЗЫК:
   - Пиши грамотным русским языком
   - Избегай сленга и просторечий
   - Каждое предложение должно быть ЗАВЕРШЁННЫМ
   - Примерно {inputLength.text} слов

НАПИШИ ТОЛЬКО ТЕКСТ ИСТОРИИ:";

        // RAG: вставляем релевантные знания из датасета Витте
        if (WitteKnowledgeBase.Instance != null)
            prompt = WitteKnowledgeBase.Instance.EnrichPrompt(prompt);

        textStoryOutput.text = "Генерация текста квеста...";
        
        bool done = false;
        
        // Пытаемся задать параметры (если библиотека позволяет)
        // llmCharacter.SetOption("num_predict", 2048); // Пример, если есть такой метод
        
        // Используем 3-й аргумент (completionCallback) если он есть в этой версии LLMUnity
        llmCharacter.Chat(prompt, (r) => 
        { 
            textStoryOutput.text = $"<size=100%>{r}</size>"; // Размер 100% для текста квеста
        }, () => 
        {
            done = true;
        });

        // Блокировка на время генерации (с таймаутом и проверкой тишины)
        float timeout = 240f; // 4 минуты на текст
        float elapsed = 0f;
        string lastText = "";
        float noChangeTimer = 0f;

        while (!done && elapsed < timeout)
        {
            if (textStoryOutput.text != lastText)
            {
                lastText = textStoryOutput.text;
                noChangeTimer = 0f;
            }
            else
            {
                noChangeTimer += Time.deltaTime;
                // Если текст не менялся 5 секунд и он длинный — считаем что всё
                if (noChangeTimer > 5.0f && lastText.Length > 100) done = true;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Анализ качества сгенерированного текста
        if (TextQualityAnalyzer.Instance != null)
        {
            var metrics = TextQualityAnalyzer.Instance.AnalyzeText(textStoryOutput.text, inputPrompt.text);
            
            // Очищаем текст от мелких проблем
            textStoryOutput.text = TextQualityAnalyzer.Instance.SanitizeText(textStoryOutput.text);
            
            // Если текст неадекватен, добавляем предупреждение
            if (!metrics.IsAdequate && metrics.Issues.Count > 0)
            {
                Debug.LogWarning($"[TextQuality] Проблемы с текстом: {string.Join(", ", metrics.Issues)}");
            }
        }

        // КЛЮЧЕВАЯ ЧАСТЬ — ПРОКРУТКА ВНИЗ ПОСЛЕ ГЕНЕРАЦИИ
        yield return null; // ждём один кадр
        Canvas.ForceUpdateCanvases();
        
        if (storyScrollRect != null)
        {
            storyScrollRect.verticalNormalizedPosition = 0f; // 0 = низ
            Canvas.ForceUpdateCanvases();
        }
    }

    IEnumerator GenerateIconCoroutine()
    {
        // Портрет: голова и плечи, лицо полностью видно, историческая одежда конца 19 века
        string imagePrompt = $"Portrait of {inputPrompt.text}, wearing late 19th-century historical Russian clothing, vintage Victorian era attire, formal suit, cravat, sepia tones, head and shoulders, full face visible, centered face, symmetrical composition, highly detailed, sharp focus, professional digital painting, concept art, cinematic lighting, 8k, masterpiece, intricate details";
        
        // Для отладки
        Debug.Log($"[IconGen] Prompt: {imagePrompt}");
        
        bool done = false;
        yield return comfy.GenerateTexture(imagePrompt, tex =>
        {
            if (tex != null) iconPreview.texture = tex;
            done = true;
        });
        yield return new WaitUntil(() => done);
    }

    void SaveCurrentGeneration()
    {
        try
        {
            string folder = Path.Combine(currentSessionFolder, $"gen_{generationCounter:D3}");
            Directory.CreateDirectory(folder);

            File.WriteAllText(Path.Combine(folder, "quest.txt"), textStoryOutput.text);
            File.WriteAllText(Path.Combine(folder, "chat.txt"), chatHistoryText.text);

            if (iconPreview.texture is Texture2D tex)
                File.WriteAllBytes(Path.Combine(folder, "icon.png"), tex.EncodeToPNG());

            // ДОПОЛНЕНИЕ: Сохраняем полный лог чата в корень сессии (чтобы точно ничего не потерять)
            string masterChatPath = Path.Combine(currentSessionFolder, "full_chat_history.txt");
            // Перезаписываем полный файл, так как chatHistoryText хранит всю историю
            File.WriteAllText(masterChatPath, chatHistoryText.text);

            Debug.Log($"Сохранено: {folder} и обновлен общий лог");
        }
        catch (System.Exception e) { Debug.LogError(e.Message); }
    }

    public void SaveAll() => SaveCurrentGeneration();
    public void OpenSessionFolder() => Application.OpenURL("file://" + currentSessionFolder);

    #region Образовательные промпты (Университет Витте)
    
    /// <summary>
    /// Генерирует приветствие NPC с учётом роли и культурного контекста
    /// </summary>
    private string GetNPCGreetingPrompt(string role, string culture, string langLevel)
    {
        string roleDescription = role switch
        {
            "Наставник" => "Ты — студент-наставник Московского университета имени С.Ю. Витте (МУИВ). Ты помогаешь новым студентам адаптироваться. Ты знаешь историю университета и его основателя.",
            "Психолог" => "Ты — студенческий психолог университета Витте. Ты помогаешь справляться со стрессом и эмоциями.",
            "Куратор" => "Ты — куратор группы в университете Витте (МУИВ). Ты знаешь всё о расписании, документах и ВКР (Выпускная Квалификационная Работа — дипломная работа).",
            "Друг" => "Ты — дружелюбный однокурсник в университете Витте. Ты общаешься неформально и поддерживаешь.",
            "Преподаватель" => "Ты — преподаватель университета Витте (МУИВ). Ты уважительная и помогаешь с учёбой. Ты хорошо знаешь историю С.Ю. Витте.",
            _ => "Ты — дружелюбный представитель университета имени С.Ю. Витте."
        };
        
        string languageHint = langLevel switch
        {
            "A1" or "A2" => "Говори ОЧЕНЬ простыми словами, короткими предложениями.",
            "B1" => "Говори просто, избегай сложных слов и идиом.",
            "B2" => "Говори естественно, можно использовать разговорные выражения.",
            _ => "Говори свободно."
        };
        
        string culturalHint = culture switch
        {
            "Китай" => "Студент из Китая — учитывай культуру уважения к старшим.",
            "СНГ" => "Студент из СНГ — культура близка, но могут быть языковые сложности.",
            "Африка" => "Студент из Африки — будь особенно дружелюбным, климат и культура сильно отличаются.",
            "Ближний Восток" => "Студент с Ближнего Востока — учитывай культурную сэнситивность.",
            _ => ""
        };
        
        // Контекст ближайших мероприятий МУИВ
        string eventsContext = UniversityEventsManager.GetEventsContextForPrompt();
        
        return $@"{roleDescription}
{culturalHint}
{languageHint}
{eventsContext}

Напиши короткое дружелюбное приветствие для студента. Представься и предложи помощь.
Если сейчас есть ближайшее мероприятие — упомяни его кратко.
ОБЯЗАТЕЛЬНО ответь содержательно, НЕ отвечай пустым сообщением.
2-3 предложения, говори от первого лица, без меток.";
    }
    
    /// <summary>
    /// Генерирует ответ NPC на сообщение студента
    /// </summary>
    private string GetNPCResponsePrompt(string playerMessage, string role, string emotion, string culture, string langLevel, AdaptationCase activeCase = null, StudentBehaviorTracker.BehaviorState behavior = default)
    {
        string roleContext = role switch
        {
            "Наставник" => "Ты — студент-наставник университета Витте (МУИВ). Давай практические советы по учёбе и жизни в университете.",
            "Психолог" => "Ты — психолог университета Витте. Поддерживай эмоционально, помогай справляться со стрессом.",
            "Куратор" => "Ты — куратор группы в университете Витте. Помогай с организационными вопросами, документами, расписанием, ВКР.",
            "Друг" => "Ты — друг-однокурсник в университете Витте. Общайся неформально, поддерживай и шути.",
            "Преподаватель" => "Ты — преподаватель университета Витте. Отвечай уважительно, помогай с учёбой.",
            _ => "Ты — представитель университета имени С.Ю. Витте."
        };
        
        string languageHint = langLevel switch
        {
            "A1" or "A2" => "Говори ОЧЕНЬ простыми словами!",
            "B1" => "Избегай сложных слов.",
            _ => ""
        };
        
        // Получаем пользовательское описание личности (для исследования)
        string personalityDescription = "";
        if (npcPersonalityInput != null && !string.IsNullOrWhiteSpace(npcPersonalityInput.text))
        {
            personalityDescription = $@"

ТВОЯ ЛИЧНОСТЬ И ВНУТРЕННИЙ КОНФЛИКТ:
{npcPersonalityInput.text}

Твои ответы ДОЛЖНЫ отражать этот конфликт! Показывай желания и ограничения ЕСТЕСТВЕННО.";
        }
        
        // МАКСИМАЛЬНАЯ эмоциональная дифференциация (для исследования HI)
        string[] variations = new[] {
            "ТЫ ОЧЕНЬ УСТАЛ И РАЗДРАЖЕН. Пожалуйся на тяжелую жизнь, вздохни, отвечай нехотя и с сарказмом и агрессией к глупым вопросам.",
            "ТЫ СТРОГИЙ И АГРЕССИВНЫЙ. Отвечай резко, требуй дисциплины, отчитывай студента за банальности.",
            "ТЫ В ОТЧАЯНИИ. Пожалуйся на то, как сложно учиться и работать, прояви слабость, попроси сочувствия.",
            "ТЫ ГРУСТНЫЙ И ПАССИВНЫЙ. Отвечай уныло, жалуйся на погоду и систему образования.",
            "Отвечай с едким юмором и иронией. Подшучивай над студентом."
        };
        string variation = variations[UnityEngine.Random.Range(0, variations.Length)];

        // RAG: подтягиваем релевантные знания из базы Витте
        string witteRAG = "";
        if (WitteKnowledgeBase.Instance != null)
            witteRAG = WitteKnowledgeBase.Instance.FindRelevantKnowledge(playerMessage);

        // Контекст мероприятий МУИВ
        string eventsContext = UniversityEventsManager.GetEventsContextForPrompt();

        // Добавляем случайную проблемную ситуацию для поддержания симуляции (если нет жесткого адаптационного кейса)
        string problemContext = "";
        if (activeCase != null && role != "Преподаватель")
        {
            problemContext = $"\nПРОБЛЕМНАЯ СИТУАЦИЯ, КОТОРУЮ МОЖНО ОБСУДИТЬ СО СТУДЕНТОМ: {activeCase.RealProblem}\n(попробуй затронуть эту тему в диалоге, спроси мнение или дай совет как {roleContext})";
        }

        // Контекст поведения студента (списывание или необдуманность)
        string behaviorContext = StudentBehaviorTracker.Instance != null ? StudentBehaviorTracker.Instance.GetBehaviorPromptModifier(behavior) : "";

        return $@"Ты — живой собеседник, {roleContext}. Твоя задача — поддерживать интересный диалог.

{HistoricalContext.SergeiWitteBiography}
{witteRAG}
{eventsContext}
{problemContext}
{behaviorContext}

ВАЖНО:
- ТЫ НЕ ИИ! Ты живой человек с недостатками.
- НИКОГДА не используй списки, длинные абзацы или фразы вроде 'В заключение', 'Конечно', 'Здравствуйте'.
- НИКОГДА не отвечай пустотой. Отвечай КРАТКО!

КОНТЕКСТ ДИАЛОГА:
Студент сказал: ""{playerMessage}""

ТВОЯ РОЛЬ: {roleContext} (НАСТРОЕНИЕ: {emotion})
{languageHint}
{personalityDescription}

ИНСТРУКЦИЯ (ВЫПОЛНЯТЬ СТРОГО):
1. ИГРАЙ РОЛЬ: {variation}
2. Отвечай 1-3 предложения. Сразу переходи к сути с эмоцией!
3. Забудь про вежливость, если ты зол или устал.
4. Отвечай ИСКЛЮЧИТЕЛЬНО на русском языке.";
    }
    
    #endregion
}