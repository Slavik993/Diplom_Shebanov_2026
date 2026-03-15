using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════
// МОДЕЛЬ ДАННЫХ ВИЗУАЛЬНОЙ НОВЕЛЛЫ
// Используется для генерации и воспроизведения VN-сцен
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Информация о студенте (вводится на начальной форме)
/// </summary>
[Serializable]
public class StudentInfo
{
    public string Surname;      // Фамилия
    public string FirstName;    // Имя
    public string Patronymic;   // Отчество
    public string Group;        // Группа

    public string FullName => $"{Surname} {FirstName} {Patronymic}".Trim();
}

/// <summary>
/// Позиция персонажа на экране
/// </summary>
public enum VNCharacterPosition
{
    Left,
    Center,
    Right
}

/// <summary>
/// Определение персонажа визуальной новеллы
/// </summary>
[Serializable]
public class VNCharacter
{
    public string Id;           // Уникальный ID (например "lena", "temir", "judge")
    public string DisplayName;  // Отображаемое имя ("Лена", "Темир", "Судья")
    public string SpriteKey;    // Ключ спрайта (для загрузки из Resources/VN/Characters/)
    public string Description;  // Описание персонажа для AI генерации

    public VNCharacter() { }

    public VNCharacter(string id, string displayName, string spriteKey = "", string description = "")
    {
        Id = id;
        DisplayName = displayName;
        SpriteKey = string.IsNullOrEmpty(spriteKey) ? id : spriteKey;
        Description = description;
    }
}

/// <summary>
/// Позиция персонажа на конкретной странице
/// </summary>
[Serializable]
public class VNCharacterSlot
{
    public string CharacterId;              // ID персонажа
    public VNCharacterPosition Position;    // Позиция на экране
    public bool IsHighlighted;              // Подсвечен ли (говорящий)

    public VNCharacterSlot() { }

    public VNCharacterSlot(string charId, VNCharacterPosition pos, bool highlighted = false)
    {
        CharacterId = charId;
        Position = pos;
        IsHighlighted = highlighted;
    }
}

/// <summary>
/// Вариант выбора на странице визуальной новеллы
/// </summary>
[Serializable]
public class VNChoice
{
    public string Text;             // Текст кнопки выбора
    public int NextPageIndex;       // Индекс следующей страницы (-1 = конец)
    public bool IsCorrect;          // Правильный ли ответ
    public string Feedback;         // Обратная связь после выбора (опционально)

    public VNChoice() { }

    public VNChoice(string text, int nextPageIndex, bool isCorrect = false, string feedback = "")
    {
        Text = text;
        NextPageIndex = nextPageIndex;
        IsCorrect = isCorrect;
        Feedback = feedback;
    }
}

/// <summary>
/// Одна страница визуальной новеллы
/// </summary>
[Serializable]
public class VNPage
{
    public int PageIndex;                       // Индекс страницы
    public string SpeakerName;                  // Имя говорящего (пустое = нарратор)
    public string DialogueText;                 // Текст диалога
    public string BackgroundKey;                // Ключ фоновой картинки
    public List<VNCharacterSlot> Characters;    // Персонажи на экране
    public List<VNChoice> Choices;              // Варианты выбора (пустой = просто текстовая страница)
    public bool IsEnding;                       // Финальная страница
    public int DefaultNextPage;                 // Следующая страница (для страниц без выбора)

    public VNPage()
    {
        Characters = new List<VNCharacterSlot>();
        Choices = new List<VNChoice>();
        DefaultNextPage = -1;
    }

    /// <summary>
    /// Страница с выбором?
    /// </summary>
    public bool HasChoices => Choices != null && Choices.Count > 0;
}

/// <summary>
/// Полная визуальная новелла (набор страниц + метаданные)
/// </summary>
[Serializable]
public class VisualNovelScene
{
    public int CaseId;                              // Связанный кейс адаптации
    public string Title;                            // Название новеллы
    public string Description;                      // Краткое описание
    public List<VNCharacter> Characters;            // Все персонажи новеллы
    public List<VNPage> Pages;                      // Все страницы
    public string DefaultBackgroundKey;             // Фон по умолчанию
    public int TotalCorrectChoices;                 // Общее кол-во правильных ответов (для подсчёта)

    public VisualNovelScene()
    {
        Characters = new List<VNCharacter>();
        Pages = new List<VNPage>();
    }

    /// <summary>
    /// Находит персонажа по ID
    /// </summary>
    public VNCharacter GetCharacter(string charId)
    {
        return Characters.Find(c => c.Id == charId);
    }

    /// <summary>
    /// Подсчитывает общее число правильных ответов в новелле
    /// </summary>
    public int CountCorrectChoices()
    {
        int count = 0;
        foreach (var page in Pages)
        {
            if (page.Choices == null) continue;
            foreach (var choice in page.Choices)
            {
                if (choice.IsCorrect) count++;
            }
        }
        TotalCorrectChoices = count;
        return count;
    }

    // ═══════════════════════════════════════════════════
    // СЕРИАЛИЗАЦИЯ И СОХРАНЕНИЕ
    // ═══════════════════════════════════════════════════

    public static string GetSaveDirectory()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, "VisualNovels");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public static string GetSaveFilePath(int caseId)
    {
        return Path.Combine(GetSaveDirectory(), $"novel_case_{caseId}.txt");
    }

    /// <summary>
    /// Сохраняет сгенерированную новеллу на диск
    /// </summary>
    public void SaveToDisk()
    {
        try
        {
            string path = GetSaveFilePath(CaseId);
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(path, json);
            Debug.Log($"[VN Data] Новелла {CaseId} сохранена: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VN Data] Ошибка при сохранении новеллы: {e.Message}");
        }
    }

    /// <summary>
    /// Загружает ранее сгенерированную новеллу с диска (если есть)
    /// </summary>
    public static VisualNovelScene LoadFromDisk(int caseId)
    {
        try
        {
            string path = GetSaveFilePath(caseId);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var scene = JsonUtility.FromJson<VisualNovelScene>(json);
                Debug.Log($"[VN Data] Новелла {caseId} загружена с диска.");
                return scene;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VN Data] Ошибка при загрузке новеллы: {e.Message}");
        }
        return null;
    }
}

/// <summary>
/// Результат прохождения визуальной новеллы
/// </summary>
[Serializable]
public class VNPlayResult
{
    public StudentInfo Student;
    public int CaseId;
    public int CorrectAnswers;
    public int WrongAnswers;
    public int TotalChoicesMade;
    public float Score;                 // 0..1
    public List<string> ChoiceLog;      // Лог выборов

    public VNPlayResult()
    {
        ChoiceLog = new List<string>();
    }

    public void RecordChoice(string choiceText, bool isCorrect)
    {
        TotalChoicesMade++;
        if (isCorrect) CorrectAnswers++;
        else WrongAnswers++;
        ChoiceLog.Add($"[{(isCorrect ? "✓" : "✗")}] {choiceText}");
        Score = TotalChoicesMade > 0 ? (float)CorrectAnswers / TotalChoicesMade : 0f;
    }
}
