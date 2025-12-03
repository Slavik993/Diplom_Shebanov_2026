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
    public TMP_Text chatHistoryText;           // ← теперь это основной чат
    public TMP_InputField playerInput;         // ← поле ввода игрока

    [Header("==== TEXT OUTPUT CENTER ====")]
    public TMP_Text textStoryOutput;

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

        // Приветствие NPC при старте (по желанию)
        StartCoroutine(GenerateNPCResponse("")); // пустое сообщение = просто приветствие
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

    // ====================== ОТПРАВКА СООБЩЕНИЯ ИГРОКОМ ======================
    public void SendPlayerMessage()
    {
        if (string.IsNullOrWhiteSpace(playerInput.text)) return;

        string playerMessage = playerInput.text.Trim();
        AddChatMessage("Игрок", playerMessage);

        playerInput.text = "";
        playerInput.ActivateInputField();

        StartCoroutine(GenerateNPCResponse(playerMessage));
    }

    // ====================== ОТВЕТ NPC ======================
    IEnumerator GenerateNPCResponse(string playerMessage)
    {
        if (!llmCharacter)
        {
            AddChatMessage("NPC", "Ошибка: нет связи с ИИ");
            yield break;
        }

        AddChatMessage("NPC", "…");
        StartCoroutine(ScrollDelayed());

        string history = GetShortChatHistory();

        string prompt = $@"Ты — NPC в русской народной сказке или фэнтези.
ОТВЕЧАЙ ИСКЛЮЧИТЕЛЬНО ОДНОЙ-ДВУМЯ КОРОТКИМИ ФРАЗАМИ НА РУССКОМ ЯЗЫКЕ.
БЕЗ КАВЫЧЕК. БЕЗ ДЕЙСТВИЙ В СКОБКАХ. БЕЗ ПОЯСНЕНИЙ.

Эмоция: {dropdownNPCEmotion.captionText.text}
Отношение к игроку: {dropdownNPCRelation.captionText.text}

Предыдущий диалог:
{history}

{(string.IsNullOrEmpty(playerMessage) ? "Поприветствуй игрока теплом и по-русски." : $"Игрок сказал: {playerMessage}")}

Твоя реплика сейчас:";

        bool done = false;
        string reply = "";

        llmCharacter.Chat(prompt, r =>
        {
            reply = r.Trim();

            // Жёсткая очистка от мусора
            reply = Regex.Replace(reply, @"[\(\[][^)\]]*[)\]]", "");     // убираем (смеётся)
            reply = Regex.Replace(reply, @"^[""«»'""](.*)[""»'""]$", "$1"); // убираем кавычки
            reply = reply.Split('\n')[0].Trim();                        // только первая строка
            reply = reply.Split('—')[0].Trim();                         // иногда тире
            reply = reply.Split('-')[0].Trim();

            done = true;
        });

        yield return new WaitUntil(() => done);

        if (string.IsNullOrWhiteSpace(reply) || reply == "…")
            reply = "Хм...";

        // Заменяем "…" на настоящий ответ
        if (chatHistoryText != null)
        {
            chatHistoryText.text = chatHistoryText.text.TrimEnd();
            int idx = chatHistoryText.text.LastIndexOf("\n<color=#FFAA00>NPC:</color> …");
            if (idx >= 0)
                chatHistoryText.text = chatHistoryText.text.Remove(idx);
        }

        AddChatMessage("NPC", reply);
        StartCoroutine(ScrollDelayed());
    }

    // ====================== ДОБАВЛЕНИЕ В ЧАТ ======================
    private void AddChatMessage(string sender, string message)
    {
        if (chatHistoryText == null) return;

        string color = sender == "Игрок" ? "#00FF00" : "#FFAA00";
        chatHistoryText.text += $"\n<color={color}>{sender}:</color> {message}";
    }

    // ====================== СКРОЛЛ ======================
    private IEnumerator ScrollDelayed()
    {
        yield return null;
        yield return null;
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // ====================== ИСТОРИЯ ======================
    private string GetShortChatHistory()
    {
        if (chatHistoryText == null || string.IsNullOrEmpty(chatHistoryText.text))
            return "Диалог только начинается.";

        string[] lines = chatHistoryText.text.Split('\n');
        int start = Mathf.Max(0, lines.Length - 12);
        string result = "";
        for (int i = start; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Contains("Игрок:") || line.Contains("NPC:"))
                result += line + "\n";
        }
        return string.IsNullOrEmpty(result.Trim()) ? "Диалог только начинается." : result.Trim();
    }

    // ====================== ОСТАЛЬНЫЕ ГЕНЕРАЦИИ ======================
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

        string prompt = $@"Ты — гениальный русскоязычный геймдизайнер. 
ОТВЕЧАЙ ТОЛЬКО НА РУССКОМ ЯЗЫКЕ, без английских слов.
Создай квест на тему: {inputPrompt.text}
Длина: {inputLength.text} слов
Стиль: {dropdownStyle.captionText.text}
Тип: {dropdownType.captionText.text}
Сложность: {dropdownDifficulty.captionText.text}
Выведи только текст квеста, без кавычек и пояснений.";

        textStoryOutput.text = "Генерация текста...";
        bool done = false;
        llmCharacter.Chat(prompt, r => { textStoryOutput.text = r; done = true; });
        yield return new WaitUntil(() => done);
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

    // ====================== СОХРАНЕНИЕ ======================
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