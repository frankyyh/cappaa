using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("SFX Clips")]
    public AudioClip walkClip;
    public AudioClip jumpClip;
    public AudioClip scareClip;

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Footstep Pitch Range")]
    [Range(0.5f, 2f)] public float minWalkPitch = 0.95f;
    [Range(0.5f, 2f)] public float maxWalkPitch = 1.05f;

    private void Awake()
    {
        // Simple Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    // 🎧 Public Calls
    public void PlayWalk()
    {
        float randomPitch = Random.Range(minWalkPitch, maxWalkPitch);
        PlaySFX(walkClip, 0.6f, randomPitch);
    }

    public void PlayJump()
    {
        PlaySFX(jumpClip, 1f, 1f);
    }

    public void PlayScare()
    {
        PlaySFX(scareClip, 1f, 1f);
    }
}
