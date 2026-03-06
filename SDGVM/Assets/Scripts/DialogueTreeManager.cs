using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Управляет текущим диалоговым деревом: загрузка, переходы, UI кнопок выбора.
/// Интегрируется с PersonalityAnalyzer для HI и PersonalityResearchLogger для логирования.
/// </summary>
public class DialogueTreeManager : MonoBehaviour
{
    public static DialogueTreeManager Instance { get; private set; }

    [Header("UI: Панель выбора")]
    [Tooltip("Родительский объект для кнопок выбора (VerticalLayoutGroup)")]
    public Transform choicesPanel;

    [Tooltip("Префаб кнопки выбора (Button с TMP_Text)")]
    public GameObject choiceButtonPrefab;

    [Header("UI: Чат")]
    public TMP_Text chatHistoryText;

    // Текущее состояние
    private DialogueTree currentTree;
    private DialogueNode currentNode;
    private bool isActive = false;
    private float accumulatedHI = 0f;
    private int choiceCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// Проверяет, есть ли готовое дерево для данного кейса
    /// </summary>
    public bool HasTreeForCase(int caseId)
    {
        return DialogueTrees.GetTree(caseId) != null;
    }

    /// <summary>
    /// Запускает диалоговое дерево для указанного кейса
    /// </summary>
    public void StartTree(int caseId)
    {
        currentTree = DialogueTrees.GetTree(caseId);
        if (currentTree == null)
        {
            Debug.LogWarning($"[DialogueTree] Нет дерева для кейса {caseId}");
            return;
        }

        isActive = true;
        accumulatedHI = 0f;
        choiceCount = 0;

        currentNode = currentTree.GetStartNode();
        if (currentNode != null)
        {
            ShowNode(currentNode);
        }

        Debug.Log($"[DialogueTree] Запущено дерево для кейса {caseId}");
    }

    /// <summary>
    /// Активно ли сейчас дерево?
    /// </summary>
    public bool IsActive => isActive;

    /// <summary>
    /// Отображает узел диалога: текст NPC + кнопки выбора
    /// </summary>
    private void ShowNode(DialogueNode node)
    {
        if (node == null) return;

        // Добавляем реплику NPC в чат
        string npcColor = "#E29C45"; // Медный (Steampunk)
        if (chatHistoryText != null)
        {
            chatHistoryText.text += $"\n<color={npcColor}><size=20>NPC:</size></color> <size=20>{node.Text}</size>";
        }

        // Анализируем HI текста NPC
        if (PersonalityAnalyzer.Instance != null)
        {
            var metrics = PersonalityAnalyzer.Instance.AnalyzeResponse(node.Text);
            Debug.Log($"[DialogueTree] HI узла '{node.NodeId}': {metrics.HumanityIndex:P0}");
        }

        // Логируем
        if (PersonalityResearchLogger.Instance != null)
        {
            float nodeHI = PersonalityAnalyzer.Instance != null 
                ? PersonalityAnalyzer.Instance.AnalyzeResponse(node.Text).HumanityIndex 
                : 0f;
            PersonalityResearchLogger.Instance.LogDialogue("DialogueTree", "", node.Text, nodeHI);
        }

        // Очищаем предыдущие кнопки
        ClearChoices();

        if (node.IsEnding)
        {
            // Финальный узел — показываем итог
            ShowEnding();
            return;
        }

        // Создаём кнопки выбора
        if (node.Choices != null && node.Choices.Count > 0)
        {
            ShowChoices(node.Choices);
        }
    }

    /// <summary>
    /// Показывает кнопки выбора для игрока
    /// </summary>
    private void ShowChoices(List<PlayerChoice> choices)
    {
        if (choicesPanel == null || choiceButtonPrefab == null)
        {
            // Если нет префаба/панели — создаём кнопки программно
            CreateChoicesFromCode(choices);
            return;
        }

        foreach (var choice in choices)
        {
            var btnObj = Instantiate(choiceButtonPrefab, choicesPanel);
            var tmpText = btnObj.GetComponentInChildren<TMP_Text>();
            if (tmpText != null) tmpText.text = choice.Text;

            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string nextId = choice.NextNodeId;
                float bonus = choice.HumanityBonus;
                btn.onClick.AddListener(() => OnChoiceSelected(nextId, bonus, choice.Text));
            }
        }

        if (choicesPanel != null)
            choicesPanel.gameObject.SetActive(true);
    }

    /// <summary>
    /// Создаёт кнопки прямо в коде (если нет префаба)
    /// </summary>
    private void CreateChoicesFromCode(List<PlayerChoice> choices)
    {
        // Используем chatHistoryText для подсказки, а реальные кнопки создаём рядом
        if (chatHistoryText == null) return;

        // Находим или создаём панель
        Canvas canvas = chatHistoryText.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("ChoicesPanel_Auto");
        panel.transform.SetParent(chatHistoryText.transform.parent, false);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(10, 10, 5, 5);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = panel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        choicesPanel = panel.transform;

        foreach (var choice in choices)
        {
            // Создаём кнопку
            GameObject btnObj = new GameObject($"Choice_{choice.NextNodeId}");
            btnObj.transform.SetParent(panel.transform, false);

            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.15f, 0.08f, 0.95f); // Тёмная латунь

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.45f, 0.30f, 0.15f, 1f); // Подсветка при наведении
            colors.pressedColor = new Color(0.60f, 0.40f, 0.20f, 1f);
            btn.colors = colors;

            // LayoutElement для высоты
            var le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = 45f;
            le.preferredHeight = 50f;

            // Текст
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            var tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = choice.Text;
            tmpText.fontSize = 18;
            tmpText.color = new Color(0.92f, 0.80f, 0.55f, 1f); // Золотистый текст
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;
            tmpText.enableWordWrapping = true;
            tmpText.margin = new Vector4(10, 5, 10, 5);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Обработчик
            string nextId = choice.NextNodeId;
            float bonus = choice.HumanityBonus;
            string choiceText = choice.Text;
            btn.onClick.AddListener(() => OnChoiceSelected(nextId, bonus, choiceText));
        }
    }

    /// <summary>
    /// Обработка выбора игрока
    /// </summary>
    private void OnChoiceSelected(string nextNodeId, float humanityBonus, string choiceText)
    {
        // Добавляем выбор игрока в чат
        string playerColor = "#77DD77"; // Изумрудный
        if (chatHistoryText != null)
        {
            chatHistoryText.text += $"\n<color={playerColor}><size=28>Игрок:</size></color> <size=28>{choiceText}</size>";
        }

        // Накапливаем HI
        accumulatedHI += humanityBonus;
        choiceCount++;

        Debug.Log($"[DialogueTree] Выбор: '{choiceText}' → узел '{nextNodeId}', HI бонус: {humanityBonus:+0.0;-0.0}");

        // Очищаем кнопки
        ClearChoices();

        // Переходим к следующему узлу
        if (currentTree != null)
        {
            currentNode = currentTree.GetNode(nextNodeId);
            if (currentNode != null)
            {
                ShowNode(currentNode);
            }
            else
            {
                Debug.LogWarning($"[DialogueTree] Узел '{nextNodeId}' не найден!");
                ShowEnding();
            }
        }
    }

    /// <summary>
    /// Завершение сценария
    /// </summary>
    private void ShowEnding()
    {
        isActive = false;

        float averageHI = choiceCount > 0 ? accumulatedHI / choiceCount : 0f;
        averageHI = Mathf.Clamp01(averageHI);

        string resultColor = averageHI > 0.5f ? "#77DD77" : "#FF6666";
        
        if (chatHistoryText != null)
        {
            chatHistoryText.text += $"\n\n<color=#E29C45><size=22>═══ Сценарий завершён ═══</size></color>";
            chatHistoryText.text += $"\n<color={resultColor}><size=20>Индекс человечности (HI): {averageHI:P0}</size></color>";
            chatHistoryText.text += $"\n<color=#AAAAAA><size=16>Сделано выборов: {choiceCount}</size></color>";
        }

        Debug.Log($"[DialogueTree] Сценарий завершён. Средний HI: {averageHI:P0}, выборов: {choiceCount}");
    }

    /// <summary>
    /// Очищает панель кнопок
    /// </summary>
    private void ClearChoices()
    {
        if (choicesPanel == null) return;

        for (int i = choicesPanel.childCount - 1; i >= 0; i--)
        {
            Destroy(choicesPanel.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Полный сброс (для нового сценария)
    /// </summary>
    public void Reset()
    {
        isActive = false;
        currentTree = null;
        currentNode = null;
        accumulatedHI = 0f;
        choiceCount = 0;
        ClearChoices();
    }
}
