using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;  // Для Action
using UnityEngine;
using LLMUnity;

public class QualityTester : MonoBehaviour
{
    public LLMCharacter llm;
    public int testCount = 5;
    public float averageTime = 0f;
    public float successRate = 0f;
    public float bleuScore = 0f;

    [ContextMenu("Тест")]
    public async void RunBatchTest()
    {
        var times = new List<float>();
        int success = 0;
        string reference = "найди меч в пещере";

        for (int i = 0; i < testCount; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            string result = await GetLLMResponseAsync("Сгенерируй квест: найди меч");

            sw.Stop();
            times.Add((float)sw.Elapsed.TotalSeconds);

            if (result.ToLower().Contains("меч")) success++;
        }

        averageTime = times.Average();
        successRate = (float)success / testCount;
        bleuScore = CalculateSimpleBLEU(reference, reference);

        Debug.Log($"[Тест] Время: {averageTime:F2}s | Успех: {successRate:P0} | BLEU: {bleuScore:F2}");
    }

    // Вспомогательный async wrapper для Chat
    private async Task<string> GetLLMResponseAsync(string prompt)
    {
        var tcs = new TaskCompletionSource<string>();
        string response = "";

        await llm.Chat(
            prompt,
            token => { response += token; },
            () => { Debug.LogWarning("TODO QualityTester>> Здесь что-то непонятное, надо разобраться"); }
        );

        return await tcs.Task;  // Ждём завершения
    }

    float CalculateSimpleBLEU(string reference, string candidate)
    {
        int matches = 0;
        string[] refWords = reference.Split(' ');
        string[] candWords = candidate.Split(' ');

        foreach (string word in candWords)
            if (System.Array.IndexOf(refWords, word) >= 0)
                matches++;

        float precision = candWords.Length > 0 ? (float)matches / candWords.Length : 0;
        return Mathf.Clamp01(precision);
    }
}