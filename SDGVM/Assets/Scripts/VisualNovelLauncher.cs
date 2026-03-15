using UnityEngine;
using LLMUnity;

/// <summary>
/// Точка входа для визуальной новеллы.
/// Добавьте этот компонент на пустой GameObject в новой сцене (VisualNovel.unity).
/// Он автоматически создаёт все необходимые компоненты и строит сцену.
/// </summary>
public class VisualNovelLauncher : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("ID адаптационного кейса (2, 6, 11, 15, 20-31)")]
    public int caseId = 31;
    [Tooltip("Принудительно генерировать заново (игнорировать файлы в StreamingAssets)")]
    public bool forceRegenerate = false;

    [Header("LLM (опционально)")]
    [Tooltip("Перетащите LLMCharacter для AI-генерации. Без него — fallback из DialogueTree.")]
    public LLMCharacter llmCharacter;

    void Start()
    {
        Debug.Log($"[VNLauncher] Запуск визуальной новеллы для кейса {caseId}");

        // Создаём все необходимые компоненты
        var generator = gameObject.AddComponent<VisualNovelGenerator>();
        if (llmCharacter != null)
            generator.llmCharacter = llmCharacter;

        var sceneBuilder = gameObject.AddComponent<VisualNovelSceneBuilder>();

        var player = gameObject.AddComponent<VisualNovelPlayer>();
        player.sceneBuilder = sceneBuilder;
        player.generator = generator;
        player.caseId = caseId;
        player.forceRegenerate = forceRegenerate;

        // Убедимся, что AdaptationScenariosManager существует
        if (AdaptationScenariosManager.Instance == null)
        {
            var asmObj = new GameObject("AdaptationScenariosManager");
            asmObj.AddComponent<AdaptationScenariosManager>();
        }

        // Убедимся, что ComfyUIManager существует на сцене (чтобы визуалы генерировались по умолчанию)
        var comfyManager = FindObjectOfType<ComfyUIManager>();
        if (comfyManager == null)
        {
            var comfyObj = new GameObject("ComfyUIManager");
            comfyManager = comfyObj.AddComponent<ComfyUIManager>();
            
            // Если мы создаём его из кода, сервер мог не успеть запуститься к моменту генерации первой сцены, 
            // так как он стартует асинхронно в OnEnable/Start. Но так как процесс генерации ИИ тоже длинный (LLM), 
            // ComfyUI (если включен autoStartServer) обычно успевает запуститься до того как понадобятся картинки.
            Debug.Log("[VNLauncher] Компонент ComfyUIManager автоматически добавлен на сцену.");
        }
        generator.comfyUI = comfyManager;

        Debug.Log("[VNLauncher] Все компоненты созданы. Ожидание ввода студента.");
    }
}
