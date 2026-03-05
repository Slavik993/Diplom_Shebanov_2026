using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Асинхронный парсер новостей и мероприятий с официального сайта МУИВ (muiv.ru).
/// Использует UnityWebRequest для скачивания HTML и Regex для извлечения заголовков.
/// </summary>
public class MuivEventParser : MonoBehaviour
{
    public static MuivEventParser Instance { get; private set; }

    [Header("Настройки парсинга")]
    [Tooltip("URL страницы новостей МУИВ")]
    public string targetUrl = "https://www.muiv.ru/news/";
    
    [Tooltip("Количество новостей для парсинга")]
    public int maxNewsItems = 3;

    [Tooltip("Задержка между повторными попытками (сек)")]
    public float retryDelay = 60f;

    [Header("Спарсенные данные")]
    public List<string> ParsedNewsTitles = new List<string>();
    
    public bool IsParsed { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Запускаем парсинг при старте приложения
        StartCoroutine(ParseEventsRoutine());
    }

    public void ForceUpdateNews()
    {
        StartCoroutine(ParseEventsRoutine());
    }

    private IEnumerator ParseEventsRoutine()
    {
        IsParsed = false;
        
        while (!IsParsed)
        {
            Debug.Log($"[MuivEventParser] Пытаемся загрузить новости с {targetUrl}...");
            
            using (UnityWebRequest request = UnityWebRequest.Get(targetUrl))
            {
                // Добавляем user-agent, чтобы сервер не отклонил запрос
                request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"[MuivEventParser] Ошибка загрузки новостей: {request.error}. Повтор через {retryDelay} сек.");
                    yield return new WaitForSeconds(retryDelay);
                    continue;
                }

                // Получаем сырой HTML код
                string html = request.downloadHandler.text;
                
                // Парсим заголовки. На сайте МУИВ новости обычно лежат в блоках с определенными классами или тегами.
                // Так как у нас нет HtmlAgilityPack, используем Regex для поиска текста внутри типичных тегов.
                // Ищем комбинации, похожие на заголовки новостей (например, внутри <a ...> Текст </a>)
                // Пример: <a href="/news/..." class="news-list-item__title">Заголовок новости</a>
                
                // Это регулярное выражение ищет теги <a> или <div> с текстом, характерным для заголовков
                // Адаптировано под типичную структуру битрикс/сайтов вузов
                MatchCollection matches = Regex.Matches(html, @"<a[^>]*href=[""']/news/[^>]*>(?<title>[^<]+)</a>");
                
                List<string> newTitles = new List<string>();
                
                foreach (Match match in matches)
                {
                    string title = match.Groups["title"].Value.Trim();
                    
                    // Фильтруем мусор и слишком короткие/длинные строки
                    if (title.Length > 10 && title.Length < 150 && !title.Contains("<img") && !newTitles.Contains(title))
                    {
                        // Очищаем от HTML сущностей (например, &nbsp;, &quot;)
                        title = title.Replace("&nbsp;", " ").Replace("&quot;", "\"").Replace("&laquo;", "«").Replace("&raquo;", "»");
                        newTitles.Add(title);
                        
                        if (newTitles.Count >= maxNewsItems) break;
                    }
                }

                if (newTitles.Count > 0)
                {
                    ParsedNewsTitles = newTitles;
                    IsParsed = true;
                    Debug.Log($"[MuivEventParser] Успешно загружено {ParsedNewsTitles.Count} новостей с сайта МУИВ.");
                    
                    // Выведем их для отладки
                    foreach (var t in ParsedNewsTitles)
                    {
                        Debug.Log($"[MuivNews] {t}");
                    }
                }
                else
                {
                    Debug.LogWarning("[MuivEventParser] Страница загружена, но новости не найдены Regex-парсером. Возможно, изменилась верстка сайта.");
                    // Фоллбэк: если распарсить сайт не удалось, ставим заглушку
                    SetFallbackNews();
                    IsParsed = true;
                }
            }
        }
    }
    
    private void SetFallbackNews()
    {
        ParsedNewsTitles = new List<string>
        {
            "Стартовал прием заявок на научно-практическую конференцию Витте",
            "Московский университет имени С.Ю. Витте открывает новые программы ДО",
            "Встреча студентов с работодателями в рамках недели карьеры МУИВ"
        };
        Debug.Log("[MuivEventParser] Использованы запасные новости (Fallback).");
    }

    /// <summary>
    /// Генерирует строку контекста для LLM промпта на основе спарсенных новостей
    /// </summary>
    public string GetParsedNewsContextForPrompt()
    {
        // Если еще не спарсили
        if (!IsParsed || ParsedNewsTitles.Count == 0) return "";

        string context = "\nАКТУАЛЬНЫЕ НОВОСТИ МУИВ (упоминай естественно в диалоге, если это уместно):";
        foreach (var news in ParsedNewsTitles)
        {
            context += $"\n• {news}";
        }
        return context;
    }
}
