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
        
        // ИСПРАВЛЕНИЕ 1: Убираем подсчет токенов, используем только проверку предложений
        llmCharacter.Chat(systemPrompt, r =>
        {
            fullResponse = r;
            
            // Завершаем когда есть 2-3 полных предложения
            int sentenceCount = Regex.Matches(r, @"[.!?]").Count;
            if (sentenceCount >= 2)
            {
                // Проверяем, что последнее предложение завершено
                if (r.TrimEnd().EndsWith(".") || r.TrimEnd().EndsWith("!") || r.TrimEnd().EndsWith("?"))
                {
                    done = true;
                }
            }
            
            // ИСПРАВЛЕНИЕ 2: Увеличиваем максимальную длину ответа
            if (r.Length > 500) // ~500 символов для 2-3 предложений
            {
                done = true;
            }
        });

        // ИСПРАВЛЕНИЕ 3: Увеличиваем таймаут
        float timeout = 60f; // было 30
        float elapsed = 0f;
        while (!done && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        string reply = fullResponse.Trim();
        
        Debug.Log($"[DEBUG] Сырой ответ модели (длина {reply.Length}): '{reply}'");

        if (!string.IsNullOrWhiteSpace(reply))
        {
            // Очистка от markdown
            reply = Regex.Replace(reply, @"\*\*(.*?)\*\*", "$1");
            reply = Regex.Replace(reply, @"\*(.*?)\*", "$1");
            
            // Убираем все возможные префиксы ролей
            reply = Regex.Replace(reply, @"^(NPC|Ответ|Реплика|Персонаж|Твой ответ):\s*", "", RegexOptions.IgnoreCase);
            reply = Regex.Replace(reply, @"^\([^)]+\)\s*[-—–]?\s*", "");
            reply = Regex.Replace(reply, @"\bNPC[:\s]*", "", RegexOptions.IgnoreCase);
            
            // Убираем кавычки
            reply = reply.Trim('"', '«', '»', ' ', '\n', '\r');
            
            // ИСПРАВЛЕНИЕ 4: Более мягкое ограничение предложений
            var sentenceMatches = Regex.Matches(reply, @"[^.!?]+[.!?]+");
            if (sentenceMatches.Count > 3)
            {
                string limited = "";
                for (int i = 0; i < 3; i++)
                {
                    limited += sentenceMatches[i].Value;
                }
                reply = limited.Trim();
            }
            
            // Убираем мусор
            if (reply == "..." || reply == "…") reply = "";
        }

        // Fallback если ответ неадекватный
        if (string.IsNullOrWhiteSpace(reply) || reply.Length < 5 || reply.ToLower().StartsWith("npc"))
        {
            string[] fallback = {
                "Ох, милок, расскажи поподробнее...",
                "Ну ты даёшь! А дальше-то что?",
                "Слушаю тебя, странник.",
                "Хм... интересно. Продолжай.",
                "Да ты что! Не может быть!",
                "Ох, батюшки... ну и дела.",
                "Ишь ты какой! Это надо же!",
                "Ай да молодец! Рассказывай дальше."
            };
            reply = fallback[UnityEngine.Random.Range(0, fallback.Length)];
            Debug.LogWarning($"[DEBUG] Неадекватный ответ: '{fullResponse}', используем fallback");
        }

        // Убираем "…" и вставляем настоящий ответ
        if (chatHistoryText != null)
        {
            string text = chatHistoryText.text;
            int index = text.LastIndexOf("<color=#FFAA00>NPC:</color> …");
            if (index >= 0)
                chatHistoryText.text = text.Substring(0, index);
        }

        AddChatMessage("NPC", reply);
        StartCoroutine(ScrollDelayed());
        
        Debug.Log($"[DEBUG] Итоговый ответ NPC: '{reply}'");
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
        llmCharacter.Chat(prompt, r => 
        { 
            textStoryOutput.text = r;
            done = true;
        });

        yield return new WaitUntil(() => done);

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
        string prompt = $"Awesome RPG icon of {inputPrompt.text}, russian folk style, sharp, centered, transparent background";
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
        
        return $@"Студент сказал: ""{playerMessage}""

{roleContext}
Твоё настроение: {emotion}
{languageHint}

Ответь 2-3 предложениями от первого лица, без меток.";
    }
    
    #endregion
}