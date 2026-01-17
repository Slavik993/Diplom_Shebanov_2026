using UnityEngine;

public class JetPack : MonoBehaviour
{
    [Header("JetPack Settings")]
    public string jetPackName = "Basic JetPack";
    public int price = 100;
    public float flyForce = 5f;
    public Material particleMaterial;
    public string description = "Standard jetpack";
    
    [Header("Visual Settings")]
    public Color jetPackColor = Color.gray;
    public ParticleSystem customParticleEffect;
    
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = jetPackColor;
        }
    }
    
    public string GetJetPackName()
    {
        return jetPackName;
    }
    
    public int GetPrice()
    {
        return price;
    }
    
    public float GetFlyForce()
    {
        return flyForce;
    }
    
    public string GetDescription()
    {
        return description;
    }
    
    public void SetJetPackColor(Color color)
    {
        jetPackColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
    
    public ParticleSystem GetCustomParticleEffect()
    {
        return customParticleEffect;
    }
}
