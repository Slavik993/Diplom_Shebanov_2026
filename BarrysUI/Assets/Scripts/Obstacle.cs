using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float destroyX = -10f;
    
    [Header("Obstacle Type")]
    public ObstacleType type = ObstacleType.Static;
    public float moveSpeed = 2f;
    public float moveRange = 2f;
    
    private Vector3 startPosition;
    private float moveDirection = 1f;
    
    public enum ObstacleType
    {
        Static,
        Moving,
        Rotating
    }
    
    public void Initialize(float gameSpeed)
    {
        speed = gameSpeed;
        startPosition = transform.position;
    }
    
    void Update()
    {
        MoveObstacle();
        CheckDestroy();
        
        if (type == ObstacleType.Moving)
        {
            MoveVertical();
        }
        else if (type == ObstacleType.Rotating)
        {
            RotateObstacle();
        }
    }
    
    private void MoveObstacle()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
    }
    
    private void MoveVertical()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * moveSpeed) * moveRange;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    private void RotateObstacle()
    {
        transform.Rotate(0, 0, moveSpeed * 50 * Time.deltaTime);
    }
    
    private void CheckDestroy()
    {
        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.GameOver();
        }
    }
}
