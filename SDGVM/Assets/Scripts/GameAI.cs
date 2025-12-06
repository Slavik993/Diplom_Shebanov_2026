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

    [Header("==== INPUT LEFT PANEL ====")]
    public TMP_InputField inputPrompt;
    public TMP_InputField inputLength;
    public TMP_Dropdown dropdownStyle;
    public TMP_Dropdown dropdownType;
    public TMP_Dropdown dropdownDifficulty;
    public TMP_InputField inputIconStyle;
    public TMP_InputField inputIconSize;
    public TMP_Dropdown dropdownNPCEmotion;
    public TMP_Dropdown dropdownNPCRelation;

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

        // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: используем прямую инструкцию без ролевых меток
        string systemPrompt;
        
        if (string.IsNullOrEmpty(playerMessage))
        {
            systemPrompt = "Напиши короткое приветствие от дружелюбного персонажа русской сказки. 2-3 предложения. Говори от первого лица.";
        }
        else
        {
            string emotion = dropdownNPCEmotion.captionText.text;
            systemPrompt = $@"Игрок сказал тебе: ""{playerMessage}""

    Ты - персонаж русской сказки. Твоё настроение: {emotion}
    Ответь игроку естественно, от первого лица, 2-3 предложения на русском языке.
    Говори просто и по-человечески, без меток вроде ""NPC:"" или ""Ответ:"".

    Твой ответ:";
        }

        bool done = false;
        string fullResponse = "";
        int tokenCount = 0;
        const int maxTokens = 150; // Ограничение на количество токенов

        llmCharacter.Chat(systemPrompt, r =>
        {
            fullResponse = r;
            tokenCount++;
            
            // Завершаем когда есть 2-3 предложения или достигнут лимит токенов
            int sentenceCount = Regex.Matches(r, @"[.!?]").Count;
            if (sentenceCount >= 2 || tokenCount > maxTokens)
            {
                done = true;
            }
        });

        // Ждём завершения с таймаутом
        float timeout = 30f;
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
            
            // ВАЖНО: убираем все возможные префиксы ролей (включая в начале строки)
            reply = Regex.Replace(reply, @"^(NPC|Ответ|Реплика|Персонаж|Твой ответ):\s*", "", RegexOptions.IgnoreCase);
            reply = Regex.Replace(reply, @"^\([^)]+\)\s*[-–—]?\s*", ""); // убираем (Пираты против Петра Первого) - 
            
            // Убираем повторяющиеся префиксы внутри текста
            reply = Regex.Replace(reply, @"\bNPC[:\s]*", "", RegexOptions.IgnoreCase);
            
            // Убираем кавычки
            reply = reply.Trim('"', '«', '»', ' ', '\n', '\r');
            
            // Ограничиваем 2-3 предложениями
            var sentenceMatches = Regex.Matches(reply, @"[^.!?]+[.!?]+");
            if (sentenceMatches.Count > 0 && sentenceMatches.Count > 3)
            {
                string limited = "";
                for (int i = 0; i < Math.Min(3, sentenceMatches.Count); i++)
                {
                    limited += sentenceMatches[i].Value;
                }
                reply = limited.Trim();
            }
            
            // Убираем мусор типа "...", если это единственное содержимое
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

        textStoryOutput.text = "Генерация текста...";
        bool done = false;
        
        llmCharacter.Chat(prompt, r => 
        { 
            textStoryOutput.text = r;
            // Проверяем что текст завершён
            if (r.Length > 100 && (r.EndsWith(".") || r.EndsWith("!") || r.EndsWith("?")))
            {
                done = true;
            }
        });
        
        // Ждём завершения с таймаутом
        float timeout = 60f;
        float elapsed = 0f;
        while (!done && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f); // Дожидаем последние токены
        
        // Скролл для текста квеста
        if (storyScrollRect != null)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            storyScrollRect.verticalNormalizedPosition = 1f;
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
}