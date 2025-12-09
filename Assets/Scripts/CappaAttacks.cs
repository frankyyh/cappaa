using System.Collections;
using UnityEngine;

public class CappaAttacks : MonoBehaviour
{
    [Header("Underwater Control")]
    [SerializeField] private bool underwater = false;
    
    [Header("Hands Attack")]
    [SerializeField] private GameObject handsIndicator;
    [SerializeField] private float _handsAttackIndicatorDelay;
    [SerializeField] private Collider2D handCollider1;
    [SerializeField] private Collider2D handCollider2;
    
    [Header("Jump Attack")]
    [SerializeField] private GameObject jumpIndicator;
    [SerializeField] private float _jumpAttackIndicatorDelay;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float jumpDuration = 1f;
    
    [Header("Attack Timing")]
    [SerializeField] private float minAttackDelay = 8f;
    [SerializeField] private float maxAttackDelay = 10f;
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Cappa cappaScript;
    
    private float _attackTimer;
    private bool isAttacking = false;
    private bool wasScaredDuringJump = false;
    private Vector3 originalPosition;
    private float jumpStartY;
    
    void Start()
    {
        originalPosition = transform.position;
        ResetAttackTimer();
        
        // Find references if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        if (cappaScript == null)
        {
            cappaScript = GetComponent<Cappa>();
        }
        
        // Disable colliders initially
        if (handCollider1 != null) handCollider1.enabled = false;
        if (handCollider2 != null) handCollider2.enabled = false;
    }

    void Update()
    {
        // Only attack when underwater
        if (!underwater || isAttacking) return;
        
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            // Pick an attack at random
            if (Random.value < 0.5f)
                HandsAttack();
            else
                JumpAttack();

            ResetAttackTimer();
        }
    }
    
    void ResetAttackTimer()
    {
        _attackTimer = Random.Range(minAttackDelay, maxAttackDelay);
    }
    
    void HandsAttack()
    {
        StartCoroutine(HandAttackCoroutine());
    }
    
    IEnumerator HandAttackCoroutine()
    {
        isAttacking = true;
        
        // Show indicator
        if (handsIndicator != null)
        {
            handsIndicator.SetActive(true);
        }
        
        yield return new WaitForSeconds(_handsAttackIndicatorDelay);
        
        // Hide indicator and raise hands (enable colliders)
        if (handsIndicator != null)
        {
            handsIndicator.SetActive(false);
        }
        
        // Activate hand collider GameObjects and enable colliders
        if (handCollider1 != null)
        {
            // Activate the GameObject first
            handCollider1.gameObject.SetActive(true);
            // Then enable the collider component
            handCollider1.enabled = true;
        }
        
        if (handCollider2 != null)
        {
            // Activate the GameObject first
            handCollider2.gameObject.SetActive(true);
            // Then enable the collider component
            handCollider2.enabled = true;
        }
        
        // Check for player collision while colliders are active
        float checkDuration = 0.5f; // How long to check for collision
        float elapsed = 0f;
        bool playerHit = false;
        
        while (elapsed < checkDuration && !playerHit)
        {
            elapsed += Time.deltaTime;
            
            // Check if player is colliding with either hand collider
            if (player != null)
            {
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    if (handCollider1 != null && handCollider1.IsTouching(playerCollider))
                    {
                        playerHit = true;
                    }
                    else if (handCollider2 != null && handCollider2.IsTouching(playerCollider))
                    {
                        playerHit = true;
                    }
                }
            }
            
            if (playerHit)
            {
                // Player dies
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.Die();
                }
                break;
            }
            
            yield return null;
        }
        
        // Deactivate hand collider GameObjects after attack
        if (handCollider1 != null)
        {
            handCollider1.enabled = false;
            handCollider1.gameObject.SetActive(false);
        }
        
        if (handCollider2 != null)
        {
            handCollider2.enabled = false;
            handCollider2.gameObject.SetActive(false);
        }
        
        isAttacking = false;
    }
    
    void JumpAttack()
    {
        StartCoroutine(JumpAttackCoroutine());
    }
    
    IEnumerator JumpAttackCoroutine()
    {
        isAttacking = true;
        wasScaredDuringJump = false;
        
        // Reset scared state at the start of each jump attack
        if (cappaScript != null)
        {
            cappaScript.ResetScaredState();
        }
        
        // Show indicator
        if (jumpIndicator != null)
        {
            jumpIndicator.SetActive(true);
        }
        
        yield return new WaitForSeconds(_jumpAttackIndicatorDelay);
        
        // Hide indicator
        if (jumpIndicator != null)
        {
            jumpIndicator.SetActive(false);
        }
        
        // Cappa jumps up (using transform, not rigidbody)
        // Store only Y position to allow horizontal movement
        jumpStartY = transform.position.y;
        float jumpUpTime = 0.5f; // Time to reach peak
        
        // Phase 1: Jump up to highest point (no scare check during jump)
        float elapsedTime = 0f;
        while (elapsedTime < jumpUpTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / jumpUpTime;
            
            // Smooth jump up animation
            float yOffset = Mathf.Lerp(0f, jumpHeight, progress);
            
            // Update only Y position, preserve X and Z (for horizontal movement)
            Vector3 currentPos = transform.position;
            transform.position = new Vector3(currentPos.x, jumpStartY + yOffset, currentPos.z);
            
            yield return null;
        }
        
        // Ensure we're at the peak
        Vector3 peakPos = transform.position;
        transform.position = new Vector3(peakPos.x, jumpStartY + jumpHeight, peakPos.z);
        
        // Phase 2: Stay at peak for 1 second, check if player scares
        float waitTime = 1f;
        elapsedTime = 0f;
        
        while (elapsedTime < waitTime)
        {
            elapsedTime += Time.deltaTime;
            
            // Check if cappa was scared during wait
            if (cappaScript != null && cappaScript.IsScared())
            {
                wasScaredDuringJump = true;
                break;
            }
            
            yield return null;
        }
        
        // Phase 3: Handle outcome
        if (wasScaredDuringJump)
        {
            // Player scared cappa - fall back quickly
            float fallTime = 0.3f;
            float fallElapsed = 0f;
            float currentY = transform.position.y;
            
            while (fallElapsed < fallTime)
            {
                fallElapsed += Time.deltaTime;
                float fallProgress = fallElapsed / fallTime;
                float yOffset = Mathf.Lerp(jumpHeight, 0f, fallProgress);
                
                Vector3 currentPos = transform.position;
                transform.position = new Vector3(currentPos.x, jumpStartY + yOffset, currentPos.z);
                
                yield return null;
            }
            
            // Ensure we're back at original position
            Vector3 finalPos = transform.position;
            transform.position = new Vector3(finalPos.x, jumpStartY, finalPos.z);
        }
        else
        {
            // Player didn't scare - check if player is in scare range and kill them
            if (player != null && cappaScript != null)
            {
                // Check if player is in scare range
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                float scareRange = cappaScript.GetScareRange();
                
                if (distanceToPlayer <= scareRange)
                {
                    // Player is in scare range and didn't scare cappa - player dies
                    PlayerController playerController = player.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        playerController.Die();
                    }
                }
            }
            
            // Fall back down
            float fallTime = jumpUpTime;
            float fallElapsed = 0f;
            
            while (fallElapsed < fallTime)
            {
                fallElapsed += Time.deltaTime;
                float fallProgress = fallElapsed / fallTime;
                float yOffset = Mathf.Lerp(jumpHeight, 0f, fallProgress);
                
                Vector3 currentPos = transform.position;
                transform.position = new Vector3(currentPos.x, jumpStartY + yOffset, currentPos.z);
                
                yield return null;
            }
            
            // Ensure we're back at original position
            Vector3 finalPos = transform.position;
            transform.position = new Vector3(finalPos.x, jumpStartY, finalPos.z);
        }
        
        // Reset scared state after jump attack completes (for next attack)
        if (cappaScript != null)
        {
            cappaScript.ResetScaredState();
        }
        
        isAttacking = false;
    }
    
    // Public method to set underwater state
    public void SetUnderwater(bool value)
    {
        bool wasUnderwater = underwater;
        underwater = value;
        
        // When going underwater, reset attack state and scared state
        if (value && !wasUnderwater)
        {
            // Reset attack state
            isAttacking = false;
            wasScaredDuringJump = false;
            
            // Reset attack timer to start fresh
            ResetAttackTimer();
            
            // Reset Cappa's scared state when going underwater
            if (cappaScript != null)
            {
                cappaScript.ResetScaredState();
            }
            
            Debug.Log("Cappa went underwater - attack system initialized");
        }
    }
    
    // Public method to check if underwater
    public bool IsUnderwater()
    {
        return underwater;
    }
    
}
