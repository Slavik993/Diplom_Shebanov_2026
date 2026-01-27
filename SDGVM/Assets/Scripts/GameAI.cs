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
        }

        StartCoroutine(GenerateNPCResponse(""));
        
        // ПРИНУДИТЕЛЬНАЯ установка шрифта диалога (игнорирует Inspector)
        if (chatHistoryText != null)
        {
            chatHistoryText.fontSize = 16; // Маленький шрифт
            chatHistoryText.enableWordWrapping = true;
        }
        
        // Автоматическое исправление Scroll View
        FixScrollViews();
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
                 tmp.fontSize = 18; // Уменьшенный шрифт для читаемости
                 tmp.alignment = TextAlignmentOptions.TopLeft;
             }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    void CreateSessionFolder()
    {
        string sessionName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        currentSessionFolder = Path.Combine(Application.dataPath, saveFolderRoot, sessionName);
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
        
        if (string.IsNullOrEmpty(playerMessage))
        {
            // Приветствие с учётом роли NPC
            systemPrompt = GetNPCGreetingPrompt(npcRole, culturalContext, languageLevel);
        }
        else
        {
            string emotion = dropdownNPCEmotion.captionText.text;
            systemPrompt = GetNPCResponsePrompt(playerMessage, npcRole, emotion, culturalContext, languageLevel);
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

        if (string.IsNullOrWhiteSpace(reply))
        {
             reply = "Извини, я задумался о вечном и забыл, что хотел сказать.";
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
             string currentText = chatHistoryText.text;
             // Ищем наш маркер ожидания. В AddChatMessage мы добавляли "..."
             // Но там был и цвет: <color=#FFAA00>NPC:</color> …
             string ellipsisMarkup = "<color=#FFAA00>NPC:</color> …";
             int index = currentText.LastIndexOf(ellipsisMarkup);
             
             if (index >= 0)
             {
                 // Отрезаем всё после последнего NPC (то есть удаляем "…")
                 // И добавляем нормальный ответ
                 chatHistoryText.text = currentText.Substring(0, index);
                 AddChatMessage("NPC", reply);
             }
             else
             {
                 // Если маркера нет (странно), просто добавляем
                 AddChatMessage("NPC", reply);
             }
        }

        Debug.Log($"[DEBUG] Итоговый ответ NPC: '{reply}'");
        
        // Логируем для исследования личности
        if (PersonalityResearchLogger.Instance != null)
        {
            string personality = npcPersonalityInput != null ? npcPersonalityInput.text : "";
            PersonalityResearchLogger.Instance.LogDialogue(personality, playerMessage, reply);
        }
        
        StartCoroutine(ScrollDelayed());
    }

    private void AddChatMessage(string sender, string message)
    {
        if (chatHistoryText == null) return;

        string color = sender == "Игрок" ? "#00FF00" : "#FFAA00";
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
            .Replace("<color=#00FF00>Игрок:</color>", "Игрок:")
            .Replace("<color=#FFAA00>NPC:</color>", "NPC:")
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

        string prompt = $@"Создай квест на русском языке.
    Тема: {inputPrompt.text}
    Длина: {inputLength.text} слов
    Стиль: {dropdownStyle.captionText.text}
    Тип: {dropdownType.captionText.text}
    Сложность: {dropdownDifficulty.captionText.text}

    Напиши только текст квеста, без пояснений.";

        textStoryOutput.text = "Генерация текста квеста...";
        
        bool done = false;
        
        // Пытаемся задать параметры (если библиотека позволяет)
        // llmCharacter.SetOption("num_predict", 2048); // Пример, если есть такой метод
        
        // Используем 3-й аргумент (completionCallback) если он есть в этой версии LLMUnity
        llmCharacter.Chat(prompt, (r) => 
        { 
            textStoryOutput.text = r;
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
        // Формируем более точный промпт для персонажа
        string role = dropdownNPCRole != null ? dropdownNPCRole.captionText.text : "Character";
        string culture = dropdownCulturalContext != null ? dropdownCulturalContext.captionText.text : "Russian Folk";
        
        // Переводим некоторые русские термины для лучшего понимания моделью (если модель английская)
        // Но если модель понимает русский - оставляем. Обычно модели лучше работают с английскими тегами.
        // Простой маппинг:
        string stylePrompt = "digital painting, rpg character portrait, high quality, sharp focus";
        
        string prompt = $"Portrait of a {role}, {culture} style, {inputPrompt.text}, {stylePrompt}, centered, transparent background";
        
        bool done = false;
        yield return comfy.GenerateTexture(prompt, tex =>
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

            Debug.Log($"Сохранено: {folder}");
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
            "Наставник" => "Ты — студент-наставник университета Витте. Ты помогаешь новым студентам адаптироваться.",
            "Психолог" => "Ты — студенческий психолог университета. Ты помогаешь справляться со стрессом и эмоциями.",
            "Куратор" => "Ты — куратор группы в университете Витте. Ты знаешь всё о расписании и документах.",
            "Друг" => "Ты — дружелюбный однокурсник. Ты общаешься неформально и поддерживаешь.",
            "Преподаватель" => "Ты — преподаватель университета Витте. Ты уважительная и помогаешь с учёбой.",
            _ => "Ты — дружелюбный представитель университета."
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
        
        return $@"{roleDescription}
{culturalHint}
{languageHint}

Напиши короткое дружелюбное приветствие для студента. Представься и предложи помощь.
2-3 предложения, говори от первого лица, без меток.";
    }
    
    /// <summary>
    /// Генерирует ответ NPC на сообщение студента
    /// </summary>
    private string GetNPCResponsePrompt(string playerMessage, string role, string emotion, string culture, string langLevel)
    {
        string roleContext = role switch
        {
            "Наставник" => "Ты — студент-наставник. Давай практические советы по учёбе и жизни в университете.",
            "Психолог" => "Ты — психолог. Поддерживай эмоционально, помогай справляться со стрессом.",
            "Куратор" => "Ты — куратор. Помогай с организационными вопросами, документами, расписанием.",
            "Друг" => "Ты — друг-однокурсник. Общайся неформально, поддерживай и шути.",
            "Преподаватель" => "Ты — преподаватель. Отвечай уважительно, помогай с учёбой.",
            _ => "Ты — представитель университета."
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

ВАЖНО! Твоя личность:
{npcPersonalityInput.text}

Твои ответы должны отражать этот внутренний конфликт, показывай свои желания и ограничения в речи.";
        }
        
        return $@"Студент сказал: ""{playerMessage}""

{roleContext}
Твоё настроение: {emotion}
{languageHint}
{personalityDescription}

Ответь 2-3 предложениями от первого лица, без меток.";
    }
    
    #endregion
}