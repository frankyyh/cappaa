using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("SFX Clips")]
    public AudioClip walkClip;
    public AudioClip jumpClip;
    public AudioClip scareClip;
    public AudioClip deathClip;

    [Header("Kap SFX")]
    public AudioClip kapJump;
    public AudioClip kapScared;
    public AudioClip kapHand;
    public AudioClip kapIdle;              // main idle
    public AudioClip[] kapIdleAltClips;    // alternate idle variations
    public AudioClip kapEnterWater;

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Footstep Settings")]
    [Range(0.1f, 1f)] public float stepInterval = 0.25f;  // delay between footsteps
    public float minWalkPitch = 0.95f;
    public float maxWalkPitch = 1.05f;

    [Header("Kap Idle Settings")]
    [Range(0.1f, 10f)] public float kapIdleCooldown = 3f; // seconds between idle sounds

    private float _stepCooldownTimer = 0f;
    private float _kapIdleCooldownTimer = 0f;

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
        if (_stepCooldownTimer > 0f)
            _stepCooldownTimer -= Time.deltaTime;

        if (_kapIdleCooldownTimer > 0f)
            _kapIdleCooldownTimer -= Time.deltaTime;
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
        if (_stepCooldownTimer > 0f) return;

        float randomPitch = Random.Range(minWalkPitch, maxWalkPitch);
        PlaySFX(walkClip, 0.6f, randomPitch);

        _stepCooldownTimer = stepInterval;
    }

    // Core SFX
    public void PlayJump()
    {
        PlaySFX(jumpClip, 1f, 1f);
    }

    public void PlayScare()
    {
        PlaySFX(scareClip, 1f, 1f);
    }

    public void PlayDeath()
    {
        PlaySFX(deathClip, 1f, 1f);
    }

    // --- Kap SFX ---

    public void PlayKapJump()
    {
        PlaySFX(kapJump, 1f, 1f);
    }

    public void PlayKapScared()
    {
        PlaySFX(kapScared, 1f, 1f);
    }

    public void PlayKapHand()
    {
        PlaySFX(kapHand, 1f, 1f);
    }

    public void PlayKapEnterWater()
    {
        PlaySFX(kapEnterWater, 1f, 1f);
    }

    public void PlayKapIdle()
    {
        // Cooldown: do nothing if still waiting
        if (_kapIdleCooldownTimer > 0f) return;

        // Choose which idle clip to play
        AudioClip chosen = kapIdle;

        int baseCount = string.IsNullOrEmpty(kapIdle?.name) ? 0 : 1;
        int altCount = (kapIdleAltClips != null) ? kapIdleAltClips.Length : 0;
        int total = baseCount + altCount;

        if (total == 0)
        {
            // no idle clips assigned
            return;
        }

        int index = Random.Range(0, total);
        if (baseCount == 1 && index == 0)
        {
            chosen = kapIdle;
        }
        else
        {
            int altIndex = index - baseCount;
            chosen = kapIdleAltClips[altIndex];
        }

        // Slightly softer idle
        PlaySFX(chosen, 0.7f, 1f);

        // Reset cooldown
        _kapIdleCooldownTimer = kapIdleCooldown;
    }
}
