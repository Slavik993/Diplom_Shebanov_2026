using UnityEngine;
using System;
using System.IO;
using System.Text;

/// <summary>
/// Логирует диалоги для исследования формирования личности NPC
/// Сохраняет в CSV: Timestamp, PersonalityDescription, PlayerMessage, NPCResponse
/// </summary>
public class PersonalityResearchLogger : MonoBehaviour
{
    [Header("Настройки логирования")]
    public bool enableLogging = true;
    public string logFileName = "personality_research_log.csv";
    
    private string logFilePath;
    private static PersonalityResearchLogger _instance;
    public static PersonalityResearchLogger Instance => _instance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Создаём путь к файлу логов
        string folder = Path.Combine(Application.dataPath, "ResearchLogs");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
            
        logFilePath = Path.Combine(folder, logFileName);
        
        // Создаём заголовок CSV, если файл новый
        if (!File.Exists(logFilePath))
        {
            File.WriteAllText(logFilePath, "Timestamp,SessionID,PersonalityDescription,PlayerMessage,NPCResponse\n", Encoding.UTF8);
        }
    }

    private string _sessionId;
    private string SessionId
    {
        get
        {
            if (string.IsNullOrEmpty(_sessionId))
                _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return _sessionId;
        }
    }

    /// <summary>
    /// Логирует диалог в CSV файл
    /// </summary>
    public void LogDialogue(string personalityDescription, string playerMessage, string npcResponse)
    {
        if (!enableLogging) return;
        
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Экранируем кавычки и переносы для CSV
            string safePersonality = EscapeCSV(personalityDescription);
            string safePlayer = EscapeCSV(playerMessage);
            string safeNPC = EscapeCSV(npcResponse);
            
            string line = $"{timestamp},{SessionId},{safePersonality},{safePlayer},{safeNPC}\n";
            
            File.AppendAllText(logFilePath, line, Encoding.UTF8);
            
            Debug.Log($"[PersonalityResearch] Диалог записан в {logFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PersonalityResearch] Ошибка записи: {e.Message}");
        }
    }

    private string EscapeCSV(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        
        // Заменяем переносы строк на пробелы, кавычки на двойные
        string result = input.Replace("\n", " ").Replace("\r", " ");
        result = result.Replace("\"", "\"\"");
        
        // Оборачиваем в кавычки если есть запятые или кавычки
        if (result.Contains(",") || result.Contains("\"") || result.Contains(";"))
            result = $"\"{result}\"";
            
        return result;
    }

    /// <summary>
    /// Начинает новую сессию исследования
    /// </summary>
    [ContextMenu("Начать новую сессию")]
    public void StartNewSession()
    {
        _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Debug.Log($"[PersonalityResearch] Новая сессия: {_sessionId}");
    }

    /// <summary>
    /// Открывает папку с логами
    /// </summary>
    [ContextMenu("Открыть папку логов")]
    public void OpenLogFolder()
    {
        string folder = Path.Combine(Application.dataPath, "ResearchLogs");
        Application.OpenURL("file://" + folder);
    }
}
