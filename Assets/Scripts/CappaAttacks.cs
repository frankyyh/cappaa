using System.Collections;
using UnityEngine;

public class CappaAttacks : MonoBehaviour
{
    [Header("Underwater Control")]
    [SerializeField] private bool underwater = false;
    
    [Header("Hands Attack")]
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
    
    [Header("Animation")]
    [SerializeField] private Animator animator; // Cappa's animator for attack animations
    
    [Header("Scare Settings")]
    [SerializeField] private bool useKeyCode = true; // Use legacy KeyCode or new Input System
    [SerializeField] private KeyCode scareKeyCode = KeyCode.E; // Key to press to scare
    
    private float _attackTimer;
    private bool isAttacking = false;
    private bool wasScaredDuringJump = false;
    private Vector3 originalPosition;
    private float jumpStartY;
    private bool handCollidersActivated = false; // Track if colliders are active
    private bool handCollidersDeactivated = false; // Track if colliders should be deactivated
    
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
        
        // Get animator from Cappa script if not assigned
        if (animator == null && cappaScript != null)
        {
            animator = cappaScript.GetComponent<Animator>();
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
        handCollidersActivated = false;
        handCollidersDeactivated = false;
        
        // Trigger hand attack animation (indicator is handled by animation holding on specific frames)
        TriggerHandAttackAnimation();
        
        // Wait for colliders to be activated by animation event (first frame with hands high)
        while (!handCollidersActivated)
        {
            yield return null;
        }
        
        // Check for player collision while colliders are active
        bool playerHit = false;
        
        while (!handCollidersDeactivated && !playerHit)
        {
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
        
        // Wait for animation to finish before returning to idle
        // The colliders are already deactivated by the animation event
        
        // Return to underwater idle animation after attack
        ReturnToUnderwaterIdle();
        
        isAttacking = false;
    }
    
    // Called by Animation Event at first frame where hands are held high
    public void ActivateHandColliders()
    {
        handCollidersActivated = true;
        
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
        
        Debug.Log("Hand colliders activated by animation event");
    }
    
    // Called by Animation Event at second frame where hands are held high
    public void DeactivateHandColliders()
    {
        handCollidersDeactivated = true;
        
        // Deactivate hand collider GameObjects
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
        
        Debug.Log("Hand colliders deactivated by animation event");
    }
    
    // Trigger hand attack animation
    private void TriggerHandAttackAnimation()
    {
        if (animator == null) return;
        
        // Try trigger parameter first (recommended for one-time animations)
        if (HasParameter("HandAttack"))
        {
            animator.SetTrigger("HandAttack");
        }
        else if (HasParameter("HandsAttack"))
        {
            animator.SetTrigger("HandsAttack");
        }
        else if (HasParameter("Attack"))
        {
            animator.SetTrigger("Attack");
        }
        // Fallback to bool parameter
        else if (HasParameter("IsHandAttacking"))
        {
            animator.SetBool("IsHandAttacking", true);
        }
        
        Debug.Log("Hand attack animation triggered");
    }
    
    // Return to underwater idle animation after attack
    private void ReturnToUnderwaterIdle()
    {
        if (cappaScript != null)
        {
            cappaScript.SetAnimationState("UnderwaterIdle");
        }
        else if (animator != null)
        {
            // Fallback: directly set underwater idle if Cappa script not available
            if (HasParameter("IsHandAttacking"))
            {
                animator.SetBool("IsHandAttacking", false);
            }
            if (HasParameter("IsUnderwaterIdle"))
            {
                animator.SetBool("IsUnderwaterIdle", true);
            }
        }
    }
    
    // Helper method to check if animator has a parameter
    private bool HasParameter(string paramName)
    {
        if (animator == null || animator.parameters == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
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
            
            // Check if player pressed scare key during wait
            bool scareKeyPressed = false;
            if (useKeyCode)
            {
                scareKeyPressed = Input.GetKeyDown(scareKeyCode);
            }
            else
            {
                // Use new Input System
                UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null && cappaScript != null)
                {
                    // Get the scare key from Cappa script
                    UnityEngine.InputSystem.Key scareKey = cappaScript.GetScareKey();
                    scareKeyPressed = keyboard[scareKey].wasPressedThisFrame;
                }
            }
            
            // If player pressed scare key, scare Cappa
            if (scareKeyPressed && cappaScript != null)
            {
                cappaScript.ScareCappaDuringJumpAttack();
                wasScaredDuringJump = true;
                break;
            }
            
            // Also check if cappa was already scared (backup check)
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
            // Player scared cappa - return to underwater position
            Vector2 underwaterPos = Vector2.zero;
            if (cappaScript != null)
            {
                underwaterPos = cappaScript.GetUnderwaterPosition();
            }
            
            // If underwater position not available, use current position as fallback
            if (underwaterPos == Vector2.zero)
            {
                underwaterPos = transform.position;
            }
            
            // Fall back quickly to underwater position
            float fallTime = 0.3f;
            float fallElapsed = 0f;
            Vector2 startPos = transform.position;
            
            while (fallElapsed < fallTime)
            {
                fallElapsed += Time.deltaTime;
                float fallProgress = fallElapsed / fallTime;
                
                // Smoothly move back to underwater position
                Vector2 currentPos = Vector2.Lerp(startPos, underwaterPos, fallProgress);
                transform.position = currentPos;
                
                yield return null;
            }
            
            // Ensure we're back at underwater position
            transform.position = underwaterPos;
            
            Debug.Log("Cappa returned to underwater position after being scared");
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
