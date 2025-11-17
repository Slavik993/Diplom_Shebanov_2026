using UnityEngine;
  // Sentis 2.1.3 (не InferenceEngine!)

public class NPCController : MonoBehaviour
{/*
    [Header("Модель ONNX")]
    public Unity.InferenceEngine.ModelAsset model;  // Перетащи .onnx

    private Unity.InferenceEngine.Worker worker;  // Worker (не IWorker)

    void Start()
    {
        if (model == null)
        {
            Debug.LogError("[NPC] Модель ONNX не назначена!");
            return;
        }

        try
        {
            var runtimeModel = Unity.InferenceEngine.ModelLoader.Load(model);
            worker = new Unity.InferenceEngine.Worker(runtimeModel, Unity.InferenceEngine.BackendType.CPU);  // CPU для локальной машины
            Debug.Log("[NPC] Worker создан успешно.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NPC] Ошибка: {e.Message}");
        }
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
*/
    public string GetReaction(float rep, float threat)
    {
/*        if (worker == null) return "NPC не готов";

        try
        {
            // ← ИСПРАВЛЕНО: Tensor<float> (generic, по docs 2.1.3)
            using var input = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, 2), new[] { rep, threat });

            // Schedule (асинхронно)
            worker.Schedule(input);

            // ← ИСПРАВЛЕНО: PeekOutput + ReadbackAndClone (синхронно для CPU)
            using var output = worker.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
            if (output == null) return "Ошибка вывода";

            using var cpuOutput = output.ReadbackAndClone();  // Копирует на CPU (блокирующий для простоты)
            float value = cpuOutput[0];  // Scalar

            return value > 0.7f ? "Привет!" :
                   value > 0.3f ? "Хммм..." : "Уходи!";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NPC] Ошибка инференса: {e.Message}");
            return "Ошибка расчёта";
        }
    }

    [ContextMenu("Тест реакции")]
    public void TestReaction()
    {
        string reaction = GetReaction(0.8f, 0.2f);
        Debug.Log($"[NPC Тест] Реакция: {reaction}");
*/    return "test::heya";}
}