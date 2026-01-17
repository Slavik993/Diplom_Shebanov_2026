using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-1000)]
public class SceneSetup : MonoBehaviour
{
    [Header("Required Prefabs")]
    public GameObject playerPrefab;
    public GameObject coinPrefab;
    public GameObject obstaclePrefab;
    public GameObject spawnerPrefab;
    
    [Header("UI Prefabs")]
    public GameObject mainMenuUIPrefab;
    public GameObject gameUIPrefab;
    public GameObject shopUIPrefab;
    
    void Awake()
    {
        CreateGameManager();
        CreateAudioSystem();
        CreatePlayer();
        CreateSpawner();
        CreateUI();
    }
    
    private void CreateGameManager()
    {
        GameObject gameManagerObj = new GameObject("GameManager");
        gameManagerObj.AddComponent<GameManager>();
    }
    
    private void CreateAudioSystem()
    {
        GameObject audioSystemObj = new GameObject("AudioSystem");
        audioSystemObj.AddComponent<AudioSystem>();
        
        var audioSource = audioSystemObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        var sfxSource = audioSystemObj.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }
    
    private void CreatePlayer()
    {
        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            player.name = "Player";
            player.tag = "Player";
            
            if (player.GetComponent<Rigidbody2D>() == null)
            {
                var rb = player.AddComponent<Rigidbody2D>();
                rb.gravityScale = 1f;
                rb.freezeRotation = true;
            }
            
            if (player.GetComponent<Collider2D>() == null)
            {
                var collider = player.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1f, 1f);
            }
            
            if (player.GetComponent<PlayerController>() == null)
            {
                player.AddComponent<PlayerController>();
            }
        }
        else
        {
            CreateDefaultPlayer();
        }
    }
    
    private void CreateDefaultPlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        
        var spriteRenderer = player.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreatePlayerSprite();
        spriteRenderer.color = Color.white;
        
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        
        var collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 1f);
        
        player.AddComponent<PlayerController>();
        
        CreateJetPacks(player);
        CreateJetParticles(player);
    }
    
    private void CreateJetPacks(GameObject player)
    {
        GameObject jetPacksContainer = new GameObject("JetPacks");
        jetPacksContainer.transform.SetParent(player.transform);
        
        for (int i = 0; i < 3; i++)
        {
            GameObject jetPack = new GameObject($"JetPack_{i}");
            jetPack.transform.SetParent(jetPacksContainer.transform);
            jetPack.transform.localPosition = new Vector3(-0.5f, 0, 0);
            
            var spriteRenderer = jetPack.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateJetPackSprite(i);
            spriteRenderer.sortingOrder = -1;
            
            var jetPackComponent = jetPack.AddComponent<JetPack>();
            jetPackComponent.jetPackName = $"JetPack {i}";
            jetPackComponent.price = i * 100 + 100;
            jetPackComponent.flyForce = 5f + i * 0.5f;
            jetPackComponent.description = $"Standard jetpack level {i + 1}";
            
            jetPack.SetActive(i == 0);
        }
    }
    
    private void CreateJetParticles(GameObject player)
    {
        GameObject particleSystem = new GameObject("JetParticles");
        particleSystem.transform.SetParent(player.transform);
        particleSystem.transform.localPosition = new Vector3(-0.5f, 0, 0);
        
        var ps = particleSystem.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = Color.orange;
        main.startSize = 0.2f;
        main.startSpeed = 5f;
        main.startLifetime = 0.5f;
        
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.rotation = new Vector3(0, 0, 180f);
    }
    
    private void CreateSpawner()
    {
        if (spawnerPrefab != null)
        {
            GameObject spawner = Instantiate(spawnerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            spawner.name = "Spawner";
        }
        else
        {
            CreateDefaultSpawner();
        }
    }
    
    private void CreateDefaultSpawner()
    {
        GameObject spawner = new GameObject("Spawner");
        spawner.AddComponent<Spawner>();
    }
    
    private void CreateUI()
    {
        CreateCanvas();
        CreateMainMenuUI();
        CreateGameUI();
        CreateShopUI();
    }
    
    private void CreateCanvas()
    {
        GameObject canvas = new GameObject("Canvas");
        canvas.AddComponent<Canvas>();
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        
        var canvasComponent = canvas.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
    }
    
    private void CreateMainMenuUI()
    {
        GameObject mainMenuUI = new GameObject("MainMenuUI");
        mainMenuUI.transform.SetParent(FindObjectOfType<Canvas>().transform);
        mainMenuUI.AddComponent<MainMenuUI>();
    }
    
    private void CreateGameUI()
    {
        GameObject gameUI = new GameObject("GameUI");
        gameUI.transform.SetParent(FindObjectOfType<Canvas>().transform);
        gameUI.AddComponent<GameUI>();
    }
    
    private void CreateShopUI()
    {
        GameObject shopUI = new GameObject("ShopUI");
        shopUI.transform.SetParent(FindObjectOfType<Canvas>().transform);
        shopUI.AddComponent<ShopManager>();
    }
    
    private Sprite CreatePlayerSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                if (x >= 20 && x <= 44 && y >= 10 && y <= 54)
                {
                    pixels[y * 64 + x] = Color.blue;
                }
                else if (x >= 24 && x <= 40 && y >= 5 && y <= 15)
                {
                    pixels[y * 64 + x] = Color.blue;
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
        
        Color jetPackColor = index switch
        {
            0 => Color.gray,
            1 => Color.red,
            2 => Color.cyan,
            _ => Color.gray
        };
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (x >= 5 && x <= 25 && y >= 10 && y <= 22)
                {
                    pixels[y * 32 + x] = jetPackColor;
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
}
