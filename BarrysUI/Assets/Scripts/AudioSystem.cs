using UnityEngine;

public class AudioSystem : MonoBehaviour
{
    public static AudioSystem Instance { get; private set; }
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    
    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip coinSound;
    public AudioClip purchaseSound;
    public AudioClip equipSound;
    public AudioClip jetPackSound;
    public AudioClip gameOverSound;
    public AudioClip buttonClickSound;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        PlayBackgroundMusic();
    }
    
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }
    
    public void StopBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
    
    public void PlayCoinSound()
    {
        PlaySFX(coinSound);
    }
    
    public void PlayPurchaseSound()
    {
        PlaySFX(purchaseSound);
    }
    
    public void PlayEquipSound()
    {
        PlaySFX(equipSound);
    }
    
    public void PlayJetPackSound()
    {
        PlaySFX(jetPackSound);
    }
    
    public void PlayGameOverSound()
    {
        PlaySFX(gameOverSound);
    }
    
    public void PlayButtonClickSound()
    {
        PlaySFX(buttonClickSound);
    }
    
    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
        }
    }
}
