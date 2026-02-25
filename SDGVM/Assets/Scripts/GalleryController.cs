using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Управляет Галереей сгенерированных историй и иконок.
/// Загружает сохраненные результаты из QuestSessions.
/// </summary>
public class GalleryController : MonoBehaviour
{
    [Header("UI Префабы и Контейнеры")]
    public GameObject galleryItemPrefab; // Префаб карточки галереи
    public Transform gridContainer;      // GridLayoutGroup для карточек
    
    [Header("Детальный просмотр")]
    public GameObject detailPanel;
    public Image detailIcon;
    public TMP_Text detailStoryText;
    public TMP_Text detailChatText;
    public Button btnCloseDetail;

    public void Awake()
    {
        if (btnCloseDetail) btnCloseDetail.onClick.AddListener(CloseDetailPanel);
        if (detailPanel) detailPanel.SetActive(false);
    }

    /// <summary>
    /// Очищает и заново загружает элементы галереи из файлов
    /// </summary>
    public void RefreshGallery()
    {
        // Очистка старых элементов
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }

        string sessionsDir = Path.Combine(Application.persistentDataPath, "QuestSessions");
        if (!Directory.Exists(sessionsDir)) return;

        // Ищем все папки gen_XXX
        var genFolders = Directory.GetDirectories(sessionsDir, "gen_*", SearchOption.AllDirectories)
                                  .OrderByDescending(d => d) // Сортировка: новые сверху
                                  .Take(10);                 // Берем последние 10 для скорости

        foreach (string folder in genFolders)
        {
            CreateGalleryItem(folder);
        }
    }

    private void CreateGalleryItem(string folderPath)
    {
        if (galleryItemPrefab == null || gridContainer == null) return;

        string questPath = Path.Combine(folderPath, "quest.txt");
        string chatPath = Path.Combine(folderPath, "chat.txt");
        string iconPath = Path.Combine(folderPath, "icon.png");

        // Проверяем наличие хотя бы истории
        if (!File.Exists(questPath)) return;

        // Создаем карточку
        GameObject item = Instantiate(galleryItemPrefab, gridContainer);
        
        // Значок (может быть пустым)
        Image itemImage = item.GetComponentInChildren<Image>(); // Найти Image компонент
        Texture2D tex = null;
        if (File.Exists(iconPath))
        {
            tex = new Texture2D(2, 2);
            byte[] fileData = File.ReadAllBytes(iconPath);
            tex.LoadImage(fileData);
            if (itemImage != null)
            {
                itemImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        // Текст превью
        TMP_Text itemText = item.GetComponentInChildren<TMP_Text>();
        string fullStory = File.ReadAllText(questPath);
        string shortStory = fullStory.Length > 50 ? fullStory.Substring(0, 50) + "..." : fullStory;
        if (itemText != null)
        {
             itemText.text = shortStory;
        }

        // Привязываем клик к открытию деталки
        string fullChat = File.Exists(chatPath) ? File.ReadAllText(chatPath) : "Нет чата";
        Button btn = item.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => OpenDetailPanel(tex, fullStory, fullChat));
        }
    }

    private void OpenDetailPanel(Texture2D icon, string story, string chat)
    {
        if (detailPanel == null) return;
        
        if (detailIcon != null)
        {
            if (icon != null)
            {
                detailIcon.sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
                detailIcon.gameObject.SetActive(true);
            }
            else
            {
                detailIcon.gameObject.SetActive(false);
            }
        }
        
        if (detailStoryText != null) detailStoryText.text = story;
        if (detailChatText != null) detailChatText.text = chat;
        
        detailPanel.SetActive(true);
    }

    private void CloseDetailPanel()
    {
        if (detailPanel) detailPanel.SetActive(false);
    }
}
