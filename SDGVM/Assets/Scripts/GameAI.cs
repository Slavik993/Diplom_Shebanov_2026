using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LLMUnity;
using System.Collections;
using System.IO;
using System;

public class GameAI : MonoBehaviour
{
    [Header("LLM NPC / Текст")]
    public LLMCharacter llmCharacter;

    [Header("==== INPUT LEFT PANEL ====")]
    public TMP_InputField inputPrompt;
    public TMP_InputField inputLength;
    public TMP_Dropdown dropdownStyle;
    public TMP_Dropdown dropdownType;
    public TMP_Dropdown dropdownDifficulty;
    public TMP_InputField inputIconStyle;
    public TMP_InputField inputIconSize;
    public TMP_Dropdown dropdownNPCEmotion;
    public TMP_Dropdown dropdownNPCRelation;

    [Header("==== TEXT OUTPUT CENTER ====")]
    public TMP_Text textStoryOutput;

    [Header("==== NPC / PLAYER PANEL ====")]
    public TMP_Text npcText;
    public TMP_InputField playerInput;

    [Header("==== IMAGE OUTPUT ====")]
    public RawImage iconPreview;

    [Header("==== BUTTONS ====")]
    public Button btnGenerate;
    public Button btnSaveAll;

    [Header("==== IMAGE GENERATION ====")]
    public ComfyUIManager comfy;

    // 🆕 СЕССИОННОЕ СОХРАНЕНИЕ
    [Header("==== AUTO SAVE SETTINGS ====")]
    public string saveFolderRoot = "QuestSessions";
    public bool autoSaveAfterGeneration = true;
    
    private string currentSessionFolder;
    private int generationCounter = 0;

    void Start()
    {
        CreateSessionFolder();
        
        btnGenerate.onClick.AddListener(GenerateAll);
        btnSaveAll.onClick.AddListener(SaveAll);
    }

    // 🆕 Создание уникальной папки для сессии
    void CreateSessionFolder()
    {
        string sessionName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        currentSessionFolder = Path.Combine(Application.dataPath, saveFolderRoot, sessionName);
        
        try
        {
            Directory.CreateDirectory(currentSessionFolder);
            Debug.Log($"📁 Session folder created: {currentSessionFolder}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to create session folder: {e.Message}");
        }
    }

    // ===============================================================
    // ************** FULL GENERATION PIPELINE  ***********************
    // ===============================================================
    public void GenerateAll()
    {
        generationCounter++;
        StartCoroutine(GenerateAllSequence());
    }

    IEnumerator GenerateAllSequence()
    {
        // Генерируем историю/квест
        yield return StartCoroutine(GenerateStoryCoroutine());
        
        // Ждем немного
        yield return new WaitForSeconds(0.5f);
        
        // Генерируем NPC диалог
        yield return StartCoroutine(GenerateNPCCoroutine());
        
        // Ждем немного
        yield return new WaitForSeconds(0.5f);
        
        // Генерируем иконку
        yield return StartCoroutine(GenerateIconCoroutine());
        
        // 🆕 Автосохранение если включено
        if (autoSaveAfterGeneration)
        {
            SaveCurrentGeneration();
        }
    }

    // ===============================================================
    // **************  STORY TEXT  ***********************************
    // ===============================================================
    public void GenerateStory()
    {
        StartCoroutine(GenerateStoryCoroutine());
    }

    IEnumerator GenerateStoryCoroutine()
    {
        if (!llmCharacter) yield break;

        string prompt = $@"Ты — гениальный русскоязычный геймдизайнер. 
            ОТВЕЧАЙ ТОЛЬКО НА РУССКОМ ЯЗЫКЕ, без английских слов.
            Создай квест на тему: {inputPrompt.text}
            Длина: {inputLength.text} слов
            Стиль: {dropdownStyle.captionText.text}
            Тип: {dropdownType.captionText.text}
            Сложность: {dropdownDifficulty.captionText.text}
            Выведи только текст квеста, без кавычек и пояснений.";

        textStoryOutput.text = "Генерация текста...";
        
        bool done = false;
        llmCharacter.Chat(prompt, (result) => 
        {
            textStoryOutput.text = result;
            done = true;
        });
        
        yield return new WaitUntil(() => done);
    }

    // ===============================================================
    // **************  NPC BEHAVIOR  *********************************
    // ===============================================================
    public void GenerateNPC()
    {
        StartCoroutine(GenerateNPCCoroutine());
    }

    IEnumerator GenerateNPCCoroutine()
    {
        if (!llmCharacter) yield break;

        string npcPrompt = $@"Ты — NPC в русской фэнтези-игре.
            ОТВЕЧАЙ ТОЛЬКО НА РУССКОМ, живым языком, коротко.
            Эмоция: {dropdownNPCEmotion.captionText.text}
            Отношение к игроку: {dropdownNPCRelation.captionText.text}
            Сейчас игрок сказал: ""{playerInput.text}""
            Твоя реплика:";

        npcText.text = "...";
        
        bool done = false;
        llmCharacter.Chat(npcPrompt, (reply) => 
        {
            npcText.text = reply;
            done = true;
        });
        
        yield return new WaitUntil(() => done);
    }

    // ===============================================================
    // **************  IMAGE GENERATION *******************************
    // ===============================================================
    public void GenerateIcon()
    {
        StartCoroutine(GenerateIconCoroutine());
    }

    IEnumerator GenerateIconCoroutine()
    {
        string prompt = $"Awesome RPG icon of a {inputPrompt.text}, game asset, sharp, centered, transparent background";

        bool done = false;
        Texture2D resultTex = null;
        
        yield return comfy.GenerateTexture(prompt, (tex) =>
        {
            resultTex = tex;
            iconPreview.texture = tex;
            done = true;
        });
        
        yield return new WaitUntil(() => done);
    }

    // ===============================================================
    // **************  SAVE CURRENT GENERATION ************************
    // ===============================================================
    void SaveCurrentGeneration()
    {
        try
        {
            // Создаем подпапку для конкретной генерации
            string genFolder = Path.Combine(currentSessionFolder, $"generation_{generationCounter:D3}");
            Directory.CreateDirectory(genFolder);

            // Сохраняем параметры генерации
            SaveGenerationParams(genFolder);
            
            // Сохраняем историю/квест
            if (!string.IsNullOrEmpty(textStoryOutput.text) && 
                textStoryOutput.text != "Генерация текста...")
            {
                File.WriteAllText(
                    Path.Combine(genFolder, "quest.txt"), 
                    textStoryOutput.text
                );
            }

            // Сохраняем NPC диалог
            if (!string.IsNullOrEmpty(npcText.text) && npcText.text != "...")
            {
                File.WriteAllText(
                    Path.Combine(genFolder, "npc_dialog.txt"), 
                    npcText.text
                );
            }

            // Сохраняем иконку
            if (iconPreview.texture is Texture2D tex)
            {
                byte[] pngData = tex.EncodeToPNG();
                File.WriteAllBytes(
                    Path.Combine(genFolder, "icon.png"), 
                    pngData
                );
            }

            Debug.Log($"💾 Generation #{generationCounter} saved to: {genFolder}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save generation: {e.Message}");
        }
    }

    // 🆕 Сохраняем параметры генерации в отдельный файл
    void SaveGenerationParams(string folder)
    {
        string paramsFile = Path.Combine(folder, "_parameters.txt");
        
        string parameters = $@"=== GENERATION PARAMETERS ===
Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

[STORY/QUEST]
Prompt: {inputPrompt.text}
Length: {inputLength.text} words
Style: {dropdownStyle.captionText.text}
Type: {dropdownType.captionText.text}
Difficulty: {dropdownDifficulty.captionText.text}

[NPC]
Emotion: {dropdownNPCEmotion.captionText.text}
Relation: {dropdownNPCRelation.captionText.text}

[ICON]
Style: {inputIconStyle.text}
Size: {inputIconSize.text}
";
        
        File.WriteAllText(paramsFile, parameters);
    }

    // ===============================================================
    // **************  SAVE ALL (MANUAL)  *****************************
    // ===============================================================
    public void SaveAll()
    {
        SaveCurrentGeneration();
        
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    // 🆕 Открыть папку текущей сессии
    public void OpenSessionFolder()
    {
        if (Directory.Exists(currentSessionFolder))
        {
            Application.OpenURL("file://" + currentSessionFolder);
            Debug.Log($"📂 Opening: {currentSessionFolder}");
        }
    }

    // 🆕 Получить путь к папке сессии
    public string GetSessionFolder()
    {
        return currentSessionFolder;
    }

    void OnApplicationQuit()
    {
        Debug.Log($"📊 Session complete! Generated {generationCounter} quests");
        Debug.Log($"📁 Saved to: {currentSessionFolder}");
    }
}