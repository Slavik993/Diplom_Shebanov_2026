using UnityEngine;
#if UNITY_EDITOR
using LLMUnity;
#endif

/// <summary>
/// Точка входа для визуальной новеллы.
/// Добавьте этот компонент на пустой GameObject в новой сцене (VisualNovel.unity).
/// Он автоматически создаёт все необходимые компоненты и строит сцену.
/// 
/// В STANDALONE БИЛДЕ: не создаёт LLM/ComfyUI — только загружает готовые сценарии из StreamingAssets.
/// В EDITOR: полный функционал с генерацией.
/// </summary>
public class VisualNovelLauncher : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("ID адаптационного кейса (2, 6, 11, 15, 20-31)")]
    public int caseId = 31;
    [Tooltip("Принудительно генерировать заново (игнорировать файлы в StreamingAssets)")]
    public bool forceRegenerate = false;

#if UNITY_EDITOR
    [Header("LLM (опционально — только в Editor)")]
    [Tooltip("Перетащите LLMCharacter для AI-генерации. Без него — fallback из DialogueTree.")]
    public LLMCharacter llmCharacter;
#endif

    void Start()
    {
        Debug.Log($"[VNLauncher] Запуск визуальной новеллы для кейса {caseId}");

        var sceneBuilder = gameObject.AddComponent<VisualNovelSceneBuilder>();

        var player = gameObject.AddComponent<VisualNovelPlayer>();
        player.sceneBuilder = sceneBuilder;
        player.caseId = caseId;

        // Убедимся, что AdaptationScenariosManager существует
        if (AdaptationScenariosManager.Instance == null)
        {
            var asmObj = new GameObject("AdaptationScenariosManager");
            asmObj.AddComponent<AdaptationScenariosManager>();
        }

#if UNITY_EDITOR
        // ═══════════════════════════════════════════════════
        // EDITOR: создаём генератор и ComfyUI для AI-генерации
        // ═══════════════════════════════════════════════════
        var generator = gameObject.AddComponent<VisualNovelGenerator>();
        if (llmCharacter != null)
            generator.llmCharacter = llmCharacter;

        player.generator = generator;
        player.forceRegenerate = forceRegenerate;

        var comfyManager = FindObjectOfType<ComfyUIManager>();
        if (comfyManager == null)
        {
            var comfyObj = new GameObject("ComfyUIManager");
            comfyManager = comfyObj.AddComponent<ComfyUIManager>();
            Debug.Log("[VNLauncher] Компонент ComfyUIManager автоматически добавлен на сцену.");
        }
        generator.comfyUI = comfyManager;

        Debug.Log("[VNLauncher] Editor-режим: все компоненты AI созданы. Ожидание ввода студента.");
#else
        // ═══════════════════════════════════════════════════
        // STANDALONE BUILD: без генератора, загружаем готовые файлы
        // ═══════════════════════════════════════════════════
        player.generator = null;
        player.forceRegenerate = false;

        Debug.Log("[VNLauncher] Standalone-режим: загрузка готовых сценариев из StreamingAssets.");
#endif
    }
}
