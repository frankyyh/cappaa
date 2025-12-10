using UnityEngine;
using UnityEngine.InputSystem;

public class Cappa : MonoBehaviour
{
    [Header("Detection Ranges")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float scareRange = 5f;
    
    [Header("Scare Settings")]
    [SerializeField] private Key scareKey = Key.E;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float fallSpeed = 10f;
    [SerializeField] private bool useKeyCode = true;
    [SerializeField] private KeyCode scareKeyCode = KeyCode.E;
    [SerializeField] private float jumpArcHeight = 5f; // Height of the parabolic arc
    [SerializeField] private float jumpDuration = 2f; // Total time for the jump arc
    
    [Header("Movement References")]
    [SerializeField] private Transform horizontalEndPosition; // Where Cappa stops moving right
    [SerializeField] private Transform finalWaterPosition; // Final position in water where Cappa stops
    
    [Header("References")]
    public Transform player;
    [SerializeField] private string waterTag = "Water";
    [SerializeField] private CappaAttacks cappaAttacks;
    
    [Header("Animation")]
    [SerializeField] public Animator animator;
    
    [Header("Splash Effect")]
    [SerializeField] private GameObject splashEffectObject; // Child object with splash animation (recommended)
    [SerializeField] private GameObject splashPrefab; // Prefab to spawn for splash (alternative)
    [SerializeField] private bool useChildObject = true; // Use child object or spawn prefab
    [SerializeField] private float splashTriggerTime = 0.6f; // Time (0-1) during jump when splash plays (0.6 = 60% through jump)
    
    private bool splashAnimationComplete = false;
    public bool stalling = true;
    
    private bool isScared = false;
    private bool hasReachedHorizontalEnd = false;
    private bool hasReachedFinalPosition = false;
    private Rigidbody2D rb;
    private Collider2D cappaCollider;
    public SpriteRenderer spriteRenderer;
    private Vector2 originalPosition;
    private Vector2 underwaterPosition; // Store the underwater position after first scare
    private bool isFirstScare = true; // Track if this is the first time being scared
    private string currentAnimationState = ""; // Track current animation state
    private Coroutine jumpArcCoroutine; // Track the jump arc coroutine
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cappaCollider = GetComponent<Collider2D>();
        originalPosition = transform.position;
        
        // Get Animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Get SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // If no rigidbody, add one for movement
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation
        }
    }
    
    private void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Cappa: Player not found! Make sure player has 'Player' tag.");
            }
        }
        
        // Find CappaAttacks if not assigned
        if (cappaAttacks == null)
        {
            cappaAttacks = GetComponent<CappaAttacks>();
        }
    }
    
    private void Update()
    {
        // Check if underwater and maintain underwater animation
        // IMPORTANT: Don't override animations when CappaAttacks is handling an attack
        if (cappaAttacks != null && cappaAttacks.IsUnderwater() && !isScared)
        {
            // Check if CappaAttacks is currently attacking - if so, don't override animations
            bool isAttacking = cappaAttacks.IsAttacking();
            
            // Also check if we're in an attack animation state (backup check)
            if (!isAttacking && (currentAnimationState == "JumpAttackUp" || currentAnimationState == "JumpAttackFall" || 
                currentAnimationState == "HandAttack" || currentAnimationState == "Attacking"))
            {
                isAttacking = true;
            }
            
            // Only set underwater idle if not attacking and not already in underwater idle
            if (!isAttacking && currentAnimationState != "UnderwaterIdle")
            {
                SetAnimationState("UnderwaterIdle");
            }
        }
        
        // Handle scared movement
        if (isScared)
        {
            HandleScaredMovement();
            return;
        }
        
        // Don't check attack/scare range when underwater (attacks are handled by CappaAttacks)
        if (cappaAttacks != null && cappaAttacks.IsUnderwater())
        {
            return;
        }
        
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Check if player is in attack range (only when not underwater)
        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }
        // Check if player is in scare range
        else if (distanceToPlayer <= scareRange)
        {
            CheckScareInput();
        }
    }
    
    private void AttackPlayer()
    {
        // Player dies - trigger death through player controller
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.Die();
        }
        else
        {
            // Fallback: restart game directly if no player controller found
            RestartGame();
        }
    }
    
    private void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
    
    private void CheckScareInput()
    {
        bool keyPressed = false;
        
        if (useKeyCode)
        {
            // Use legacy KeyCode system
            keyPressed = Input.GetKeyDown(scareKeyCode);
        }
        else
        {
            // Use new Input System
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyPressed = keyboard[scareKey].wasPressedThisFrame;
            }
        }
        
        if (keyPressed)
        {
            ScareCappa();
        }
    }
    
    private void ScareCappa()
    {
        if (isScared) return; // Already scared
        
        // Only handle first scare (jumping into water)
        // Future scares are handled by CappaAttacks during jump attacks
        bool alreadyUnderwater = cappaAttacks != null && cappaAttacks.IsUnderwater();
        if (alreadyUnderwater)
        {
            // Future scares are handled by CappaAttacks, not here
            return;
        }
        
        isScared = true;
        hasReachedHorizontalEnd = false;
        hasReachedFinalPosition = false;
        isFirstScare = false;
        Debug.Log("Cappa is scared! Jumping away!");
        
        // Play scared sound effect
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayKapScared();
        }
        
        // Make cappa fall through ground (disable collider temporarily)
        if (cappaCollider != null)
        {
            cappaCollider.isTrigger = true;
        }
        
        // Validate transform references
        if (horizontalEndPosition == null)
        {
            Debug.LogError("Cappa: Horizontal End Position transform is not assigned!");
            return;
        }
        if (finalWaterPosition == null)
        {
            Debug.LogError("Cappa: Final Water Position transform is not assigned!");
            return;
        }
        
        // Start the parabolic jump arc
        if (jumpArcCoroutine != null)
        {
            StopCoroutine(jumpArcCoroutine);
        }
        jumpArcCoroutine = StartCoroutine(ParabolicJumpArc());
        
        // Trigger the jump/fall animation once
        SetAnimationState("Jumping");
    }
    
    private void HandleScaredMovement()
    {
        // Movement is now handled by the ParabolicJumpArc coroutine
        // This method is kept for compatibility but movement happens in the coroutine
        if (hasReachedFinalPosition)
        {
            // Stop all movement
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
    
    private System.Collections.IEnumerator ParabolicJumpArc()
    {
        Debug.Log($"=== ParabolicJumpArc STARTED === splashTriggerTime={splashTriggerTime}, jumpDuration={jumpDuration}, useChildObject={useChildObject}");
        
        Vector2 startPos = transform.position;
        Vector2 peakPos = horizontalEndPosition.position;
        Vector2 endPos = finalWaterPosition.position;
        
        Debug.Log($"Jump positions - Start: {startPos}, Peak: {peakPos}, End: {endPos}");
        
        // Calculate the peak height (above the horizontal end position)
        Vector2 arcPeak = new Vector2(peakPos.x, peakPos.y + jumpArcHeight);
        
        bool splashPlayed = false;
        bool hasEnteredWater = false;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / jumpDuration; // Normalized time (0 to 1)
            
            // Create a smooth parabolic path using quadratic Bezier curve
            // P(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
            // Where P₀ = start, P₁ = peak, P₂ = end
            Vector2 position = (1f - t) * (1f - t) * startPos + 
                              2f * (1f - t) * t * arcPeak + 
                              t * t * endPos;
            
            // Play splash effect at fixed time during jump
            if (!splashPlayed && t >= splashTriggerTime)
            {
                Debug.Log($"=== SPLASH TRIGGERED === t={t}, splashTriggerTime={splashTriggerTime}, elapsedTime={elapsedTime}, jumpDuration={jumpDuration}");
                PlaySplashEffect();
                splashPlayed = true;
                splashAnimationComplete = false; // Reset flag
                
                Debug.Log("Cappa entered water - splash effect played");
            }
            else if (!splashPlayed)
            {
                // Debug every 10% progress
                if (Mathf.FloorToInt(t * 10) != Mathf.FloorToInt((t - Time.deltaTime / jumpDuration) * 10))
                {
                    Debug.Log($"Jump progress: {t * 100:F1}% (splash triggers at {splashTriggerTime * 100:F1}%)");
                }
            }
            
            // Wait for splash animation to complete before setting underwater state
            // Note: Sprite renderer is turned on in OnSplashAnimationComplete()
            if (splashPlayed && !hasEnteredWater && splashAnimationComplete)
            {
                hasEnteredWater = true;
                if (cappaAttacks != null)
                {
                    cappaAttacks.SetUnderwater(true);
                }
                
                // Store underwater position for future scares (will be updated when jump completes)
                underwaterPosition = transform.position;
                
                // Switch to underwater idle animation
                SetAnimationState("UnderwaterIdle");
                
                Debug.Log("Splash animation complete - Cappa now underwater");
            }
            
            // Update position
            transform.position = position;
            
            yield return null;
        }
        
        // Ensure we're exactly at the end position
        transform.position = endPos;
        hasReachedFinalPosition = true;
        hasReachedHorizontalEnd = true;
        
        // Store underwater position for future scares
        underwaterPosition = endPos;
        
        // Turn off sprite renderer when jump animation is done
        if (spriteRenderer != null)
        {
            animator.SetBool("IsUnderwaterIdle", true);

            // spriteRenderer.enabled = false;
            // spriteRenderer.sortingOrder = -4;
            Debug.Log("Cappa sprite renderer turned off - jump animation complete");
        }
        
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        // Trigger BGMover when reaching the peak position (horizontal end)
        BGMover bgMover = FindAnyObjectByType<BGMover>();
        if (bgMover != null)
        {
            bgMover.enabled = true;
            bgMover.canMoveBeginning = true;
        }
        
        // Ensure underwater state is set if not already
        if (cappaAttacks != null && !cappaAttacks.IsUnderwater())
        {
            while(stalling)
            {
                yield return null;
            }
            cappaAttacks.SetUnderwater(true);
            SetAnimationState("UnderwaterIdle");
        }
        
        Debug.Log("Cappa completed parabolic jump arc - now underwater");
    }
    
    // Called by SplashEffect when splash animation completes - turn on sprite renderer
    public void OnSplashAnimationComplete()
    {
        splashAnimationComplete = true;
        
        // Turn on sprite renderer when splash animation is done
        // if (spriteRenderer != null)
        // {
        //     spriteRenderer.enabled = true;
        //     Debug.Log("Cappa sprite renderer turned on - splash animation complete");
        // }
    }
    
    // Public method to get underwater position (for CappaAttacks to use)
    public Vector2 GetUnderwaterPosition()
    {
        return underwaterPosition;
    }
    
    // Public method to check if cappa is scared
    public bool IsScared()
    {
        return isScared;
    }
    
    // Public method to reset scared state
    public void ResetScaredState()
    {
        isScared = false;
    }
    
    // Public method to get scare range
    public float GetScareRange()
    {
        return scareRange;
    }
    
    // Public method to get scare key (for CappaAttacks to use)
    public Key GetScareKey()
    {
        return scareKey;
    }
    
    // Called by CappaAttacks when player scares during jump attack
    public void ScareCappaDuringJumpAttack()
    {
        if (isScared) return; // Already scared
        
        isScared = true;
        Debug.Log("Cappa scared during jump attack!");
    }
    
    // Animation state management
    public void SetAnimationState(string state)
    {
        if (animator == null) return;
        
        // Only update if state changed
        if (currentAnimationState == state) return;
        
        currentAnimationState = state;
        
        // Use bool parameters (recommended approach)
        // Reset all animation bools first
        if (HasParameter("IsRunning"))
            animator.SetBool("IsRunning", state == "Running");
        if (HasParameter("IsFalling"))
            animator.SetBool("IsFalling", state == "Falling");
        if (HasParameter("IsIdle"))
            animator.SetBool("IsIdle", state == "Idle");
        if (HasParameter("IsJumping"))
            animator.SetBool("IsJumping", state == "Jumping");
        if (HasParameter("IsUnderwaterIdle"))
            animator.SetBool("IsUnderwaterIdle", state == "UnderwaterIdle");
        if (HasParameter("IsHandAttacking"))
            animator.SetBool("IsHandAttacking", state == "HandAttack");
        if (HasParameter("IsJumpAttackingUp"))
            animator.SetBool("IsJumpAttackingUp", state == "JumpAttackUp");
        if (HasParameter("IsJumpAttackingFall"))
            animator.SetBool("IsJumpAttackingFall", state == "JumpAttackFall");
        
        // Also set "Attacking" state for jump attacks to prevent Update() from overriding
        if (state == "JumpAttackUp" || state == "JumpAttackFall" || state == "HandAttack")
        {
            if (HasParameter("IsAttacking"))
                animator.SetBool("IsAttacking", true);
        }
        else if (state == "UnderwaterIdle")
        {
            if (HasParameter("IsAttacking"))
                animator.SetBool("IsAttacking", false);
        }
        
        // Alternative: Using triggers for one-time animations
        if (state == "Jumping" && HasParameter("StartJump"))
        {
            animator.SetTrigger("StartJump");
        }
        if (state == "UnderwaterIdle" && HasParameter("StartUnderwaterIdle"))
        {
            animator.SetTrigger("StartUnderwaterIdle");
        }
        
        // Alternative: Using triggers (if you prefer trigger-based approach)
        // Uncomment this section if you want to use triggers instead of bools
        /*
        if (state == "Running" && HasParameter("StartRunning"))
        {
            animator.SetTrigger("StartRunning");
        }
        else if (state == "Falling" && HasParameter("StartFalling"))
        {
            animator.SetTrigger("StartFalling");
        }
        */
        
        Debug.Log($"Cappa animation state changed to: {state}");
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
    
    // Play splash effect when entering water
    private void PlaySplashEffect()
    {
        Debug.Log($"=== PlaySplashEffect CALLED === useChildObject={useChildObject}, splashEffectObject={splashEffectObject != null}, splashPrefab={splashPrefab != null}");
        
        // Play enter water sound effect when splash animation starts
        if (SFXManager.Instance != null)
        {
            Debug.Log("Playing kap enter water sound effect");
            SFXManager.Instance.PlayKapEnterWater();
        }
        else
        {
            Debug.LogError("SFXManager.Instance is NULL! Cannot play kap enter water sound.");
        }
        
        if (useChildObject)
        {
            // Use child object approach (recommended)
            if (splashEffectObject != null)
            {
                splashEffectObject.SetActive(true);
                
                // Get or add SplashEffect component to handle completion callback
                SplashEffect splashEffect = splashEffectObject.GetComponent<SplashEffect>();
                if (splashEffect == null)
                {
                    splashEffect = splashEffectObject.AddComponent<SplashEffect>();
                }
                // Set reference to this Cappa script so splash can notify when done
                splashEffect.SetCappaReference(this);
                
                // If the splash object has an animator, trigger the splash animation
                Animator splashAnimator = splashEffectObject.GetComponent<Animator>();
                if (splashAnimator != null)
                {
                    Debug.Log($"Splash animator found. Checking for parameters...");
                    // Try to trigger splash animation
                    if (HasParameterInAnimator(splashAnimator, "Splash"))
                    {
                        Debug.Log("Triggering 'Splash' parameter");
                        splashAnimator.SetTrigger("Splash");
                    }
                    else if (HasParameterInAnimator(splashAnimator, "Play"))
                    {
                        Debug.Log("Triggering 'Play' parameter");
                        splashAnimator.SetTrigger("Play");
                    }
                    else
                    {
                        Debug.LogWarning("Splash animator found but no 'Splash' or 'Play' parameter exists!");
                    }
                }
                else
                {
                    Debug.LogWarning("Splash effect object has no Animator component!");
                }
                
                Debug.Log("Splash effect activated (child object)");
            }
            else
            {
                Debug.LogWarning("Cappa: Splash Effect Object is not assigned!");
            }
        }
        else
        {
            // Spawn prefab approach
            if (splashPrefab != null)
            {
                Vector3 splashPosition = transform.position;
                // Optionally offset the splash position (e.g., at water level)
                if (finalWaterPosition != null)
                {
                    splashPosition = finalWaterPosition.position;
                }
                
                GameObject splashInstance = Instantiate(splashPrefab, splashPosition, Quaternion.identity);
                
                // Get or add SplashEffect component to handle completion callback
                SplashEffect splashEffect = splashInstance.GetComponent<SplashEffect>();
                if (splashEffect == null)
                {
                    splashEffect = splashInstance.AddComponent<SplashEffect>();
                }
                // Set reference to this Cappa script so splash can notify when done
                splashEffect.SetCappaReference(this);
                
                // If the prefab has an animator, trigger the splash animation
                Animator splashAnimator = splashInstance.GetComponent<Animator>();
                if (splashAnimator != null)
                {
                    if (HasParameterInAnimator(splashAnimator, "Splash"))
                    {
                        splashAnimator.SetTrigger("Splash");
                    }
                    else if (HasParameterInAnimator(splashAnimator, "Play"))
                    {
                        splashAnimator.SetTrigger("Play");
                    }
                }
                
                Debug.Log("Splash effect spawned (prefab)");
            }
            else
            {
                Debug.LogWarning("Cappa: Splash Prefab is not assigned!");
            }
        }
    }
    
    // Helper method to check if animator has a parameter (for splash animator)
    private bool HasParameterInAnimator(Animator anim, string paramName)
    {
        if (anim == null || anim.parameters == null) return false;
        
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw scare range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scareRange);
        
        // Draw movement path
        if (horizontalEndPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, horizontalEndPosition.position);
            Gizmos.DrawWireSphere(horizontalEndPosition.position, 0.3f);
        }
        
        if (finalWaterPosition != null)
        {
            Gizmos.color = Color.cyan;
            if (horizontalEndPosition != null)
            {
                Gizmos.DrawLine(horizontalEndPosition.position, finalWaterPosition.position);
            }
            else
            {
                Gizmos.DrawLine(transform.position, finalWaterPosition.position);
            }
            Gizmos.DrawWireSphere(finalWaterPosition.position, 0.3f);
        }
    }
}


