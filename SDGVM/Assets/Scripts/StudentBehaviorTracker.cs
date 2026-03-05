using UnityEngine;
using System;

/// <summary>
/// Отслеживает поведение студента во время диалога (необдуманные и списанные ответы)
/// Используется для исследования в главе ВКР 2.4.8
/// </summary>
public class StudentBehaviorTracker : MonoBehaviour
{
    private static StudentBehaviorTracker _instance;
    public static StudentBehaviorTracker Instance => _instance;

    [Header("Настройки оценки поведения")]
    [Tooltip("Ответ быстрее этого времени (в сек) считается подозрительным")]
    public float fastResponseThreshold = 3.0f;
    
    [Tooltip("Длина текста для определения 'списанного' пристром ответе")]
    public int copiedTextLengthThreshold = 50;
    
    [Tooltip("Если ответ короткий и быстрый - он необдуманный")]
    public int thoughtlessTextLengthThreshold = 10;

    // Внутреннее состояние
    private float lastNPCResponseTime;
    
    // Счетчики поведения (для накрутки характеристик в будущем)
    public int ThoughtlessAnswersCount { get; private set; }
    public int CopiedAnswersCount { get; private set; }

    public enum BehaviorState
    {
        Normal,
        Thoughtless, // Необдуманный, слишком быстрый
        Copied       // Списанный (копипаст)
    }

    void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    /// <summary>
    /// Вызывать сразу после того, как NPC закончил печатать ответ
    /// </summary>
    public void RecordNPCResponseFinished()
    {
        lastNPCResponseTime = Time.time;
    }

    /// <summary>
    /// Вызывать при отправке сообщения игроком
    /// </summary>
    public BehaviorState AnalyzePlayerInput(string playerInput)
    {
        float responseDelay = Time.time - lastNPCResponseTime;
        int inputLength = playerInput.Length;

        Debug.Log($"[BehaviorTracker] Замер времени: {responseDelay:F2}с, Длина: {inputLength} символов.");

        // Если это первый ответ (нет предыдущего)
        if (lastNPCResponseTime <= 0.01f)
            return BehaviorState.Normal;

        if (responseDelay < fastResponseThreshold)
        {
            if (inputLength > copiedTextLengthThreshold)
            {
                CopiedAnswersCount++;
                Debug.LogWarning("[BehaviorTracker] Обнаружен возможный КОПИПАСТ (списанный ответ)!");
                return BehaviorState.Copied;
            }
            else if (inputLength < thoughtlessTextLengthThreshold || IsSimpleGrabar(playerInput))
            {
                ThoughtlessAnswersCount++;
                Debug.LogWarning("[BehaviorTracker] Обнаружен НЕОБДУМАННЫЙ (быстрый) ответ!");
                return BehaviorState.Thoughtless;
            }
        }

        return BehaviorState.Normal;
    }

    private bool IsSimpleGrabar(string input)
    {
        string lower = input.ToLower().Trim();
        string[] simpleAnswers = { "да", "нет", "ок", "понятно", "ясно", "хорошо", "ага", "угу" };
        foreach(var s in simpleAnswers)
        {
            if (lower == s) return true;
        }
        return false;
    }

    public string GetBehaviorPromptModifier(BehaviorState state)
    {
        switch (state)
        {
            case BehaviorState.Copied:
                return "\n\nВАЖНО: Игрок только что вставил огромный скопированный текст за долю секунды! Возмутись этим! Скажи, что скопировать из интернета может каждый, и попроси ответить СВОИМИ словами. Это важно для оценки личности.";
            case BehaviorState.Thoughtless:
                return "\n\nВАЖНО: Игрок ответил за долю секунды на твой сложный вопрос абсолютно необдуманно. Покажи легкое раздражение. Скажи, что он даже не подумал над твоими словами, и попроси отнестись к вопросу серьезнее.";
            default:
                return "";
        }
    }
}
