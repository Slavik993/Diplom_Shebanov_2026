using UnityEngine;
using TMPro;

public class AllInputCounters : MonoBehaviour
{
    [System.Serializable]
    public class CounterPair
    {
        public TMP_InputField input;
        public TMP_Text counter;
        public int maxChars = 500;
    }

    public CounterPair[] counters;

    void Start()
    {
        foreach (var pair in counters)
        {
            if (pair.input != null)
            {
                pair.input.onValueChanged.AddListener((value) => UpdateCounter(pair, value));
                UpdateCounter(pair, pair.input.text);
            }
        }
    }

    void UpdateCounter(CounterPair pair, string value)
    {
        int len = value.Length;
        pair.counter.text = $"{len}/{pair.maxChars}";
        pair.counter.color = len > pair.maxChars ? Color.red : Color.white;
    }
}