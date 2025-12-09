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

    [Header("Footstep Settings")]
    [Range(0.1f, 1f)] public float stepInterval = 0.25f;  // delay between footsteps
    public float minWalkPitch = 0.95f;
    public float maxWalkPitch = 1.05f;

    private float _stepCooldownTimer = 0f;

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

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        if (_stepCooldownTimer > 0)
            _stepCooldownTimer -= Time.deltaTime;
    }

    private void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    // 👣 Footstep with cooldown + random pitch
    public void PlayWalk()
    {
        if (_stepCooldownTimer > 0) return;

        float randomPitch = Random.Range(minWalkPitch, maxWalkPitch);
        PlaySFX(walkClip, 0.6f, randomPitch);

        _stepCooldownTimer = stepInterval;
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
