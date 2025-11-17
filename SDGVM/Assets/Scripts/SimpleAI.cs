using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LLMUnity;

public class SimpleAI : MonoBehaviour
{
    public LLMCharacter llm;

    public TMP_InputField inputPrompt;
    public TMP_Text outputText;
    public Button sendButton;

    void Start()
    {
        if (sendButton)
            sendButton.onClick.AddListener(Send);
    }

    public void Send()
    {
        if (llm == null || string.IsNullOrEmpty(inputPrompt.text)) return;

        outputText.text = "⏳ Генерация...";

        llm.Chat(inputPrompt.text, OnLLMComplete);
    }

    void OnLLMComplete(string result)
    {
        outputText.text = result;
        Debug.Log("LLM ответ: " + result);
    }

    public void NPCSayHello()
    {
        llm.Chat("Ты — торговец. Поприветствуй героя.", s => Debug.Log("NPC: " + s));
    }
}
