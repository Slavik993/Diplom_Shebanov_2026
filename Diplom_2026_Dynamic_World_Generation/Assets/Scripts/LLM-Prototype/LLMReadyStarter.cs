using UnityEngine;
using System.Threading.Tasks;

public class LLMReadyStarter : MonoBehaviour
{
    public LLMPrototypeController controller;

    private async void Start()
    {
        if (controller == null)
        {
            Debug.LogError("LLMReadyStarter: controller не назначен.");
            return;
        }

        Debug.Log("LLMReadyStarter: ожидаю запуска LLM...");
        await Task.Delay(4000); // ждём несколько секунд, пока поднимется сервер
        controller.ProcessJsonInput(controller.testInputJson);
    }
}
