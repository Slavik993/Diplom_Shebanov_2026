using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] obstacles;
    public GameObject[] coins;
    public float spawnInterval = 2f;
    public float minSpawnY = -3f;
    public float maxSpawnY = 3f;
    public float spawnX = 10f;
    
    [Header("Difficulty Settings")]
    public float difficultyIncreaseRate = 0.1f;
    public float minSpawnInterval = 0.5f;
    
    private bool isSpawning = false;
    private float currentSpawnInterval;
    
    void Start()
    {
        currentSpawnInterval = spawnInterval;
    }
    
    public void StartSpawning()
    {
        isSpawning = true;
        StartCoroutine(SpawnRoutine());
    }
    
    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }
    
    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            SpawnObject();
            
            yield return new WaitForSeconds(currentSpawnInterval);
            
            IncreaseDifficulty();
        }
    }
    
    private void SpawnObject()
    {
        int randomChoice = Random.Range(0, 100);
        
        if (randomChoice < 60)
        {
            SpawnObstacle();
        }
        else
        {
            SpawnCoin();
        }
    }
    
    private void SpawnObstacle()
    {
        if (obstacles.Length > 0)
        {
            GameObject obstaclePrefab = obstacles[Random.Range(0, obstacles.Length)];
            Vector3 spawnPosition = new Vector3(spawnX, Random.Range(minSpawnY, maxSpawnY), 0);
            
            GameObject obstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
            Obstacle obstacleScript = obstacle.GetComponent<Obstacle>();
            
            if (obstacleScript != null)
            {
                obstacleScript.Initialize(GameManager.Instance.gameSpeed);
            }
        }
    }
    
    private void SpawnCoin()
    {
        if (coins.Length > 0)
        {
            GameObject coinPrefab = coins[Random.Range(0, coins.Length)];
            Vector3 spawnPosition = new Vector3(spawnX, Random.Range(minSpawnY, maxSpawnY), 0);
            
            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);
            Coin coinScript = coin.GetComponent<Coin>();
            
            if (coinScript != null)
            {
                coinScript.Initialize(GameManager.Instance.gameSpeed);
            }
        }
    }
    
    private void IncreaseDifficulty()
    {
        if (currentSpawnInterval > minSpawnInterval)
        {
            currentSpawnInterval -= difficultyIncreaseRate * Time.deltaTime;
            currentSpawnInterval = Mathf.Max(currentSpawnInterval, minSpawnInterval);
        }
        
        GameManager.Instance.gameSpeed += difficultyIncreaseRate * Time.deltaTime;
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(spawnX, 0, 0), 0.5f);
        
        Gizmos.color = Color.green;
        Vector3 topPoint = new Vector3(spawnX, maxSpawnY, 0);
        Vector3 bottomPoint = new Vector3(spawnX, minSpawnY, 0);
        Gizmos.DrawLine(topPoint, bottomPoint);
    }
}
