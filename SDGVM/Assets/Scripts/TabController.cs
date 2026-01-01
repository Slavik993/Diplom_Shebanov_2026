using UnityEngine;

public class TabController : MonoBehaviour
{
    [Header("Панели вкладок")]
    public GameObject storyPanel;
    public GameObject npcPanel;
    public GameObject iconPanel;
    public GameObject previewPanel;

    [Header("Кнопки вкладок")]
    public UnityEngine.UI.Button storyTab;
    public UnityEngine.UI.Button npcTab;
    public UnityEngine.UI.Button iconTab;
    public UnityEngine.UI.Button previewTab;

    void Start()
    {
        // Подключаем кнопки
        storyTab.onClick.AddListener(() => SwitchTab(0));
        npcTab.onClick.AddListener(() => SwitchTab(1));
        iconTab.onClick.AddListener(() => SwitchTab(2));
        previewTab.onClick.AddListener(() => SwitchTab(3));

        // Показываем первую вкладку
        SwitchTab(0);
    }

    public void SwitchTab(int index)
    {
        // Скрываем все панели
        storyPanel.SetActive(false);
        npcPanel.SetActive(false);
        iconPanel.SetActive(false);
        previewPanel.SetActive(false);

        // Показываем нужную
        switch (index)
        {
            case 0: storyPanel.SetActive(true); break;
            case 1: npcPanel.SetActive(true); break;
            case 2: iconPanel.SetActive(true); break;
            case 3: previewPanel.SetActive(true); break;
        }
    }
}