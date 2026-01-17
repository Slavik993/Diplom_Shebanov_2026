using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-1000)]
public class AutoSceneSetup : MonoBehaviour
{
    [Header("Auto Setup on Start")]
    public bool setupOnStart = true;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupCompleteScene();
        }
    }
    
    private void SetupCompleteScene()
    {
        Debug.Log("Setting up complete JetPack Joyride scene...");
        
        CreateGameManager();
        CreateAudioSystem();
        CreatePlayer();
        CreateSpawner();
        CreateCanvas();
        CreateUI();
        CreatePrefabs();
        
        Debug.Log("Scene setup complete! Ready to play.");
    }
    
    private void CreateGameManager()
    {
        if (FindObjectOfType<GameManager>() == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
            DontDestroyOnLoad(gm);
        }
    }
    
    private void CreateAudioSystem()
    {
        if (FindObjectOfType<AudioSystem>() == null)
        {
            GameObject audio = new GameObject("AudioSystem");
            var audioSystem = audio.AddComponent<AudioSystem>();
            
            var musicSource = audio.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            
            var sfxSource = audio.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            
            DontDestroyOnLoad(audio);
        }
    }
    
    private void CreatePlayer()
    {
        if (FindObjectOfType<PlayerController>() == null)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = Vector3.zero;
            
            var spriteRenderer = player.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreatePlayerSprite();
            spriteRenderer.sortingOrder = 2;
            
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.freezeRotation = true;
            rb.linearVelocity = Vector2.zero;
            
            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);
            
            player.AddComponent<PlayerController>();
            
            CreateJetPacks(player);
            CreateJetParticles(player);
        }
    }
    
    private void CreateJetPacks(GameObject player)
    {
        GameObject jetPacksContainer = new GameObject("JetPacks");
        jetPacksContainer.transform.SetParent(player.transform);
        
        for (int i = 0; i < 3; i++)
        {
            GameObject jetPack = new GameObject($"JetPack_{i}");
            jetPack.transform.SetParent(jetPacksContainer.transform);
            jetPack.transform.localPosition = new Vector3(-0.3f, 0, 0);
            
            var spriteRenderer = jetPack.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateJetPackSprite(i);
            spriteRenderer.sortingOrder = 1;
            
            var jetPackComponent = jetPack.AddComponent<JetPack>();
            jetPackComponent.jetPackName = GetJetPackName(i);
            jetPackComponent.price = GetJetPackPrice(i);
            jetPackComponent.flyForce = GetJetPackForce(i);
            jetPackComponent.description = GetJetPackDescription(i);
            
            jetPack.SetActive(i == 0);
        }
    }
    
    private void CreateJetParticles(GameObject player)
    {
        GameObject particleSystem = new GameObject("JetParticles");
        particleSystem.transform.SetParent(player.transform);
        particleSystem.transform.localPosition = new Vector3(-0.3f, 0, 0);
        
        var ps = particleSystem.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startColor = GetJetPackColor(0);
        main.startSize = 0.15f;
        main.startSpeed = 3f;
        main.startLifetime = 0.3f;
        
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 5)
        });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.rotation = new Vector3(0, 0, 180f);
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
    }
    
    private void CreateSpawner()
    {
        if (FindObjectOfType<Spawner>() == null)
        {
            GameObject spawner = new GameObject("Spawner");
            spawner.AddComponent<Spawner>();
        }
    }
    
    private void CreateCanvas()
    {
        if (FindObjectOfType<Canvas>() == null)
        {
            GameObject canvas = new GameObject("Canvas");
            var canvasComponent = canvas.AddComponent<Canvas>();
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
            
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
    
    private void CreateUI()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            CreateMainMenuUI(canvas);
            CreateGameUI(canvas);
            CreateShopUI(canvas);
        }
    }
    
    private void CreateMainMenuUI(Canvas canvas)
    {
        GameObject mainMenu = new GameObject("MainMenuUI");
        mainMenu.transform.SetParent(canvas.transform);
        mainMenu.AddComponent<MainMenuUI>();
        
        CreateMainMenuPanels(mainMenu);
    }
    
    private void CreateGameUI(Canvas canvas)
    {
        GameObject gameUI = new GameObject("GameUI");
        gameUI.transform.SetParent(canvas.transform);
        gameUI.AddComponent<GameUI>();
        gameUI.SetActive(false);
        
        CreateGameUIPanels(gameUI);
    }
    
    private void CreateShopUI(Canvas canvas)
    {
        GameObject shopUI = new GameObject("ShopUI");
        shopUI.transform.SetParent(canvas.transform);
        shopUI.AddComponent<ShopManager>();
        shopUI.SetActive(false);
        
        CreateShopUIPanels(shopUI);
    }
    
    private void CreateMainMenuPanels(GameObject parent)
    {
        GameObject panel = CreateUIPanel("MainMenuPanel", parent);
        panel.SetActive(true);
    }
    
    private void CreateGameUIPanels(GameObject parent)
    {
        GameObject hud = CreateUIPanel("GameHUD", parent);
        hud.SetActive(true);
        
        GameObject gameOver = CreateUIPanel("GameOverPanel", parent);
        gameOver.SetActive(false);
        
        GameObject pause = CreateUIPanel("PausePanel", parent);
        pause.SetActive(false);
    }
    
    private void CreateShopUIPanels(GameObject parent)
    {
        GameObject jetPackPanel = CreateUIPanel("JetPackShopPanel", parent);
        jetPackPanel.SetActive(true);
        
        GameObject costumePanel = CreateUIPanel("CostumeShopPanel", parent);
        costumePanel.SetActive(false);
    }
    
    private GameObject CreateUIPanel(string name, GameObject parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform);
        
        var rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        var image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0, 0, 0, 0.8f);
        
        return panel;
    }
    
    private void CreatePrefabs()
    {
        if (FindObjectOfType<PrefabsCreator>() == null)
        {
            GameObject prefabsCreator = new GameObject("PrefabsCreator");
            prefabsCreator.AddComponent<PrefabsCreator>();
        }
    }
    
    private Sprite CreatePlayerSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                if (x >= 20 && x <= 44 && y >= 15 && y <= 49)
                {
                    pixels[y * 64 + x] = new Color(0.2f, 0.6f, 1f);
                }
                else if (x >= 24 && x <= 40 && y >= 10 && y <= 20)
                {
                    pixels[y * 64 + x] = new Color(0.8f, 0.9f, 1f);
                }
                else
                {
                    pixels[y * 64 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
    
    private Sprite CreateJetPackSprite(int index)
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        Color jetPackColor = GetJetPackColor(index);
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (x >= 8 && x <= 24 && y >= 8 && y <= 24)
                {
                    pixels[y * 32 + x] = jetPackColor;
                }
                else if (x >= 10 && x <= 22 && y >= 10 && y <= 22)
                {
                    pixels[y * 32 + x] = Color.white;
                }
                else
                {
                    pixels[y * 32 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    private Color GetJetPackColor(int index)
    {
        return index switch
        {
            0 => Color.gray,
            1 => Color.red,
            2 => Color.cyan,
            _ => Color.gray
        };
    }
    
    private string GetJetPackName(int index)
    {
        return index switch
        {
            0 => "Basic JetPack",
            1 => "Advanced JetPack",
            2 => "Pro JetPack",
            _ => "Basic JetPack"
        };
    }
    
    private int GetJetPackPrice(int index)
    {
        return index switch
        {
            0 => 0,
            1 => 100,
            2 => 200,
            _ => 0
        };
    }
    
    private float GetJetPackForce(int index)
    {
        return index switch
        {
            0 => 5f,
            1 => 5.5f,
            2 => 6f,
            _ => 5f
        };
    }
    
    private string GetJetPackDescription(int index)
    {
        return index switch
        {
            0 => "Standard jetpack for beginners",
            1 => "Improved jetpack with better performance",
            2 => "Professional jetpack with maximum power",
            _ => "Standard jetpack"
        };
    }
}
