using UnityEngine;
using Unity.Sentis; // Обязательно для IWorker

public class SentisImageGenerator : MonoBehaviour
{
    [SerializeField] private ModelAsset modelAsset; // .sentis в Inspector
    private Model model;
    private IWorker worker; // Строка 8 — теперь компилируется

    void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("Назначьте ModelAsset (.sentis) в Inspector!");
            return;
        }

        model = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(BackendType.Auto, model); // Auto: GPU/CPU
        Debug.Log("IWorker создан: " + (worker != null ? "Да" : "Нет")); // Проверка в Console
    }

    public Tensor<float> Run(Tensor<float> input)
    {
        if (worker == null || input == null) 
        {
            Debug.LogError("Worker или input null!");
            return null;
        }

        worker.Execute(input);
        return worker.PeekOutput() as Tensor<float>; // Без prepareCacheForAccess (1.4+)
    }

    void OnDestroy()
    {
        worker?.Dispose();
        model?.Dispose(); // Явная очистка в 1.4+
    }
}