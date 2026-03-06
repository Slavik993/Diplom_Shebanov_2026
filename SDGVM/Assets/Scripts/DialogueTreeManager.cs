using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Управляет текущим диалоговым деревом: загрузка, переходы, UI кнопок выбора.
/// Кнопки создаются ВНУТРИ чата (как обычные строки + кликабельные элементы).
/// </summary>
public class DialogueTreeManager : MonoBehaviour
{
    public static DialogueTreeManager Instance { get; private set; }

    [Header("UI: Чат")]
    public TMP_Text chatHistoryText;
    public ScrollRect chatScrollRect;

    // Текущее состояние
    private DialogueTree currentTree;
    private DialogueNode currentNode;
    private bool isActive = false;
    private float accumulatedHI = 0f;
    private int choiceCount = 0;

    // Динамически созданные кнопки
    private List<GameObject> activeButtons = new List<GameObject>();

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
        string npcColor = "#E29C45";
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
            ShowEnding();
            return;
        }

        // Создаём кнопки выбора
        if (node.Choices != null && node.Choices.Count > 0)
        {
            CreateChoiceButtons(node.Choices);
        }
    }

    /// <summary>
    /// Создаёт кнопки выбора как дочерние элементы Content в ScrollRect чата
    /// </summary>
    private void CreateChoiceButtons(List<PlayerChoice> choices)
    {
        // Ищем Content в ScrollRect (родитель chatHistoryText)
        Transform contentParent = null;
        
        if (chatHistoryText != null)
        {
            contentParent = chatHistoryText.transform.parent;
        }
        
        if (contentParent == null)
        {
            Debug.LogError("[DialogueTree] Не найден Content для создания кнопок!");
            // Fallback: показываем выборы как текст в чате
            ShowChoicesAsText(choices);
            return;
        }

        // Добавляем разделитель в чат
        if (chatHistoryText != null)
        {
            chatHistoryText.text += "\n<color=#AAAAAA><size=14>── Выберите ответ ──</size></color>";
        }

        // Создаём кнопки ПОСЛЕ текста чата (как siblings)
        foreach (var choice in choices)
        {
            GameObject btnObj = new GameObject($"ChoiceBtn_{choice.NextNodeId}");
            btnObj.transform.SetParent(contentParent, false);

            // RectTransform
            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 55); // Высота кнопки
            
            // LayoutElement чтобы VLG корректно расположил
            var le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = 50f;
            le.preferredHeight = 55f;
            le.flexibleWidth = 1f;

            // Фон кнопки
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.22f, 0.14f, 0.06f, 0.95f); // Тёмная латунь
            img.raycastTarget = true;

            // Button component
            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.22f, 0.14f, 0.06f, 0.95f);
            colors.highlightedColor = new Color(0.45f, 0.30f, 0.15f, 1f);
            colors.pressedColor = new Color(0.60f, 0.40f, 0.20f, 1f);
            colors.selectedColor = new Color(0.35f, 0.22f, 0.10f, 1f);
            btn.colors = colors;
            btn.targetGraphic = img;

            // Текст кнопки
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);

            var tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = $"► {choice.Text}";
            tmpText.fontSize = 16;
            tmpText.color = new Color(0.92f, 0.80f, 0.55f, 1f); // Золотистый
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;
            tmpText.enableWordWrapping = true;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            tmpText.raycastTarget = false;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(15, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            // Обработчик клика
            string nextId = choice.NextNodeId;
            float bonus = choice.HumanityBonus;
            string choiceText = choice.Text;
            btn.onClick.AddListener(() => OnChoiceSelected(nextId, bonus, choiceText));

            activeButtons.Add(btnObj);
        }

        // Прокрутка вниз
        ScrollToBottom();
    }

    /// <summary>
    /// Fallback: показать выборы как кликабельный текст (если кнопки не создаются)
    /// </summary>
    private void ShowChoicesAsText(List<PlayerChoice> choices)
    {
        if (chatHistoryText == null) return;

        chatHistoryText.text += "\n<color=#AAAAAA><size=14>── Варианты ответа: ──</size></color>";
        
        for (int i = 0; i < choices.Count; i++)
        {
            string hiLabel = choices[i].HumanityBonus > 0 ? "+" : "";
            chatHistoryText.text += $"\n<color=#EBCC7A><size=18>  [{i + 1}] {choices[i].Text}</size></color>";
        }

        chatHistoryText.text += "\n<color=#AAAAAA><size=14>(Введите номер 1-" + choices.Count + " в поле ввода)</size></color>";
        
        // Сохраняем выборы для обработки текстового ввода
        _pendingChoices = choices;
    }

    private List<PlayerChoice> _pendingChoices;

    /// <summary>
    /// Обработка текстового ввода (когда кнопки не работают)
    /// </summary>
    public bool TryProcessTextInput(string input)
    {
        if (_pendingChoices == null || _pendingChoices.Count == 0) return false;

        input = input.Trim();
        if (int.TryParse(input, out int idx) && idx >= 1 && idx <= _pendingChoices.Count)
        {
            var choice = _pendingChoices[idx - 1];
            _pendingChoices = null;
            OnChoiceSelected(choice.NextNodeId, choice.HumanityBonus, choice.Text);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Обработка выбора игрока
    /// </summary>
    private void OnChoiceSelected(string nextNodeId, float humanityBonus, string choiceText)
    {
        // Добавляем выбор игрока в чат
        string playerColor = "#77DD77";
        if (chatHistoryText != null)
        {
            chatHistoryText.text += $"\n<color={playerColor}><size=20>Игрок:</size></color> <size=20>{choiceText}</size>";
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
    /// Очищает динамические кнопки
    /// </summary>
    private void ClearChoices()
    {
        foreach (var btn in activeButtons)
        {
            if (btn != null) Destroy(btn);
        }
        activeButtons.Clear();
        _pendingChoices = null;
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
