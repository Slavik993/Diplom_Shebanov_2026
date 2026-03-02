using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Реестр учебно-воспитательных мероприятий Московского университета им. С.Ю. Витте.
/// Привязывает реальный календарь вуза к игровым сценариям.
/// Мероприятия взяты из официального плана воспитательной работы МУИВ.
/// </summary>
public class UniversityEventsManager : MonoBehaviour
{
    public static UniversityEventsManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // =============================================
    //  РЕЕСТР МЕРОПРИЯТИЙ МУИВ (учебный год)
    // =============================================

    public static List<UniversityEvent> GetAllEvents()
    {
        return new List<UniversityEvent>
        {
            // ===== СЕНТЯБРЬ =====
            new UniversityEvent
            {
                Id = "sept_adaptation",
                Title = "Адаптационная неделя первокурсников",
                Month = 9, ApproxDay = 1,
                Category = EventCategory.Adaptation,
                Description = "Знакомство с вузом, кураторами, экскурсия по кампусу, получение студенческого билета.",
                Department = "Деканат, Студенческий совет",
                LinkedCaseIds = new int[] { 21 }
            },
            new UniversityEvent
            {
                Id = "sept_initiation",
                Title = "Посвящение в студенты",
                Month = 9, ApproxDay = 15,
                Category = EventCategory.Culture,
                Description = "Торжественное мероприятие для первокурсников. Концерт, викторина, командные игры.",
                Department = "Студенческий совет",
                LinkedCaseIds = new int[] { 22 }
            },

            // ===== ОКТЯБРЬ =====
            new UniversityEvent
            {
                Id = "oct_curator",
                Title = "Кураторский час: правила внутреннего распорядка",
                Month = 10, ApproxDay = 5,
                Category = EventCategory.Curatorship,
                Description = "Куратор проводит беседу о правилах поведения, пропусках, академической задолженности.",
                Department = "Деканат",
                LinkedCaseIds = new int[] { 23 }
            },
            new UniversityEvent
            {
                Id = "oct_blood_donation",
                Title = "Акция «Стань донором — спаси жизнь»",
                Month = 10, ApproxDay = 20,
                Category = EventCategory.Social,
                Description = "Выездная станция переливания крови в МУИВ. Все студенты могут стать донорами.",
                Department = "Профком, Медпункт",
                LinkedCaseIds = new int[] { 24 }
            },

            // ===== НОЯБРЬ =====
            new UniversityEvent
            {
                Id = "nov_conference",
                Title = "Всероссийская научно-практическая конференция «Стратегии национального развития: экономика»",
                Month = 11, ApproxDay = 28,
                Category = EventCategory.Science,
                Description = "Студенты представляют доклады по экономическим исследованиям. Секции: макроэкономика, финтех, МСБ.",
                Department = "Научный отдел",
                LinkedCaseIds = new int[] { 25 }
            },

            // ===== ДЕКАБРЬ =====
            new UniversityEvent
            {
                Id = "dec_legal_game",
                Title = "Юридическая деловая игра «Суд присяжных»",
                Month = 12, ApproxDay = 4,
                Category = EventCategory.Professional,
                Description = "Студенты-юристы разыгрывают судебный процесс с реальными ролями: судья, адвокат, прокурор, присяжные.",
                Department = "Юридический факультет",
                LinkedCaseIds = new int[] { 26 }
            },
            new UniversityEvent
            {
                Id = "dec_genzozh",
                Title = "Межвузовская конференция «GEN-ЗОЖ: Здоровый Образ Жизни»",
                Month = 12, ApproxDay = 5,
                Category = EventCategory.Health,
                Description = "Конференция о здоровом образе жизни студентов. Доклады, мастер-классы, спортивные секции.",
                Department = "Кафедра физвоспитания, Профком",
                LinkedCaseIds = new int[] { 27 }
            },

            // ===== ФЕВРАЛЬ =====
            new UniversityEvent
            {
                Id = "feb_psych_lab",
                Title = "Занятие в психологической лаборатории «Управление стрессом»",
                Month = 2, ApproxDay = 10,
                Category = EventCategory.Psychology,
                Description = "Практическое занятие: диагностика стресса, дыхательные техники, арт-терапия.",
                Department = "Психологическая служба МУИВ",
                LinkedCaseIds = new int[] { 28 }
            },

            // ===== МАРТ =====
            new UniversityEvent
            {
                Id = "mar_profession_week",
                Title = "Неделя профессий: мастер-классы от работодателей",
                Month = 3, ApproxDay = 15,
                Category = EventCategory.Career,
                Description = "Представители Сбер, Яндекс, 1С проводят мастер-классы. Ярмарка вакансий и стажировок.",
                Department = "Центр карьеры МУИВ",
                LinkedCaseIds = new int[] { 29 }
            },
            new UniversityEvent
            {
                Id = "mar_malyshev",
                Title = "XXII Малышевские чтения «Человек и новая эпоха»",
                Month = 3, ApproxDay = 26,
                Category = EventCategory.Science,
                Description = "Научная конференция по гуманитарным дисциплинам: философия, психология, педагогика.",
                Department = "Научный отдел",
                LinkedCaseIds = new int[] { 25 }
            },

            // ===== АПРЕЛЬ =====
            new UniversityEvent
            {
                Id = "apr_witte_congress",
                Title = "XXVI Международный конгресс молодой науки «Виттевские чтения — 2026»",
                Month = 4, ApproxDay = 28,
                Category = EventCategory.Science,
                Description = "Главное научное событие года. Студенты со всей России и из-за рубежа представляют исследования.",
                Department = "Научный отдел, Ректорат",
                LinkedCaseIds = new int[] { 30 }
            },
            new UniversityEvent
            {
                Id = "apr_esg_case",
                Title = "ESG кейс-чемпионат 2026 (призовой фонд 200 000₽)",
                Month = 4, ApproxDay = 15,
                Category = EventCategory.Professional,
                Description = "Командный чемпионат по решению бизнес-кейсов в области устойчивого развития.",
                Department = "Кафедра менеджмента",
                LinkedCaseIds = new int[] { 29 }
            },

            // ===== МАЙ =====
            new UniversityEvent
            {
                Id = "may_victory",
                Title = "Акция «Верни Герою имя» / День Победы",
                Month = 5, ApproxDay = 9,
                Category = EventCategory.Patriotic,
                Description = "Патриотическая акция: уход за мемориалами, встречи с ветеранами, концерт.",
                Department = "Студенческий совет, Воспитательный отдел",
                LinkedCaseIds = new int[] { 22 }
            },

            // ===== ИЮНЬ =====
            new UniversityEvent
            {
                Id = "jun_rector_cup",
                Title = "Кубок Ректора по футболу и волейболу",
                Month = 6, ApproxDay = 1,
                Category = EventCategory.Sport,
                Description = "Межфакультетский спортивный турнир. Команды от каждого направления.",
                Department = "Кафедра физвоспитания",
                LinkedCaseIds = new int[] { 27 }
            }
        };
    }

    // =============================================
    //  ЗАПРОСЫ К РЕЕСТРУ
    // =============================================

    /// <summary>
    /// Мероприятия текущего месяца
    /// </summary>
    public static List<UniversityEvent> GetCurrentMonthEvents()
    {
        int month = DateTime.Now.Month;
        return GetAllEvents().Where(e => e.Month == month).ToList();
    }

    /// <summary>
    /// Ближайшие N мероприятий от текущей даты
    /// </summary>
    public static List<UniversityEvent> GetUpcomingEvents(int count = 3)
    {
        int currentMonth = DateTime.Now.Month;
        int currentDay = DateTime.Now.Day;

        return GetAllEvents()
            .OrderBy(e => {
                int diff = e.Month - currentMonth;
                if (diff < 0) diff += 12; // следующий учебный год
                return diff * 31 + e.ApproxDay;
            })
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Мероприятия по категории
    /// </summary>
    public static List<UniversityEvent> GetEventsByCategory(EventCategory cat)
    {
        return GetAllEvents().Where(e => e.Category == cat).ToList();
    }

    /// <summary>
    /// Найти мероприятие по ID
    /// </summary>
    public static UniversityEvent GetEventById(string id)
    {
        return GetAllEvents().Find(e => e.Id == id);
    }

    /// <summary>
    /// Получить связанные адаптационные кейсы для мероприятия
    /// </summary>
    public static List<AdaptationCase> GetLinkedCases(UniversityEvent evt)
    {
        if (evt.LinkedCaseIds == null) return new List<AdaptationCase>();
        return evt.LinkedCaseIds
            .Select(id => AdaptationScenariosManager.GetCaseById(id))
            .Where(c => c != null)
            .ToList();
    }

    /// <summary>
    /// Генерирует контекстную подсказку для промпта на основе ближайших мероприятий
    /// </summary>
    public static string GetEventsContextForPrompt()
    {
        var upcoming = GetUpcomingEvents(2);
        if (upcoming.Count == 0) return "";

        string context = "\nБЛИЖАЙШИЕ МЕРОПРИЯТИЯ МУИВ (упоминай естественно в диалоге):";
        foreach (var evt in upcoming)
        {
            context += $"\n• {evt.Title} ({GetMonthName(evt.Month)}) — {evt.Description}";
        }
        return context;
    }

    private static string GetMonthName(int month)
    {
        string[] months = { "", "январь", "февраль", "март", "апрель", "май", "июнь",
                            "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь" };
        return month >= 1 && month <= 12 ? months[month] : "?";
    }
}

// =============================================
//  СТРУКТУРЫ ДАННЫХ
// =============================================

public enum EventCategory
{
    Adaptation,    // Адаптационные
    Culture,       // Культурно-массовые
    Curatorship,   // Кураторские
    Social,        // Социальные / волонтёрские
    Science,       // Научные конференции
    Professional,  // Деловые игры, кейсы
    Health,        // ЗОЖ
    Psychology,    // Психологическая служба
    Career,        // Карьера / стажировки
    Patriotic,     // Патриотические
    Sport          // Спортивные
}

[System.Serializable]
public class UniversityEvent
{
    public string Id;
    public string Title;
    public int Month;               // 1-12
    public int ApproxDay;            // примерная дата
    public EventCategory Category;
    public string Description;
    public string Department;        // Ответственное подразделение
    public int[] LinkedCaseIds;      // Ссылки на адаптационные кейсы
}
