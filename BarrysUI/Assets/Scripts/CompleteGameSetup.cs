using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CompleteGameSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool setupOnStart = true;
    public bool createUI = true;
    public bool createPrefabs = true;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupCompleteGame();
        }
    }
    
    private void SetupCompleteGame()
    {
        Debug.Log("Starting complete game setup...");
        
        CreateGameManager();
        CreateAudioSystem();
        CreatePlayer();
        CreateSpawner();
        CreatePrefabs();
        
        if (createUI)
        {
            CreateCompleteUI();
        }
        
        Debug.Log("Complete game setup finished! Ready to play.");
    }
    
    private void CreateGameManager()
    {
        GameObject gm = new GameObject("GameManager");
        var gameManager = gm.AddComponent<GameManager>();
        
        // Настройка параметров
        gameManager.isGameActive = false;
        gameManager.coins = 0;
        gameManager.distance = 0;
        gameManager.gameSpeed = 5f;
        gameManager.totalCoins = 1000;
        gameManager.equippedJetPack = 0;
        gameManager.equippedCostume = 0;
        
        DontDestroyOnLoad(gm);
        Debug.Log("GameManager created and configured");
    }
    
    private void CreateAudioSystem()
    {
        GameObject audio = new GameObject("AudioSystem");
        var audioSystem = audio.AddComponent<AudioSystem>();
        
        // Создаем Audio Sources
        var musicSource = audio.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0.5f;
        
        var sfxSource = audio.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = 0.7f;
        
        // Настраиваем ссылки в AudioSystem через рефлексию
        var audioSystemType = typeof(AudioSystem);
        var musicField = audioSystemType.GetField("musicSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var sfxField = audioSystemType.GetField("sfxSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (musicField != null) musicField.SetValue(audioSystem, musicSource);
        if (sfxField != null) sfxField.SetValue(audioSystem, sfxSource);
        
        DontDestroyOnLoad(audio);
        Debug.Log("AudioSystem created and configured");
    }
    
    private void CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = Vector3.zero;
        
        // SpriteRenderer
        var spriteRenderer = player.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreatePlayerSprite();
        spriteRenderer.sortingOrder = 2;
        
        // Rigidbody2D
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
        
        // Collider
        var collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 0.8f);
        
        // PlayerController
        var playerController = player.AddComponent<PlayerController>();
        playerController.flyForce = 5f;
        playerController.maxVelocity = 10f;
        playerController.gravity = -9.81f;
        playerController.currentJetPackIndex = 0;
        
        // Создаем JetPacks
        var jetPacks = CreateJetPacks(player);
        var jetParticles = CreateJetParticles(player);
        
        // Настраиваем PlayerController
        playerController.jetPacks = jetPacks;
        playerController.jetParticle = jetParticles;
        
        Debug.Log("Player created and configured");
    }
    
    private GameObject[] CreateJetPacks(GameObject player)
    {
        GameObject jetPacksContainer = new GameObject("JetPacks");
        jetPacksContainer.transform.SetParent(player.transform);
        
        GameObject[] jetPacks = new GameObject[3];
        
        for (int i = 0; i < 3; i++)
        {
            GameObject jetPack = new GameObject($"JetPack_{i}");
            jetPack.transform.SetParent(jetPacksContainer.transform);
            jetPack.transform.localPosition = new Vector3(-0.3f, 0, 0);
            
            // SpriteRenderer
            var spriteRenderer = jetPack.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateJetPackSprite(i);
            spriteRenderer.sortingOrder = 1;
            
            // JetPack компонент
            var jetPackComponent = jetPack.AddComponent<JetPack>();
            jetPackComponent.jetPackName = GetJetPackName(i);
            jetPackComponent.price = GetJetPackPrice(i);
            jetPackComponent.flyForce = GetJetPackForce(i);
            jetPackComponent.description = GetJetPackDescription(i);
            jetPackComponent.jetPackColor = GetJetPackColor(i);
            
            jetPack.SetActive(i == 0); // Активен только первый
            jetPacks[i] = jetPack;
        }
        
        return jetPacks;
    }
    
    private ParticleSystem CreateJetParticles(GameObject player)
    {
        GameObject particleSystem = new GameObject("JetParticles");
        particleSystem.transform.SetParent(player.transform);
        particleSystem.transform.localPosition = new Vector3(-0.3f, 0, 0);
        
        var ps = particleSystem.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startColor = Color.orange;
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
        
        return ps;
    }
    
    private void CreateSpawner()
    {
        GameObject spawner = new GameObject("Spawner");
        var spawnerScript = spawner.AddComponent<Spawner>();
        
        spawnerScript.spawnInterval = 2f;
        spawnerScript.minSpawnY = -3f;
        spawnerScript.maxSpawnY = 3f;
        spawnerScript.spawnX = 10f;
        spawnerScript.difficultyIncreaseRate = 0.1f;
        spawnerScript.minSpawnInterval = 0.5f;
        
        // Создаем и настраиваем префабы
        if (createPrefabs)
        {
            spawnerScript.obstacles = CreateObstaclePrefabs();
            spawnerScript.coins = CreateCoinPrefabs();
        }
        
        Debug.Log("Spawner created and configured");
    }
    
    private GameObject[] CreateObstaclePrefabs()
    {
        GameObject[] obstacles = new GameObject[3];
        
        // Static Obstacle
        obstacles[0] = CreateObstaclePrefab("StaticObstacle", Color.red, Obstacle.ObstacleType.Static);
        
        // Moving Obstacle
        obstacles[1] = CreateObstaclePrefab("MovingObstacle", Color.yellow, Obstacle.ObstacleType.Moving);
        
        // Rotating Obstacle
        obstacles[2] = CreateObstaclePrefab("RotatingObstacle", Color.magenta, Obstacle.ObstacleType.Rotating);
        
        return obstacles;
    }
    
    private GameObject CreateObstaclePrefab(string name, Color color, Obstacle.ObstacleType type)
    {
        GameObject obstacle = new GameObject(name);
        obstacle.tag = "Obstacle";
        obstacle.SetActive(false);
        
        // SpriteRenderer
        var spriteRenderer = obstacle.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateObstacleSprite(type);
        spriteRenderer.color = color;
        
        // Collider
        var collider = obstacle.AddComponent<BoxCollider2D>();
        if (type == Obstacle.ObstacleType.Rotating)
        {
            collider.size = new Vector2(2f, 0.2f);
        }
        else
        {
            collider.size = new Vector2(1f, 2f);
        }
        
        // Obstacle script
        var obstacleScript = obstacle.AddComponent<Obstacle>();
        obstacleScript.type = type;
        obstacleScript.speed = 5f;
        obstacleScript.destroyX = -10f;
        
        if (type == Obstacle.ObstacleType.Moving)
        {
            obstacleScript.moveSpeed = 2f;
            obstacleScript.moveRange = 2f;
        }
        else if (type == Obstacle.ObstacleType.Rotating)
        {
            obstacleScript.moveSpeed = 2f;
        }
        
        return obstacle;
    }
    
    private GameObject[] CreateCoinPrefabs()
    {
        GameObject[] coins = new GameObject[1];
        coins[0] = CreateCoinPrefab();
        return coins;
    }
    
    private GameObject CreateCoinPrefab()
    {
        GameObject coin = new GameObject("Coin");
        coin.tag = "Coin";
        coin.SetActive(false);
        
        // SpriteRenderer
        var spriteRenderer = coin.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCoinSprite();
        spriteRenderer.sortingOrder = 3;
        
        // Collider
        var collider = coin.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;
        collider.isTrigger = true;
        
        // Coin script
        var coinScript = coin.AddComponent<Coin>();
        coinScript.speed = 5f;
        coinScript.destroyX = -10f;
        coinScript.rotationSpeed = 180f;
        coinScript.floatAmplitude = 0.5f;
        coinScript.floatFrequency = 2f;
        
        return coin;
    }
    
    private void CreateCompleteUI()
    {
        CreateCanvas();
        CreateEventSystem();
        CreateMainMenuUI();
        CreateGameUI();
        CreateShopUI();
        
        Debug.Log("Complete UI created");
    }
    
    private void CreateCanvas()
    {
        GameObject canvas = new GameObject("Canvas");
        var canvasComponent = canvas.AddComponent<Canvas>();
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
    }
    
    private void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
    
    private void CreateMainMenuUI()
    {
        var canvas = FindObjectOfType<Canvas>();
        GameObject mainMenuUI = new GameObject("MainMenuUI");
        mainMenuUI.transform.SetParent(canvas.transform, false);
        
        var mainMenuScript = mainMenuUI.AddComponent<MainMenuUI>();
        
        // Создаем базовые UI элементы
        CreateUIPanel("MainMenuPanel", mainMenuUI);
        CreateUIPanel("SettingsPanel", mainMenuUI);
        
        Debug.Log("MainMenuUI created");
    }
    
    private void CreateGameUI()
    {
        var canvas = FindObjectOfType<Canvas>();
        GameObject gameUI = new GameObject("GameUI");
        gameUI.transform.SetParent(canvas.transform, false);
        gameUI.SetActive(false);
        
        var gameUIScript = gameUI.AddComponent<GameUI>();
        
        // Создаем базовые UI элементы
        CreateUIPanel("GameHUD", gameUI);
        CreateUIPanel("GameOverPanel", gameUI);
        CreateUIPanel("PausePanel", gameUI);
        
        Debug.Log("GameUI created");
    }
    
    private void CreateShopUI()
    {
        var canvas = FindObjectOfType<Canvas>();
        GameObject shopUI = new GameObject("ShopUI");
        shopUI.transform.SetParent(canvas.transform, false);
        shopUI.SetActive(false);
        
        var shopManager = shopUI.AddComponent<ShopManager>();
        
        // Создаем базовые UI элементы
        CreateUIPanel("JetPackShopPanel", shopUI);
        CreateUIPanel("CostumeShopPanel", shopUI);
        
        Debug.Log("ShopUI created");
    }
    
    private void CreateUIPanel(string name, GameObject parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);
        
        var rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        var image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);
    }
    
    // Спрайты
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
    
    private Sprite CreateCoinSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                if (distance <= 12f)
                {
                    pixels[y * 32 + x] = Color.yellow;
                }
                else if (distance <= 14f)
                {
                    pixels[y * 32 + x] = new Color(1f, 0.8f, 0f);
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
    
    private Sprite CreateObstacleSprite(Obstacle.ObstacleType type)
    {
        if (type == Obstacle.ObstacleType.Rotating)
        {
            Texture2D texture = new Texture2D(64, 8);
            Color[] pixels = new Color[64 * 8];
            
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    if (y >= 2 && y <= 6)
                    {
                        pixels[y * 64 + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * 64 + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 64, 8), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Texture2D texture = new Texture2D(32, 64);
            Color[] pixels = new Color[32 * 64];
            
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (x >= 10 && x <= 22)
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
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0.5f));
        }
    }
    
    // Вспомогательные методы
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
