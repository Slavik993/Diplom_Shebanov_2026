using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

[RequireComponent(typeof(CanvasGroup))]
public class StorytellerController : MonoBehaviour
{
    public TMP_InputField themeField;
    public TMP_Dropdown styleDropdown;
    public TMP_Dropdown questTypeDropdown;
    public TMP_InputField lengthField;
    public Button generateButton;
    public TextMeshProUGUI outputText;
    public TextMeshProUGUI progressText;

    // Ссылка на центральный контроллер, который умеет ProcessJsonInput
    public LLMPrototypeController controller;

    void Start()
    {
        if (controller == null) controller = FindObjectOfType<LLMPrototypeController>();
        if (generateButton != null) generateButton.onClick.AddListener(OnGenerateClicked);

        // debug init (если хочешь)
        if (styleDropdown != null && styleDropdown.options.Count == 0)
        {
            styleDropdown.options.Add(new TMP_Dropdown.OptionData("реализм"));
            styleDropdown.options.Add(new TMP_Dropdown.OptionData("фэнтези"));
            styleDropdown.options.Add(new TMP_Dropdown.OptionData("постапокалипсис"));
        }
        if (questTypeDropdown != null && questTypeDropdown.options.Count == 0)
        {
            questTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Диалоговый"));
            questTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Поисковый"));
            questTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Образовательный"));
        }
    }

    void OnGenerateClicked()
    {
        if (controller == null)
        {
            Debug.LogError("LLMPrototypeController не назначен!");
            return;
        }

        string theme = themeField != null ? themeField.text : "";
        string style = styleDropdown != null ? styleDropdown.options[styleDropdown.value].text : "";
        string qtype = questTypeDropdown != null ? questTypeDropdown.options[questTypeDropdown.value].text : "";
        string length = lengthField != null ? lengthField.text : "200";

        string json = $@"{{
    ""storyTheme"": ""{Escape(theme)}"",
    ""storyStyle"": ""{Escape(style)}"",
    ""questType"": ""{Escape(qtype)}"",
    ""length"": ""{Escape(length)}""
}}";

        Debug.Log($"📤 [Storyteller] Отправляю JSON: {json}");
        // Показываем начальный прогресс
        SetProgress(0);
        // Запускаем асинхронно
        _ = RunGeneration(json);
    }

    async Task RunGeneration(string json)
    {
        // Если контроллер поддержует callbacks/progress, можно подключить.
        await controller.ProcessJsonInput(json); // предполагает, что ProcessJsonInput ассинхронно обрабатывает и вызывает UI обновление
        // После возврата контроллера, ожидаем что UI обновлен через controller -> LLMUIBinder.DisplayResult
        SetProgress(100);
    }

    void SetProgress(int p)
    {
        if (progressText != null) progressText.text = $"⏳ Прогресс: {p}%";
    }

    string Escape(string s) => s?.Replace("\"", "\\\"") ?? "";
}
