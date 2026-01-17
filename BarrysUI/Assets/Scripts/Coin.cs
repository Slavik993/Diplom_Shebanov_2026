using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float destroyX = -10f;
    
    [Header("Animation Settings")]
    public float rotationSpeed = 180f;
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 2f;
    
    private Vector3 startPosition;
    private float timeOffset;
    
    public void Initialize(float gameSpeed)
    {
        speed = gameSpeed;
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        MoveCoin();
        AnimateCoin();
        CheckDestroy();
    }
    
    private void MoveCoin()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
    }
    
    private void AnimateCoin()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency + timeOffset) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
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
            GameManager.Instance.AddCoin();
            Destroy(gameObject);
            
            AudioSystem.Instance.PlayCoinSound();
        }
    }
}
