using System.Threading.Tasks;
using UnityEngine;

public class LLMQuestGenerator : MonoBehaviour
{
    // Здесь ты можешь хранить ссылку на свой контроллер или LLM API
    public LLMPrototypeController controller;

    // Простой пример генерации (заглушка)
    public async Task<string> GenerateQuest(string prompt)
    {
        Debug.Log($"⚙️ Генерация квеста запущена с промптом: {prompt}");

        // Имитация запроса — можно заменить на реальный вызов OpenAI / LLM
        await Task.Delay(2000);

        string result = $"Сгенерированный квест:\n{prompt}\n— Завершено успешно!";
        Debug.Log("Квест сгенерирован!");
        return result;
    }
}
