using UnityEngine;

public class PrefabsCreator : MonoBehaviour
{
    [Header("Create Prefabs on Start")]
    public bool createPrefabs = true;
    
    void Start()
    {
        if (createPrefabs)
        {
            CreateCoinPrefab();
            CreateObstaclePrefabs();
        }
    }
    
    private void CreateCoinPrefab()
    {
        GameObject coin = new GameObject("Coin");
        coin.tag = "Coin";
        
        var spriteRenderer = coin.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCoinSprite();
        spriteRenderer.sortingOrder = 1;
        
        var collider = coin.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;
        collider.isTrigger = true;
        
        coin.AddComponent<Coin>();
        
        coin.SetActive(false);
    }
    
    private void CreateObstaclePrefabs()
    {
        CreateStaticObstacle();
        CreateMovingObstacle();
        CreateRotatingObstacle();
    }
    
    private void CreateStaticObstacle()
    {
        GameObject obstacle = new GameObject("StaticObstacle");
        obstacle.tag = "Obstacle";
        
        var spriteRenderer = obstacle.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateObstacleSprite();
        spriteRenderer.color = Color.red;
        
        var collider = obstacle.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 2f);
        
        var obstacleScript = obstacle.AddComponent<Obstacle>();
        obstacleScript.type = Obstacle.ObstacleType.Static;
        
        obstacle.SetActive(false);
    }
    
    private void CreateMovingObstacle()
    {
        GameObject obstacle = new GameObject("MovingObstacle");
        obstacle.tag = "Obstacle";
        
        var spriteRenderer = obstacle.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateObstacleSprite();
        spriteRenderer.color = Color.yellow;
        
        var collider = obstacle.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 1f);
        
        var obstacleScript = obstacle.AddComponent<Obstacle>();
        obstacleScript.type = Obstacle.ObstacleType.Moving;
        obstacleScript.moveSpeed = 2f;
        obstacleScript.moveRange = 2f;
        
        obstacle.SetActive(false);
    }
    
    private void CreateRotatingObstacle()
    {
        GameObject obstacle = new GameObject("RotatingObstacle");
        obstacle.tag = "Obstacle";
        
        var spriteRenderer = obstacle.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateRotatingObstacleSprite();
        spriteRenderer.color = Color.magenta;
        
        var collider = obstacle.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2f, 0.2f);
        
        var obstacleScript = obstacle.AddComponent<Obstacle>();
        obstacleScript.type = Obstacle.ObstacleType.Rotating;
        obstacleScript.moveSpeed = 2f;
        
        obstacle.SetActive(false);
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
    
    private Sprite CreateObstacleSprite()
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
    
    private Sprite CreateRotatingObstacleSprite()
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
}
