using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Рантайм-контроллер визуальной новеллы.
/// Играет сгенерированную VisualNovelScene: форма студента → страницы VN → результаты.
/// </summary>
public class VisualNovelPlayer : MonoBehaviour
{
    [Header("References")]
    public VisualNovelSceneBuilder sceneBuilder;
    public VisualNovelGenerator generator;

    [Header("Settings")]
    public int caseId = 31;
    public bool forceRegenerate = false;

    // Текущее состояние
    private VisualNovelScene currentScene;
    private StudentInfo studentInfo;
    private VNPlayResult playResult;
    private int currentPageIndex = 0;
    private bool isPlaying = false;

    // Кэш фонов (загруженные текстуры)
    private Dictionary<string, Texture2D> backgroundCache = new Dictionary<string, Texture2D>();
    private Dictionary<string, Texture2D> characterCache = new Dictionary<string, Texture2D>();

    // Фоны по умолчанию (цвета для каждого типа сцены)
    private static readonly Dictionary<string, Color> BG_COLORS = new Dictionary<string, Color>
    {
        { "classroom", new Color(0.25f, 0.22f, 0.20f, 1f) },
        { "dormitory", new Color(0.20f, 0.18f, 0.22f, 1f) },
        { "office",    new Color(0.22f, 0.20f, 0.18f, 1f) },
        { "hallway",   new Color(0.28f, 0.25f, 0.22f, 1f) },
        { "cafeteria", new Color(0.24f, 0.22f, 0.18f, 1f) },
        { "medical",   new Color(0.22f, 0.25f, 0.25f, 1f) },
        { "outdoor",   new Color(0.18f, 0.22f, 0.20f, 1f) },
        { "stage",     new Color(0.15f, 0.15f, 0.20f, 1f) },
    };

    void Start()
    {
        if (sceneBuilder == null) sceneBuilder = GetComponent<VisualNovelSceneBuilder>();
        if (generator == null) generator = GetComponent<VisualNovelGenerator>();

        // Строим сцену
        sceneBuilder.BuildScene();

        // Подписываемся на события UI
        if (sceneBuilder.btnStart != null)
            sceneBuilder.btnStart.onClick.AddListener(OnStartClicked);
        if (sceneBuilder.btnNext != null)
            sceneBuilder.btnNext.onClick.AddListener(OnNextClicked);
        if (sceneBuilder.btnRestart != null)
            sceneBuilder.btnRestart.onClick.AddListener(OnRestartClicked);

        // Загружаем фоны и спрайты из Resources
        LoadAssets();

        Debug.Log("[VNPlayer] Инициализирован. Ожидание ввода студента.");
    }

    // ═══════════════════════════════════════
    // ЗАГРУЗКА АССЕТОВ
    // ═══════════════════════════════════════

    private void LoadAssets()
    {
        // Пытаемся загрузить фоны из Resources/VN/Backgrounds/
        string[] bgKeys = { "classroom", "dormitory", "office", "hallway", "cafeteria", "medical", "outdoor", "stage" };
        foreach (var key in bgKeys)
        {
            var tex = Resources.Load<Texture2D>($"VN/Backgrounds/{key}");
            if (tex != null)
            {
                backgroundCache[key] = tex;
                Debug.Log($"[VNPlayer] Загружен фон: {key}");
            }
        }

        // Спрайты персонажей из Resources/VN/Characters/
        var charTextures = Resources.LoadAll<Texture2D>("VN/Characters");
        foreach (var tex in charTextures)
        {
            characterCache[tex.name.ToLower()] = tex;
            Debug.Log($"[VNPlayer] Загружен спрайт: {tex.name}");
        }
    }

    // ═══════════════════════════════════════
    // КНОПКИ
    // ═══════════════════════════════════════

    private void OnStartClicked()
    {
        // Собираем данные студента
        studentInfo = new StudentInfo();
        studentInfo.Surname = sceneBuilder.inputSurname?.text ?? "";
        studentInfo.FirstName = sceneBuilder.inputFirstName?.text ?? "";
        studentInfo.Patronymic = sceneBuilder.inputPatronymic?.text ?? "";
        studentInfo.Group = sceneBuilder.inputGroup?.text ?? "";

        if (string.IsNullOrWhiteSpace(studentInfo.Surname) || string.IsNullOrWhiteSpace(studentInfo.FirstName))
        {
            Debug.LogWarning("[VNPlayer] Заполните хотя бы фамилию и имя!");
            return;
        }

        Debug.Log($"[VNPlayer] Студент: {studentInfo.FullName}, группа: {studentInfo.Group}");

        // Сначала проверяем, есть ли уже сохранённая новелла на диске
        if (!forceRegenerate)
        {
            VisualNovelScene savedScene = VisualNovelScene.LoadFromDisk(caseId);
            if (savedScene != null)
            {
                Debug.Log($"[VNPlayer] Найдена сохранённая новелла для кейса {caseId}. Запуск без генерации.");
                sceneBuilder.ShowPanel(sceneBuilder.vnPanel);
                
                // Подгружаем локальные текстуры для сохранённой сцены
                LoadTexturesFromDisk(savedScene);
                
                OnNovelGenerated(savedScene);
                return;
            }
        }
        else
        {
            Debug.Log($"[VNPlayer] Выбрана принудительная перегенерация. Игнорирую кэш для кейса {caseId}.");
            
            // Если мы решили перегенерировать, давайте заодно удалим старый txt, чтобы не мешал
            string oldPath = VisualNovelScene.GetSaveFilePath(caseId);
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }

        // В противном случае — запускаем процесс генерации
        sceneBuilder.ShowPanel(sceneBuilder.loadingPanel);

        // Генерируем новеллу
        generator.Generate(caseId, OnNovelGenerated);
    }

    private void OnNextClicked()
    {
        if (!isPlaying || currentScene == null) return;

        var page = currentScene.Pages[currentPageIndex];

        if (page.DefaultNextPage >= 0 && page.DefaultNextPage < currentScene.Pages.Count)
        {
            GoToPage(page.DefaultNextPage);
        }
        else
        {
            ShowResults();
        }
    }

    private void OnRestartClicked()
    {
        currentScene = null;
        currentPageIndex = 0;
        isPlaying = false;
        playResult = null;

        sceneBuilder.ClearChoiceButtons();
        sceneBuilder.ShowPanel(sceneBuilder.studentFormPanel);
    }

    // ═══════════════════════════════════════
    // ГЕНЕРАЦИЯ ЗАВЕРШЕНА
    // ═══════════════════════════════════════

    private void OnNovelGenerated(VisualNovelScene scene)
    {
        // Скрываем экран загрузки и показываем VN
        sceneBuilder.ShowPanel(sceneBuilder.vnPanel);

        if (scene == null)
        {
            sceneBuilder.dialogueText.text = "Ошибка генерации. Попробуйте снова.";
            sceneBuilder.btnNext.gameObject.SetActive(false);
            return;
        }


        currentScene = scene;
        playResult = new VNPlayResult();
        playResult.Student = studentInfo;
        playResult.CaseId = caseId;
        isPlaying = true;
        currentPageIndex = 0;

        // ВАЖНО: Заново загружаем текстуры с диска, если они только что сгенерировались!
        LoadTexturesFromDisk(scene);

        Debug.Log($"[VNPlayer] Новелла загружена: '{scene.Title}', {scene.Pages.Count} страниц, {scene.TotalCorrectChoices} правильных ответов");

        // Обновляем заголовок
        if (sceneBuilder.titleText != null)
            sceneBuilder.titleText.text = scene.Title;

        GoToPage(0);
    }

    // ═══════════════════════════════════════
    // НАВИГАЦИЯ ПО СТРАНИЦАМ
    // ═══════════════════════════════════════

    private void GoToPage(int pageIndex)
    {
        if (currentScene == null || pageIndex < 0 || pageIndex >= currentScene.Pages.Count)
        {
            ShowResults();
            return;
        }

        currentPageIndex = pageIndex;
        var page = currentScene.Pages[pageIndex];

        // Обновляем фон
        SetBackground(page.BackgroundKey);

        // Обновляем персонажей
        UpdateCharacters(page.Characters);

        // Обновляем текст
        sceneBuilder.speakerNameText.text = page.SpeakerName ?? "";
        sceneBuilder.dialogueText.text = page.DialogueText ?? "";

        // Обновляем счётчики
        UpdateCounters();

        // Очищаем предыдущие кнопки
        sceneBuilder.ClearChoiceButtons();

        if (page.IsEnding)
        {
            // Финальная страница — показываем кнопку для перехода к результатам
            sceneBuilder.choicesContainer.SetActive(false);
            sceneBuilder.btnNext.gameObject.SetActive(true);
            var nextLabel = sceneBuilder.btnNext.GetComponentInChildren<TMP_Text>();
            if (nextLabel != null) nextLabel.text = "Завершить ▶";
        }
        else if (page.HasChoices)
        {
            // Страница с выбором — создаём кнопки
            sceneBuilder.choicesContainer.SetActive(true);
            sceneBuilder.btnNext.gameObject.SetActive(false);

            foreach (var choice in page.Choices)
            {
                var btn = sceneBuilder.CreateChoiceButton(choice.Text);

                // Замыкание для обработки клика
                string choiceText = choice.Text;
                int nextPage = choice.NextPageIndex;
                bool isCorrect = choice.IsCorrect;
                string feedback = choice.Feedback;

                btn.onClick.AddListener(() => OnChoiceClicked(choiceText, nextPage, isCorrect, feedback));
            }
        }
        else
        {
            // Текстовая страница без выбора — кнопка "Далее"
            sceneBuilder.choicesContainer.SetActive(false);
            sceneBuilder.btnNext.gameObject.SetActive(true);
            var nextLabel = sceneBuilder.btnNext.GetComponentInChildren<TMP_Text>();
            if (nextLabel != null) nextLabel.text = "Далее ▶";
        }

        Debug.Log($"[VNPlayer] Страница {pageIndex}: {page.SpeakerName} — {(page.HasChoices ? page.Choices.Count + " выборов" : "текст")}");
    }

    // ═══════════════════════════════════════
    // ОБРАБОТКА ВЫБОРА
    // ═══════════════════════════════════════

    private void OnChoiceClicked(string choiceText, int nextPageIndex, bool isCorrect, string feedback)
    {
        // Записываем результат
        playResult.RecordChoice(choiceText, isCorrect);

        Debug.Log($"[VNPlayer] Выбор: '{choiceText}' [{(isCorrect ? "✓" : "✗")}]");

        // Показываем фидбек, если есть
        if (!string.IsNullOrEmpty(feedback))
        {
            StartCoroutine(ShowFeedbackThenGoTo(feedback, isCorrect, nextPageIndex));
        }
        else
        {
            // Обновляем счётчики и идём дальше
            UpdateCounters();
            GoToPage(nextPageIndex);
        }
    }

    private IEnumerator ShowFeedbackThenGoTo(string feedback, bool isCorrect, int nextPageIndex)
    {
        // Кратко показываем фидбек в диалоговом окне
        sceneBuilder.ClearChoiceButtons();
        sceneBuilder.choicesContainer.SetActive(false);
        sceneBuilder.btnNext.gameObject.SetActive(false);

        string marker = isCorrect ? "<color=#77DD77>✓ Правильно!</color>" : "<color=#FF6666>✗ Неверно</color>";
        sceneBuilder.dialogueText.text = $"{marker}\n{feedback}";
        sceneBuilder.speakerNameText.text = "";

        UpdateCounters();

        yield return new WaitForSeconds(2.5f);

        GoToPage(nextPageIndex);
    }

    // ═══════════════════════════════════════
    // UI ОБНОВЛЕНИЯ
    // ═══════════════════════════════════════

    private void SetBackground(string bgKey)
    {
        if (sceneBuilder.backgroundImage == null) return;

        if (!string.IsNullOrEmpty(bgKey) && backgroundCache.ContainsKey(bgKey))
        {
            sceneBuilder.backgroundImage.texture = backgroundCache[bgKey];
            sceneBuilder.backgroundImage.color = Color.white;
        }
        else
        {
            // Используем цветовую заливку
            sceneBuilder.backgroundImage.texture = null;
            if (BG_COLORS.ContainsKey(bgKey ?? ""))
                sceneBuilder.backgroundImage.color = BG_COLORS[bgKey];
            else
                sceneBuilder.backgroundImage.color = new Color(0.25f, 0.22f, 0.20f, 1f);
        }
    }

    private void UpdateCharacters(List<VNCharacterSlot> slots)
    {
        // Скрываем все слоты
        if (sceneBuilder.characterLeft != null)
            sceneBuilder.characterLeft.color = new Color(1, 1, 1, 0);
        if (sceneBuilder.characterCenter != null)
            sceneBuilder.characterCenter.color = new Color(1, 1, 1, 0);
        if (sceneBuilder.characterRight != null)
            sceneBuilder.characterRight.color = new Color(1, 1, 1, 0);

        if (slots == null || currentScene == null) return;

        foreach (var slot in slots)
        {
            RawImage targetSlot = null;
            switch (slot.Position)
            {
                case VNCharacterPosition.Left: targetSlot = sceneBuilder.characterLeft; break;
                case VNCharacterPosition.Center: targetSlot = sceneBuilder.characterCenter; break;
                case VNCharacterPosition.Right: targetSlot = sceneBuilder.characterRight; break;
            }

            if (targetSlot == null) continue;

            // Пытаемся загрузить спрайт персонажа
            VNCharacter charDef = currentScene.GetCharacter(slot.CharacterId);
            string spriteKey = charDef?.SpriteKey?.ToLower() ?? slot.CharacterId.ToLower();

            if (characterCache.ContainsKey(spriteKey))
            {
                targetSlot.texture = characterCache[spriteKey];
                targetSlot.color = slot.IsHighlighted ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);
            }
            else
            {
                // Нет спрайта — показываем цветной силуэт-заглушку
                targetSlot.texture = null;
                targetSlot.color = slot.IsHighlighted
                    ? new Color(0.5f, 0.6f, 0.8f, 0.4f)
                    : new Color(0.4f, 0.4f, 0.5f, 0.3f);
            }
        }
    }

    // ═══════════════════════════════════════
    // ЛОКАЛЬНАЯ ЗАГРУЗКА ТЕКСТУР ИЗ STREAMING ASSETS (ИЛИ ПО АБС. ПУТИ)
    // ═══════════════════════════════════════

    private void LoadTexturesFromDisk(VisualNovelScene scene)
    {
        string saveDir = VisualNovelScene.GetSaveDirectory();

        // Загружаем фоны
        HashSet<string> uniqueBgs = new HashSet<string>();
        uniqueBgs.Add(scene.DefaultBackgroundKey);
        foreach (var p in scene.Pages) 
        {
            if (!string.IsNullOrEmpty(p.BackgroundKey)) 
                uniqueBgs.Add(p.BackgroundKey);
        }

        foreach (var bgKey in uniqueBgs)
        {
            string absPath = Path.Combine(saveDir, bgKey);
            if (File.Exists(absPath))
            {
                Texture2D tex = LoadTextureFromFile(absPath);
                if (tex != null) backgroundCache[bgKey] = tex;
            }
            else if (File.Exists(bgKey)) // Если bgKey — это уже сохранённый абсолютный путь (fallback для старых сохранений)
            {
                Texture2D tex = LoadTextureFromFile(bgKey);
                if (tex != null) backgroundCache[bgKey] = tex;
            }
        }

        // Загружаем персонажей
        foreach (var ch in scene.Characters)
        {
            string spriteKey = ch.SpriteKey;
            string absPath = Path.Combine(saveDir, spriteKey);
            
            if (File.Exists(absPath))
            {
                Texture2D tex = LoadTextureFromFile(absPath);
                if (tex != null) characterCache[spriteKey.ToLower()] = tex;
            }
            else if (File.Exists(spriteKey))
            {
                Texture2D tex = LoadTextureFromFile(spriteKey);
                if (tex != null) characterCache[spriteKey.ToLower()] = tex;
            }
        }
    }

    private Texture2D LoadTextureFromFile(string path)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); 
            return tex;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VNPlayer] Ошибка загрузки текстуры {path}: {e.Message}");
            return null;
        }
    }

    private void UpdateCounters()
    {
        if (playResult == null) return;

        if (sceneBuilder.counterCorrectText != null)
            sceneBuilder.counterCorrectText.text = $"✓ {playResult.CorrectAnswers}";
        if (sceneBuilder.counterWrongText != null)
            sceneBuilder.counterWrongText.text = $"✗ {playResult.WrongAnswers}";
    }

    // ═══════════════════════════════════════
    // РЕЗУЛЬТАТЫ
    // ═══════════════════════════════════════

    private void ShowResults()
    {
        isPlaying = false;

        string resultText = $"Студент: {studentInfo.FullName}\n" +
                           $"Группа: {studentInfo.Group}\n\n" +
                           $"Кейс: {currentScene?.Title ?? "—"}\n\n" +
                           $"<color=#77DD77>Правильных ответов: {playResult.CorrectAnswers}</color>\n" +
                           $"<color=#FF6666>Неправильных ответов: {playResult.WrongAnswers}</color>\n" +
                           $"Всего выборов: {playResult.TotalChoicesMade}\n\n" +
                           $"Оценка: {playResult.Score:P0}";

        if (playResult.Score >= 0.7f)
            resultText += "\n\n<color=#77DD77>Отличный результат!</color>";
        else if (playResult.Score >= 0.4f)
            resultText += "\n\n<color=#FFAA00>Хороший результат. Попробуйте улучшить!</color>";
        else
            resultText += "\n\n<color=#FF6666>Рекомендуется повторить сценарий.</color>";

        if (sceneBuilder.resultsText != null)
            sceneBuilder.resultsText.text = resultText;

        sceneBuilder.ShowPanel(sceneBuilder.resultsPanel);

        Debug.Log($"[VNPlayer] Результаты: {playResult.CorrectAnswers}/{playResult.TotalChoicesMade} ({playResult.Score:P0})");

        // Логируем для исследования
        if (PersonalityResearchLogger.Instance != null)
        {
            PersonalityResearchLogger.Instance.LogDialogue(
                $"VN_{currentScene?.CaseId}",
                studentInfo.FullName,
                $"Score: {playResult.Score:P0}, Correct: {playResult.CorrectAnswers}, Wrong: {playResult.WrongAnswers}",
                playResult.Score
            );
        }
    }
}
