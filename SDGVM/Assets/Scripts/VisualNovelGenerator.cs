using UnityEngine;
using LLMUnity;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Генератор визуальных новелл на основе AI (LLMUnity).
/// Создаёт полную VisualNovelScene из AdaptationCase.
/// Если LLM недоступна — конвертирует существующий DialogueTree.
/// </summary>
public class VisualNovelGenerator : MonoBehaviour
{
    public static VisualNovelGenerator Instance { get; private set; }

    [Header("LLM")]
    public LLMCharacter llmCharacter;

    [Header("Visual AI (ComfyUI)")]
    [Tooltip("Если не назначен, попытается найти объект на сцене")]
    public ComfyUIManager comfyUI;

    [Header("Settings")]
    [Tooltip("Таймаут генерации текста в секундах")]
    public float generationTimeout = 120f;
    [Tooltip("Генерировать картинки после создания текста?")]
    public bool generateVisuals = true;

    /// <summary>
    /// Событие завершения генерации
    /// </summary>
    public event Action<VisualNovelScene> OnGenerationComplete;

    private bool isGenerating = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// Генерирует визуальную новеллу для указанного кейса.
    /// Сначала пробует AI-генерацию, при неудаче — fallback на DialogueTree.
    /// </summary>
    public void Generate(int caseId, Action<VisualNovelScene> callback)
    {
        if (isGenerating)
        {
            Debug.LogWarning("[VNGenerator] Генерация уже в процессе!");
            return;
        }
        StartCoroutine(GenerateCoroutine(caseId, callback));
    }

    private IEnumerator GenerateCoroutine(int caseId, Action<VisualNovelScene> callback)
    {
        isGenerating = true;
        VisualNovelScene result = null;

        // Попробовать AI генерацию
        if (llmCharacter != null)
        {
            Debug.Log($"[VNGenerator] Начинаю AI-генерацию для кейса {caseId}...");
            yield return StartCoroutine(GenerateWithAI(caseId, (scene) => { result = scene; }));
        }

        // Fallback: конвертация существующего DialogueTree
        if (result == null)
        {
            Debug.Log($"[VNGenerator] AI-генерация не удалась. Пробую fallback из DialogueTree...");
            result = ConvertFromDialogueTree(caseId);
        }

        // Если и fallback не сработал — создаём минимальную новеллу
        if (result == null)
        {
            Debug.LogWarning($"[VNGenerator] Нет данных для кейса {caseId}. Создаю минимальную новеллу.");
            result = CreateMinimalNovel(caseId);
        }

        result.CountCorrectChoices();

        // ═══════════════════════════════════════════════════
        // ГЕНЕРАЦИЯ ВИЗУАЛА ЧЕРЕЗ ComfyUI
        // ═══════════════════════════════════════════════════
        if (generateVisuals)
        {
            if (comfyUI == null) comfyUI = FindObjectOfType<ComfyUIManager>();
            
            if (comfyUI != null)
            {
                Debug.Log($"[VNGenerator] Запускаю генерацию визуала ({result.Characters.Count} персонажей, фоны...)");
                yield return StartCoroutine(GenerateVisualsForScene(result, caseId));
            }
            else
            {
                Debug.LogWarning("[VNGenerator] ComfyUIManager не найден на сцене! Пропуск генерации картинок.");
            }
        }

        // Сохраняем готовый сценарий на диск (txt)
        result.SaveToDisk();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        isGenerating = false;

        callback?.Invoke(result);
        OnGenerationComplete?.Invoke(result);
    }

    // ═══════════════════════════════════════════════════
    // AI ГЕНЕРАЦИЯ
    // ═══════════════════════════════════════════════════

    private IEnumerator GenerateWithAI(int caseId, Action<VisualNovelScene> callback)
    {
        AdaptationCase adaptCase = AdaptationScenariosManager.GetCaseById(caseId);
        if (adaptCase == null)
        {
            Debug.LogWarning($"[VNGenerator] Кейс {caseId} не найден в AdaptationScenariosManager");
            callback?.Invoke(null);
            yield break;
        }

        string prompt = BuildGenerationPrompt(adaptCase);

        bool done = false;
        string fullResponse = "";

        llmCharacter.prompt = "Ты — генератор визуальных новелл для образовательной игры. Твоя задача — создать интерактивный сценарий в формате JSON.";

        llmCharacter.Chat(prompt, (r) =>
        {
            fullResponse = r;
        }, () =>
        {
            done = true;
        });

        float elapsed = 0f;
        string lastResponse = "";
        float noChangeTimer = 0f;

        while (!done && elapsed < generationTimeout)
        {
            if (fullResponse != lastResponse)
            {
                lastResponse = fullResponse;
                noChangeTimer = 0f;
            }
            else
            {
                noChangeTimer += Time.deltaTime;
                if (noChangeTimer > 10.0f && !string.IsNullOrEmpty(fullResponse))
                    done = true;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (string.IsNullOrWhiteSpace(fullResponse))
        {
            Debug.LogWarning("[VNGenerator] LLM не вернула ответ.");
            callback?.Invoke(null);
            yield break;
        }

        Debug.Log($"[VNGenerator] Получен ответ LLM (длина {fullResponse.Length}). Парсинг...");

        VisualNovelScene scene = ParseAIResponse(fullResponse, adaptCase);
        
        // ВАЖНО: Мы не вызываем callback напрямую здесь. Возвращаем scene в корутину.
        if (scene != null)
        {
            callback?.Invoke(scene);
        }
    }

    private string BuildGenerationPrompt(AdaptationCase adaptCase)
    {
        return $@"Создай сценарий визуальной новеллы для образовательной игры МУИВ.

КЕЙС: {adaptCase.ShortTitle}
СИТУАЦИЯ: {adaptCase.GameScenario}
РОЛЬ NPC: {adaptCase.NPCRole}
ЭМОЦИЯ NPC: {adaptCase.Emotion}
УЧЕБНАЯ ЦЕЛЬ: {adaptCase.LearningGoal}

Создай сценарий в формате JSON. Требования:
- 8-15 страниц диалога
- 3-5 страниц с выбором ответа (по 2-3 варианта)
- Остальные страницы — просто текст без выбора
- У каждого выбора укажи isCorrect: true/false
- Персонажи: укажи id, displayName и позицию (left/center/right)
- Фон сцены: classroom, dormitory, office, hallway, cafeteria, medical, outdoor, stage

ФОРМАТ JSON:
{{
  ""title"": ""Название"",
  ""characters"": [
    {{""id"": ""npc1"", ""displayName"": ""Имя"", ""description"": ""Краткое ВИЗУАЛЬНОЕ описание внешности на английском (например: young female student, short brown hair, wearing a white shirt and blue tie)""}}
  ],
  ""pages"": [
    {{
      ""speakerName"": ""Имя"",
      ""text"": ""Текст диалога"",
      ""background"": ""classroom"",
      ""characters"": [{{""id"": ""npc1"", ""position"": ""center"", ""highlighted"": true}}],
      ""choices"": [
        {{""text"": ""Вариант 1"", ""nextPage"": 1, ""isCorrect"": true, ""feedback"": ""Правильно!""}}
      ]
    }}
  ]
}}

ВАЖНО: Отвечай ТОЛЬКО JSON, без пояснений. Текст диалога — на русском языке.";
    }

    private VisualNovelScene ParseAIResponse(string response, AdaptationCase adaptCase)
    {
        try
        {
            // Извлекаем JSON из ответа
            string json = ExtractJson(response);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[VNGenerator] Не удалось извлечь JSON из ответа.");
                return null;
            }

            // Парсинг JSON вручную (без сторонних библиотек)
            return ParseJsonToScene(json, adaptCase.Id);
        }
        catch (Exception e)
        {
            Debug.LogError($"[VNGenerator] Ошибка парсинга: {e.Message}");
            return null;
        }
    }

    private string ExtractJson(string text)
    {
        // Ищем JSON блок в ответе
        int start = text.IndexOf('{');
        if (start < 0) return null;

        int depth = 0;
        int end = -1;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') depth--;
            if (depth == 0)
            {
                end = i;
                break;
            }
        }

        return end > start ? text.Substring(start, end - start + 1) : null;
    }

    /// <summary>
    /// Простой парсер JSON → VisualNovelScene (без зависимостей)
    /// Использует regex для извлечения полей из JSON
    /// </summary>
    private VisualNovelScene ParseJsonToScene(string json, int caseId)
    {
        var scene = new VisualNovelScene();
        scene.CaseId = caseId;

        // Извлекаем title
        scene.Title = ExtractJsonString(json, "title") ?? $"Кейс {caseId}";

        // Извлекаем персонажей
        var charsBlock = ExtractJsonArray(json, "characters");
        if (charsBlock != null)
        {
            foreach (var charJson in SplitJsonObjects(charsBlock))
            {
                var ch = new VNCharacter();
                ch.Id = ExtractJsonString(charJson, "id") ?? "npc";
                ch.DisplayName = ExtractJsonString(charJson, "displayName") ?? "NPC";
                ch.SpriteKey = ch.Id;
                ch.Description = ExtractJsonString(charJson, "description") ?? "";
                scene.Characters.Add(ch);
            }
        }

        // Если нет персонажей — добавляем дефолтного
        if (scene.Characters.Count == 0)
        {
            scene.Characters.Add(new VNCharacter("npc", "NPC", "npc", "Персонаж"));
        }

        // Извлекаем страницы
        var pagesBlock = ExtractJsonArray(json, "pages");
        if (pagesBlock != null)
        {
            int pageIdx = 0;
            foreach (var pageJson in SplitJsonObjects(pagesBlock))
            {
                var page = new VNPage();
                page.PageIndex = pageIdx;
                page.SpeakerName = ExtractJsonString(pageJson, "speakerName") ?? "";
                page.DialogueText = ExtractJsonString(pageJson, "text") ?? "";
                page.BackgroundKey = ExtractJsonString(pageJson, "background") ?? "classroom";

                // Персонажи на странице
                var pageCharsBlock = ExtractJsonArray(pageJson, "characters");
                if (pageCharsBlock != null)
                {
                    foreach (var slotJson in SplitJsonObjects(pageCharsBlock))
                    {
                        var slot = new VNCharacterSlot();
                        slot.CharacterId = ExtractJsonString(slotJson, "id") ?? "npc";
                        string posStr = ExtractJsonString(slotJson, "position") ?? "center";
                        slot.Position = ParsePosition(posStr);
                        slot.IsHighlighted = ExtractJsonBool(slotJson, "highlighted");
                        page.Characters.Add(slot);
                    }
                }

                // Варианты выбора
                var choicesBlock = ExtractJsonArray(pageJson, "choices");
                if (choicesBlock != null)
                {
                    foreach (var choiceJson in SplitJsonObjects(choicesBlock))
                    {
                        var choice = new VNChoice();
                        choice.Text = ExtractJsonString(choiceJson, "text") ?? "...";
                        choice.NextPageIndex = ExtractJsonInt(choiceJson, "nextPage", pageIdx + 1);
                        choice.IsCorrect = ExtractJsonBool(choiceJson, "isCorrect");
                        choice.Feedback = ExtractJsonString(choiceJson, "feedback") ?? "";
                        page.Choices.Add(choice);
                    }
                }

                // Если нет выбора — устанавливаем следующую страницу
                if (!page.HasChoices)
                {
                    page.DefaultNextPage = pageIdx + 1;
                }

                scene.Pages.Add(page);
                pageIdx++;
            }
        }

        // Последняя страница — финал
        if (scene.Pages.Count > 0)
        {
            var lastPage = scene.Pages[scene.Pages.Count - 1];
            lastPage.IsEnding = true;
            lastPage.DefaultNextPage = -1;
        }

        scene.DefaultBackgroundKey = "classroom";
        return scene;
    }


    // ═══════════════════════════════════════════════════
    // COMFYUI КОНВЕЙЕР (Фоны и Персонажи)
    // ═══════════════════════════════════════════════════

    private IEnumerator GenerateVisualsForScene(VisualNovelScene scene, int caseId)
    {
        var builder = FindObjectOfType<VisualNovelSceneBuilder>();
        if (builder != null && builder.loadingPanel != null)
        {
            builder.ShowPanel(builder.loadingPanel);
            UpdateLoadingUI(builder, "Анализ сцен для генерации...", 0f);
        }

        string saveDir = VisualNovelScene.GetSaveDirectory();
        
        // 1. СОБИРАЕМ УНИКАЛЬНЫЕ ФОНЫ
        HashSet<string> uniqueBgs = new HashSet<string>();
        uniqueBgs.Add(scene.DefaultBackgroundKey);
        foreach (var page in scene.Pages)
        {
            if (!string.IsNullOrEmpty(page.BackgroundKey))
                uniqueBgs.Add(page.BackgroundKey);
        }

        int totalTasks = uniqueBgs.Count + scene.Characters.Count;
        int completedTasks = 0;

        // 2. ГЕНЕРИРУЕМ ФОНЫ
        foreach (string bgKey in uniqueBgs)
        {
            UpdateLoadingUI(builder, $"Генерация фона: {bgKey}...", (float)completedTasks / totalTasks);
            
            // Если фон — это уже имя файла (мало ли), пропускаем
            if (bgKey.EndsWith(".png")) continue;

            string fileName = $"bg_case{caseId}_{bgKey}.png";
            string filePath = Path.Combine(saveDir, fileName);
            if (File.Exists(filePath))
            {
                Debug.Log($"[VNGenerator] Фон {fileName} уже существует на диске. Пропуск.");
                
                // Обновляем ключ на относительное имя файла
                foreach (var page in scene.Pages)
                {
                    if (page.BackgroundKey == bgKey) page.BackgroundKey = fileName;
                }
                if (scene.DefaultBackgroundKey == bgKey) scene.DefaultBackgroundKey = fileName;
                
                completedTasks++;
                continue;
            }

            string contextDesc = "university " + bgKey.Replace("_", " ");
            if (bgKey == "stage") contextDesc = "university auditorium stage";
            else if (bgKey == "outdoor") contextDesc = "university campus exterior exterior";
            
            string prompt = $"Masterpiece, high quality, anime style background, {contextDesc}, visual novel scenery, empty room, no people, no text, highly detailed architecture, beautiful lighting, beautiful scenery, 16:9 4k resolution";
            Texture2D loadedTex = null;
            
            // Ждём пока ComfyUIManager сгенерирует картинку
            yield return StartCoroutine(comfyUI.GenerateTexture(prompt, (tex) => { loadedTex = tex; }));

            if (loadedTex != null)
            {
                SaveTextureToDisk(loadedTex, filePath);
                // Обновляем ключ в сценах, чтобы использовать имя файла (а не переносить абсолютный путь между компьютерами)
                foreach (var page in scene.Pages)
                {
                    if (page.BackgroundKey == bgKey) page.BackgroundKey = fileName;
                }
                if (scene.DefaultBackgroundKey == bgKey) scene.DefaultBackgroundKey = fileName;
            }
            completedTasks++;
        }

        // 3. ГЕНЕРИРУЕМ ПЕРСОНАЖЕЙ
        foreach (var character in scene.Characters)
        {
            UpdateLoadingUI(builder, $"Генерация персонажа: {character.DisplayName}...", (float)completedTasks / totalTasks);

            string charKey = character.Id;
            string fileName = $"char_case{caseId}_{charKey}.png";
            string filePath = Path.Combine(saveDir, fileName);
            
            if (File.Exists(filePath))
            {
                Debug.Log($"[VNGenerator] Спрайт {fileName} уже существует. Пропуск.");
                character.SpriteKey = fileName;
                completedTasks++;
                continue;
            }

            // Промт для персонажа (прозрачный, портрет)
            string cleanDesc = string.IsNullOrWhiteSpace(character.Description) ? "anime character" : character.Description.Replace("\n", " ").Replace("\r", "");
            string prompt = $"Masterpiece, high quality, anime style portrait of {character.DisplayName}, {cleanDesc}, standing, looking at viewer, solid white background, character design";
            Texture2D loadedTex = null;
            
            yield return StartCoroutine(comfyUI.GenerateTexture(prompt, (tex) => { loadedTex = tex; }));

            if (loadedTex != null)
            {
                SaveTextureToDisk(loadedTex, filePath);
                character.SpriteKey = fileName; // Обновляем SpriteKey на имя файла
            }
            completedTasks++;
        }

        UpdateLoadingUI(builder, "Сохранение сцены...", 1f);
        yield return new WaitForSeconds(0.5f);
    }

    private void UpdateLoadingUI(VisualNovelSceneBuilder builder, string text, float progress)
    {
        if (builder == null) return;
        if (builder.loadingText != null) builder.loadingText.text = text;
        if (builder.loadingProgress != null) builder.loadingProgress.value = progress;
    }

    private void SaveTextureToDisk(Texture2D tex, string path)
    {
        try
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[VNGenerator] Картинка сохранена: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VNGenerator] Ошибка сохранения картинки: {e.Message}");
        }
    }

    // ═══════════════════════════════════════════════════
    // JSON УТИЛИТЫ (без сторонних библиотек)
    // ═══════════════════════════════════════════════════

    private string ExtractJsonString(string json, string key)
    {
        var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        return match.Success ? match.Groups[1].Value.Replace("\\n", "\n").Replace("\\\"", "\"") : null;
    }

    private int ExtractJsonInt(string json, string key, int defaultVal = 0)
    {
        var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(-?\\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out int val) ? val : defaultVal;
    }

    private bool ExtractJsonBool(string json, string key)
    {
        var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
        return match.Success && match.Groups[1].Value.ToLower() == "true";
    }

    private string ExtractJsonArray(string json, string key)
    {
        var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\\[");
        if (!match.Success) return null;

        int start = match.Index + match.Length - 1;
        int depth = 0;
        int end = -1;
        for (int i = start; i < json.Length; i++)
        {
            if (json[i] == '[') depth++;
            else if (json[i] == ']') depth--;
            if (depth == 0) { end = i; break; }
        }
        return end > start ? json.Substring(start + 1, end - start - 1) : null;
    }

    private List<string> SplitJsonObjects(string arrayContent)
    {
        var objects = new List<string>();
        int depth = 0;
        int start = -1;

        for (int i = 0; i < arrayContent.Length; i++)
        {
            if (arrayContent[i] == '{')
            {
                if (depth == 0) start = i;
                depth++;
            }
            else if (arrayContent[i] == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    objects.Add(arrayContent.Substring(start, i - start + 1));
                    start = -1;
                }
            }
        }
        return objects;
    }

    private VNCharacterPosition ParsePosition(string pos)
    {
        switch (pos.ToLower())
        {
            case "left": return VNCharacterPosition.Left;
            case "right": return VNCharacterPosition.Right;
            default: return VNCharacterPosition.Center;
        }
    }

    // ═══════════════════════════════════════════════════
    // FALLBACK: КОНВЕРТАЦИЯ ИЗ СУЩЕСТВУЮЩЕГО DialogueTree
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Конвертирует существующий DialogueTree в VisualNovelScene
    /// </summary>
    public static VisualNovelScene ConvertFromDialogueTree(int caseId)
    {
        DialogueTree tree = DialogueTrees.GetTree(caseId);
        if (tree == null) return null;

        AdaptationCase adaptCase = AdaptationScenariosManager.GetCaseById(caseId);

        var scene = new VisualNovelScene();
        scene.CaseId = caseId;
        scene.Title = adaptCase?.ShortTitle ?? $"Кейс {caseId}";
        scene.Description = adaptCase?.GameScenario ?? "";
        scene.DefaultBackgroundKey = GuessBackground(adaptCase);

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        var nodeToPageIndex = new Dictionary<string, int>();
        var orderedNodes = new List<DialogueNode>();

        // BFS от стартового узла
        string startId = tree.StartNodeId;
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            string nodeId = queue.Dequeue();
            if (visited.Contains(nodeId)) continue;
            visited.Add(nodeId);

            DialogueNode node = tree.GetNode(nodeId);
            if (node == null) continue;

            nodeToPageIndex[nodeId] = orderedNodes.Count;
            orderedNodes.Add(node);

            if (node.Choices != null)
            {
                foreach (var choice in node.Choices)
                {
                    if (!visited.Contains(choice.NextNodeId))
                        queue.Enqueue(choice.NextNodeId);
                }
            }
        }

        // Собираем уникальных персонажей
        var uniqueChars = new Dictionary<string, VNCharacter>();

        // Создаём страницы
        for (int i = 0; i < orderedNodes.Count; i++)
        {
            var node = orderedNodes[i];
            var page = new VNPage();
            page.PageIndex = i;
            
            // Определяем фон на основе ID узла
            string bg = scene.DefaultBackgroundKey;
            string nodeLower = node.NodeId.ToLower();

            // Жестко форсируем фон для начала 31 кейса, так как он определяется как stage,
            // но на самом деле они готовятся в классе, а на сцену выходят только в узле stage_
            if (caseId == 31)
            {
                if (nodeLower.StartsWith("stage")) bg = "stage";
                else bg = "classroom";
            }
            else
            {
                // Для остальных кейсов пытаемся угадать по имени узла
                if (nodeLower.Contains("stage") || nodeLower.Contains("presentation")) bg = "stage";
                else if (nodeLower.Contains("hallway") || nodeLower.Contains("corridor")) bg = "hallway";
                else if (nodeLower.Contains("office") || nodeLower.Contains("curator")) bg = "office";
                else if (nodeLower.Contains("dorm") || nodeLower.Contains("room")) bg = "dormitory";
                else if (nodeLower.Contains("cafe") || nodeLower.Contains("food")) bg = "cafeteria";
            }
            
            page.BackgroundKey = bg;
            page.IsEnding = node.IsEnding;
            page.DialogueText = node.Text;

            // Обрабатываем говорящего из поля Emotion
            string emotionRaw = node.Emotion ?? "";
            if (string.IsNullOrWhiteSpace(emotionRaw)) emotionRaw = "Нарратор";
            
            string charId = "";
            string dispName = "";
            string desc = "";
            
            if (emotionRaw.Contains(","))
            {
                var parts = emotionRaw.Split(new[] { ',' }, 2);
                dispName = parts[0].Trim();
                desc = parts[1].Trim();
                charId = dispName.ToLower().Replace(" ", "_");
            }
            else
            {
                dispName = emotionRaw.Trim();
                charId = dispName.ToLower().Replace(" ", "_");
                desc = "anime character";
            }
            
            page.SpeakerName = dispName;
            
            // Если это не нарратор, добавляем персонажа
            if (charId != "нарратор" && charId != "narrator")
            {
                if (!uniqueChars.ContainsKey(charId))
                {
                    uniqueChars[charId] = new VNCharacter(charId, dispName, charId, desc);
                }
                page.Characters.Add(new VNCharacterSlot(charId, VNCharacterPosition.Center, true));
            }

            if (node.Choices != null && node.Choices.Count > 0)
            {
                foreach (var choice in node.Choices)
                {
                    int nextIdx = nodeToPageIndex.ContainsKey(choice.NextNodeId)
                        ? nodeToPageIndex[choice.NextNodeId]
                        : -1;

                    page.Choices.Add(new VNChoice(
                        choice.Text,
                        nextIdx,
                        choice.HumanityBonus > 0.2f, // Высокий бонус = правильный ответ
                        ""
                    ));
                }
            }
            else if (!node.IsEnding)
            {
                page.DefaultNextPage = i + 1 < orderedNodes.Count ? i + 1 : -1;
            }

            scene.Pages.Add(page);
        }

        scene.Characters.AddRange(uniqueChars.Values);
        scene.CountCorrectChoices();
        return scene;
    }

    // ═══════════════════════════════════════════════════
    // МИНИМАЛЬНАЯ НОВЕЛЛА (последний fallback)
    // ═══════════════════════════════════════════════════

    private VisualNovelScene CreateMinimalNovel(int caseId)
    {
        AdaptationCase adaptCase = AdaptationScenariosManager.GetCaseById(caseId);

        var scene = new VisualNovelScene();
        scene.CaseId = caseId;
        scene.Title = adaptCase?.ShortTitle ?? $"Кейс {caseId}";
        scene.DefaultBackgroundKey = "classroom";

        scene.Characters.Add(new VNCharacter("narrator", "Нарратор", "narrator", "Ведущий"));

        // Титульная страница
        var titlePage = new VNPage();
        titlePage.PageIndex = 0;
        titlePage.SpeakerName = "";
        titlePage.DialogueText = adaptCase?.GameScenario ?? "Добро пожаловать в визуальную новеллу.";
        titlePage.BackgroundKey = "classroom";
        titlePage.DefaultNextPage = 1;
        scene.Pages.Add(titlePage);

        // Финальная страница
        var endPage = new VNPage();
        endPage.PageIndex = 1;
        endPage.SpeakerName = "";
        endPage.DialogueText = "Сценарий завершён. Спасибо за прохождение!";
        endPage.BackgroundKey = "classroom";
        endPage.IsEnding = true;
        scene.Pages.Add(endPage);

        return scene;
    }

    // ═══════════════════════════════════════════════════
    // УТИЛИТЫ
    // ═══════════════════════════════════════════════════

    private static string GuessBackground(AdaptationCase adaptCase)
    {
        if (adaptCase == null) return "classroom";

        string scenario = (adaptCase.GameScenario + " " + adaptCase.NPCRole).ToLower();

        if (scenario.Contains("общежит") || scenario.Contains("комнат"))
            return "dormitory";
        if (scenario.Contains("деканат") || scenario.Contains("кабинет") || scenario.Contains("кафедр"))
            return "office";
        if (scenario.Contains("столов") || scenario.Contains("еда") || scenario.Contains("кафе"))
            return "cafeteria";
        if (scenario.Contains("врач") || scenario.Contains("полик") || scenario.Contains("донор") || scenario.Contains("психолог"))
            return "medical";
        if (scenario.Contains("актов") || scenario.Contains("сцен") || scenario.Contains("конференц") || scenario.Contains("хакатон"))
            return "stage";
        if (scenario.Contains("коридор") || scenario.Contains("лестниц"))
            return "hallway";
        if (scenario.Contains("ярмарк") || scenario.Contains("парк"))
            return "outdoor";

        return "classroom";
    }

    private static string ExtractNpcName(string npcRole)
    {
        if (string.IsNullOrEmpty(npcRole)) return "NPC";

        // Попробуем извлечь первое значимое слово (обычно роль: "Куратор", "Методист" и т.д.)
        string[] parts = npcRole.Split(new[] { ',', '/', '(' }, StringSplitOptions.RemoveEmptyEntries);
        string name = parts[0].Trim();

        // Если слишком длинное — берём первые 2 слова
        string[] words = name.Split(' ');
        if (words.Length > 3)
            name = words[0] + " " + words[1];

        return name;
    }
}
