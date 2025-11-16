using UnityEngine;
using System.Reflection;
using System.Threading.Tasks;

public class LLMServiceAwaiter : MonoBehaviour
{
    public LLMPrototypeController controller;
    public int timeoutMs = 15000; // подгони при необходимости

    private async void Start()
    {
        if (controller == null)
        {
            Debug.LogError("LLMServiceAwaiter: controller не назначен.");
            return;
        }

        Debug.Log("LLMServiceAwaiter: ожидаю готовности LLM...");
        bool ready = await WaitForLLMService(timeoutMs);

        if (ready)
        {
            Debug.Log("LLMServiceAwaiter: LLM готов — запускаю обработку.");
            // Запускаем тестовую обработку
            controller.ProcessJsonInput(controller.testInputJson);
            // Или controller.LoadAndProcessFile();
        }
        else
        {
            Debug.LogWarning("LLMServiceAwaiter: таймаут ожидания LLM-сервиса.");
        }
    }

    private async Task<bool> WaitForLLMService(int timeout)
    {
        int waited = 0;
        const int step = 250;
        while (waited < timeout)
        {
            if (IsAnyLLMInstanceReady()) return true;
            await Task.Delay(step);
            waited += step;
        }
        return false;
    }

    // Ищем все MonoBehaviour и проверяем типы с именем "LLM" (LLMUnity.LLM)
    // Если находим любой экземпляр, читаем все булевы поля/свойства — если хоть одно true, считаем сервис готовым.
    private bool IsAnyLLMInstanceReady()
    {
        var monos = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        foreach (var m in monos)
        {
            var t = m.GetType();
            if (t.Name != "LLM") continue; // фильтр по имени класса плагина
            // свойства
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var p in props)
            {
                if (p.PropertyType == typeof(bool))
                {
                    try { if ((bool)p.GetValue(m)) return true; } catch { }
                }
            }
            // поля
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(bool))
                {
                    try { if ((bool)f.GetValue(m)) return true; } catch { }
                }
            }
        }
        return false;
    }
}
