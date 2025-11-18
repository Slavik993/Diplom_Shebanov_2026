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

    void Start()
    {
        btnGenerate.onClick.AddListener(GenerateAll);
        btnSaveAll.onClick.AddListener(SaveAll);
    }

    // ===============================================================
    // ************** FULL GENERATION PIPELINE  ***********************
    // ===============================================================
    public void GenerateAll()
    {
        GenerateStory();
        GenerateNPC();
        GenerateIcon();
    }

    // ===============================================================
    // **************  STORY TEXT  ***********************************
    // ===============================================================
    public void GenerateStory()
    {
        if (!llmCharacter) return;

        string prompt =
            $"Создай историю. Тема: {inputPrompt.text}. " +
            $"Длина: {inputLength.text} слов. " +
            $"Стиль: {dropdownStyle.captionText.text}. " +
            $"Тип: {dropdownType.captionText.text}. " +
            $"Сложность: {dropdownDifficulty.captionText.text}.";

        textStoryOutput.text = "Генерация текста...";
        llmCharacter.Chat(prompt, (result) => textStoryOutput.text = result);
    }

    // ===============================================================
    // **************  NPC BEHAVIOR  *********************************
    // ===============================================================
    public void GenerateNPC()
    {
        if (!llmCharacter) return;

        string npcPrompt =
            $"Ты NPC c эмоцией {dropdownNPCEmotion.captionText.text}. " +
            $"Отношение к игроку: {dropdownNPCRelation.captionText.text}. " +
            $"Скажи реплику.";

        npcText.text = "...";
        llmCharacter.Chat(npcPrompt, (reply) => npcText.text = reply);
    }

    // ===============================================================
    // **************  IMAGE GENERATION *******************************
    // ===============================================================
    public void GenerateIcon()
    {
        string prompt =
            $"Иконка {inputIconStyle.text}, размер {inputIconSize.text}. " +
            $"В стиле {dropdownStyle.captionText.text}.";

        StartCoroutine(comfy.GenerateTexture(prompt, (tex) =>
        {
            iconPreview.texture = tex;
        }));
    }

    // ===============================================================
    // **************  SAVE EVERYTHING ********************************
    // ===============================================================
    public void SaveAll()
    {
        string folder = Application.dataPath + "/Generated/";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        File.WriteAllText(folder + "story.txt", textStoryOutput.text);
        File.WriteAllText(folder + "npc.txt", npcText.text);

        if (iconPreview.texture is Texture2D tex)
        {
            File.WriteAllBytes(folder + "icon.png", tex.EncodeToPNG());
        }

        Debug.Log("✔ Сохранено в /Assets/Generated/");
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
