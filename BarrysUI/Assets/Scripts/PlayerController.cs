using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float flyForce = 5f;
    public float maxVelocity = 10f;
    public float gravity = -9.81f;
    
    [Header("JetPack Settings")]
    public GameObject[] jetPacks;
    public int currentJetPackIndex = 0;
    public ParticleSystem jetParticle;
    
    private Rigidbody2D rb;
    private bool isFlying = false;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        EquipJetPack(currentJetPackIndex);
    }
    
    void Update()
    {
        HandleInput();
        UpdateAnimation();
    }
    
    void FixedUpdate()
    {
        ApplyGravity();
        ClampVelocity();
    }
    
    private void HandleInput()
    {
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            isFlying = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, flyForce);
            
            if (jetParticle != null && !jetParticle.isPlaying)
            {
                jetParticle.Play();
            }
        }
        else
        {
            isFlying = false;
            if (jetParticle != null && jetParticle.isPlaying)
            {
                jetParticle.Stop();
            }
        }
    }
    
    private void ApplyGravity()
    {
        if (!isFlying)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + gravity * Time.fixedDeltaTime);
        }
    }
    
    private void ClampVelocity()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -maxVelocity, maxVelocity));
    }
    
    private void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsFlying", isFlying);
            animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        }
    }
    
    public void EquipJetPack(int index)
    {
        if (index >= 0 && index < jetPacks.Length)
        {
            foreach (var jetPack in jetPacks)
            {
                jetPack.SetActive(false);
            }
            
            jetPacks[index].SetActive(true);
            currentJetPackIndex = index;
            
            var jetPackComponent = jetPacks[index].GetComponent<JetPack>();
            if (jetPackComponent != null)
            {
                flyForce = jetPackComponent.flyForce;
                if (jetParticle != null)
                {
                    jetParticle.GetComponent<ParticleSystemRenderer>().material = jetPackComponent.particleMaterial;
                }
            }
        }
    }
    
    public void ChangeCostume(Sprite newCostume)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = newCostume;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            GameManager.Instance.GameOver();
        }
    }
}
