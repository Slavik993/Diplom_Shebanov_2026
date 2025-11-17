using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;  // Для Action
using LLMUnity;  // LLM-Unity

public class StoryTeller : MonoBehaviour
{
    [Header("UI Элементы")]
    public TMP_InputField inputGenre;
    public TMP_InputField inputDifficulty;
    public TMP_Text outputQuest;
    public Button generateButton;

    [Header("LLM")]
    public LLM llm;  // Назначь в инспекторе

    private string currentResponse = "";

    void Start()
    {
        if (generateButton != null)
            generateButton.onClick.AddListener(() => GenerateQuest());
    }

    // Public метод для SDGVManager
    public void GenerateQuest()
    {
        if (llm == null)
        {
            Debug.LogError("[StoryTeller] LLM не назначен!");
            return;
        }

        currentResponse = "Генерация...";
        if (outputQuest != null) outputQuest.text = currentResponse;

        string genre = inputGenre?.text ?? "фэнтези";
        string difficulty = inputDifficulty?.text ?? "средняя";
        string prompt = $"Создай квест. Жанр: {genre}. Сложность: {difficulty}. Формат JSON.";

        // ← ИСПРАВЛЕНО: llm.Chat (реальный API, по GitHub LLMUnity)
        llm.Chat(
            prompt,
            OnTokenReceived,  // Стриминг
            OnChatCompleted   // Завершение
        );
    }

    private void OnTokenReceived(string token)
    {
        currentResponse += token;
        if (outputQuest != null) outputQuest.text = currentResponse;
        Debug.Log("[StoryTeller] Токен: " + token);
    }

    private void OnChatCompleted(string fullResponse)
    {
        currentResponse = fullResponse;
        ParseAndDisplayQuest(fullResponse);
        Debug.Log("[StoryTeller] Квест готов: " + fullResponse);
    }

    private void ParseAndDisplayQuest(string jsonResponse)
    {
        if (outputQuest == null) return;

        try
        {
            QuestData quest = JsonUtility.FromJson<QuestData>(jsonResponse);
            outputQuest.text = $"Заголовок: {quest.title}\nОписание: {quest.description}\nЦель: {quest.objective}\nНаграда: {quest.reward}";
        }
        catch
        {
            outputQuest.text = "Ошибка парсинга: " + jsonResponse;
        }
    }
}

[System.Serializable]
public class QuestData
{
    public string title;
    public string description;
    public string objective;
    public string reward;
}