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
    private bool canBeScared = false; // Track if Cappa can be scared (controlled by animation events)
    private bool scareWindowEnded = false; // Track if scare window has ended
    private bool jumpUpComplete = false; // Track if jump up movement is complete (animation event)
    private bool jumpDownComplete = false; // Track if jump down movement is complete (animation event)
    private bool shouldFall = false; // Track if Cappa should start falling
    private bool shouldStartJumpUp = false; // Track if Cappa should start moving up (animation event)
    private bool shouldStartJumpDown = false; // Track if Cappa should start moving down (animation event)
    private bool playerWasInRangeDuringScareWindow = false; // Track if player was in range during scare window (for death check)
    
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
        if (!underwater || isAttacking)
        {
            // Debug why attacks aren't happening
            if (!underwater && _attackTimer <= 0f)
            {
                Debug.LogWarning($"Attack blocked: underwater={underwater}, isAttacking={isAttacking}, timer={_attackTimer}");
            }
            return;
        }
        
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            // Pick an attack at random
            if (Random.value < 0.5f)
            {
                Debug.Log("Starting Hand Attack");
                HandsAttack();
            }
            else
            {
                Debug.Log("Starting Jump Attack");
                JumpAttack();
            }

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
        
        // Set animation state to indicate we're attacking (prevents Cappa.Update from overriding)
        if (cappaScript != null)
        {
            cappaScript.SetAnimationState("HandAttack");
        }
        
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
        
        // Play hand attack sound effect
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayKapHand();
        }
        
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
        // Don't set animation state here - let it be set after coroutine completes
        // This prevents interference with the coroutine's final reset section
        // The animation state will be set in the finally block or after coroutine ends
        Debug.Log("ReturnToUnderwaterIdle called - animation will be set after coroutine completes");
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
        
        try
        {
        // Reset scared state at the start of each jump attack
        if (cappaScript != null)
        {
            cappaScript.ResetScaredState();
                // Set animation state to indicate we're attacking (prevents Cappa.Update from overriding)
                cappaScript.SetAnimationState("JumpAttackUp");
            }
            
            // Reset all movement and animation flags
            jumpUpComplete = false;
            jumpDownComplete = false;
            shouldFall = false;
            shouldStartJumpUp = false;
            shouldStartJumpDown = false;
            canBeScared = false;
            scareWindowEnded = false;
        
            // Trigger jump attack up animation (Phase 1: Jumping up)
            TriggerJumpAttackUpAnimation();
        
        // Cappa jumps up (using transform, not rigidbody)
        // Store only Y position to allow horizontal movement
        jumpStartY = transform.position.y;
        
        // Phase 1a: Wait for animation event to start moving up
        while (!shouldStartJumpUp && !wasScaredDuringJump)
        {
            // Check for early scare input even before movement starts
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
                    UnityEngine.InputSystem.Key scareKey = cappaScript.GetScareKey();
                    scareKeyPressed = keyboard[scareKey].wasPressedThisFrame;
                }
            }
            
            // If player pressed scare key early, scare Cappa immediately
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
        
        // Phase 1b: Move up after animation event fires, until CompleteJumpUp event
        // Also check for early scares during jump up
        float currentYOffset = 0f;
        while (!jumpUpComplete && !wasScaredDuringJump)
        {
            // Move Cappa up smoothly (only if movement has started)
            if (shouldStartJumpUp)
            {
                // Smoothly move towards peak height
                currentYOffset = Mathf.MoveTowards(currentYOffset, jumpHeight, (jumpHeight / 0.5f) * Time.deltaTime);
                Vector3 currentPos = transform.position;
                transform.position = new Vector3(currentPos.x, jumpStartY + currentYOffset, currentPos.z);
            }
            
            // Check for early scare input even during jump up
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
                    UnityEngine.InputSystem.Key scareKey = cappaScript.GetScareKey();
                    scareKeyPressed = keyboard[scareKey].wasPressedThisFrame;
                }
            }
            
            // If player pressed scare key early, scare Cappa immediately
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
        
        // If scared early, skip to handling scared state
        if (wasScaredDuringJump)
        {
            // Ensure we're at least at some height if jump up wasn't complete
            if (!jumpUpComplete)
            {
                Vector3 currentPos = transform.position;
                // Use current Y or a minimum height
                float currentHeight = Mathf.Max(currentPos.y - jumpStartY, jumpHeight * 0.5f);
                transform.position = new Vector3(currentPos.x, jumpStartY + currentHeight, currentPos.z);
            }
            else
            {
                // Ensure we're at the peak (set by animation event)
                Vector3 peakPos = transform.position;
                transform.position = new Vector3(peakPos.x, jumpStartY + jumpHeight, peakPos.z);
            }
        }
        else
        {
            // Ensure we're at the peak (set by animation event)
            Vector3 peakPos = transform.position;
            transform.position = new Vector3(peakPos.x, jumpStartY + jumpHeight, peakPos.z);
            
            Debug.Log($"=== ENTERING SCARE WINDOW PHASE === Position: {transform.position}, jumpStartY: {jumpStartY}, jumpHeight: {jumpHeight}");
            
            // Phase 2: Wait for animation events to control scare window
            // Reset scare window flags BEFORE waiting (in case StartScareWindow was already called)
            bool wasAlreadyScared = canBeScared; // Check if StartScareWindow already fired
            canBeScared = false;
            scareWindowEnded = false;
            
            Debug.Log($"=== RESET SCARE WINDOW FLAGS === canBeScared={canBeScared}, scareWindowEnded={scareWindowEnded}, wasAlreadyScared={wasAlreadyScared}");
            
            // If StartScareWindow already fired, we need to set it back
            if (wasAlreadyScared)
            {
                Debug.Log("=== StartScareWindow already fired - setting canBeScared back to true ===");
                canBeScared = true;
            }
            
            // Wait for scare window to start (triggered by animation event) - but only if not already set
            if (!canBeScared)
            {
                Debug.Log($"=== WAITING FOR SCARE WINDOW TO START === canBeScared={canBeScared}, scareWindowEnded={scareWindowEnded}, wasScaredDuringJump={wasScaredDuringJump}");
                while (!canBeScared && !scareWindowEnded && !wasScaredDuringJump)
                {
                    yield return null;
                }
            }
            
            Debug.Log($"=== SCARE WINDOW START WAIT COMPLETE === canBeScared={canBeScared}, scareWindowEnded={scareWindowEnded}, wasScaredDuringJump={wasScaredDuringJump}");
            
            // Phase 2b: Check for scare input while scare window is open
            playerWasInRangeDuringScareWindow = false; // Reset for this attack
            int frameCount = 0;
            Debug.Log($"=== SCARE WINDOW OPENED === canBeScared={canBeScared}, scareWindowEnded={scareWindowEnded}, wasScaredDuringJump={wasScaredDuringJump}");
            
            // Check if window already ended before we could check
            if (scareWindowEnded)
            {
                Debug.LogWarning("=== WARNING: Scare window ended before loop could run! ===");
            }
            
            while (canBeScared && !scareWindowEnded && !wasScaredDuringJump)
            {
                frameCount++;
                
                // Check if player pressed scare key
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
                
                if (scareKeyPressed)
                {
                    Debug.Log($"Frame {frameCount}: SCARE KEY PRESSED!");
                }
                
                // If player pressed scare key, scare Cappa
                if (scareKeyPressed && cappaScript != null)
                {
                    Debug.Log($"Frame {frameCount}: Scaring Cappa during jump attack");
                    cappaScript.ScareCappaDuringJumpAttack();
                    wasScaredDuringJump = true;
                    break;
                }
                
                // Also check if cappa was already scared (backup check)
                if (cappaScript != null && cappaScript.IsScared())
                {
                    Debug.Log($"Frame {frameCount}: Cappa already scared (backup check)");
                    wasScaredDuringJump = true;
                    break;
                }
                
                // CONTINUOUSLY check if player is in range during the scare window
                if (player != null && cappaScript != null && !wasScaredDuringJump)
                {
                    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                    float scareRange = cappaScript.GetScareRange();
                    
                    if (distanceToPlayer <= scareRange)
                    {
                        if (!playerWasInRangeDuringScareWindow)
                        {
                            Debug.Log($"Frame {frameCount}: Player entered scare range! Distance: {distanceToPlayer}, Range: {scareRange}");
                        }
                        playerWasInRangeDuringScareWindow = true;
                    }
                    else if (frameCount % 10 == 0) // Log every 10 frames to avoid spam
                    {
                        Debug.Log($"Frame {frameCount}: Player NOT in range. Distance: {distanceToPlayer}, Range: {scareRange}, Cappa pos: {transform.position}, Player pos: {player.position}");
                    }
                }
                else
                {
                    if (player == null)
                        Debug.LogWarning($"Frame {frameCount}: Player is NULL!");
                    if (cappaScript == null)
                        Debug.LogWarning($"Frame {frameCount}: CappaScript is NULL!");
                }
                
                yield return null;
            }
            
            Debug.Log($"=== SCARE WINDOW LOOP EXITED === Frame: {frameCount}, canBeScared={canBeScared}, scareWindowEnded={scareWindowEnded}, wasScaredDuringJump={wasScaredDuringJump}, playerWasInRange={playerWasInRangeDuringScareWindow}");
            
            // Wait for scare window to end if it hasn't already and not scared
            while (!scareWindowEnded && !wasScaredDuringJump)
            {
                yield return null;
            }
            
            Debug.Log($"=== WAITING FOR SCARE WINDOW END COMPLETE === scareWindowEnded={scareWindowEnded}, wasScaredDuringJump={wasScaredDuringJump}");
            
            // If EndScareWindow was never called, force it to end after a timeout
            // This ensures the death check always runs
            if (!scareWindowEnded && !wasScaredDuringJump)
            {
                Debug.LogWarning("=== EndScareWindow was never called! Forcing scare window to end ===");
                scareWindowEnded = true;
                canBeScared = false;
            }
            
            // ALWAYS check if player should die when scare window closes (if not scared)
            // This check MUST happen regardless of whether the scare window loop ran
            Debug.Log($"=== DEATH CHECK STARTING === wasScaredDuringJump={wasScaredDuringJump}, player={player != null}, cappaScript={cappaScript != null}");
            
            // Perform death check - this should ALWAYS run if window ended and player didn't scare
            if (!wasScaredDuringJump)
            {
                Debug.Log("=== Player did NOT scare - checking if they should die ===");
                
                if (player == null)
                {
                    Debug.LogError("=== DEATH CHECK FAILED: Player is NULL ===");
                }
                else if (cappaScript == null)
                {
                    Debug.LogError("=== DEATH CHECK FAILED: CappaScript is NULL ===");
        }
        else
        {
                    // Check if player is in scare range at the moment the window closes
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                float scareRange = cappaScript.GetScareRange();
                    
                // Use the continuous check OR the final check
                bool playerInRange = playerWasInRangeDuringScareWindow || (distanceToPlayer <= scareRange);
                
                Debug.Log($"=== DEATH CHECK DETAILS ===");
                Debug.Log($"  Distance to player: {distanceToPlayer}");
                Debug.Log($"  Scare range: {scareRange}");
                Debug.Log($"  Was in range during window: {playerWasInRangeDuringScareWindow}");
                    Debug.Log($"  Is in range now: {distanceToPlayer <= scareRange}");
                    Debug.Log($"  Final playerInRange result: {playerInRange}");
                    Debug.Log($"  Cappa position: {transform.position}");
                    Debug.Log($"  Player position: {player.position}");
                    Debug.Log($"  Was scared: {wasScaredDuringJump}");
                    
                    if (playerInRange)
                {
                    // Player is in scare range and didn't scare cappa - player dies
                        Debug.Log("=== PLAYER IS IN SCARE RANGE AND DIDN'T SCARE - PLAYER DIES ===");
                    PlayerController playerController = player.GetComponent<PlayerController>();
                    if (playerController != null)
                        {
                            Debug.Log("Calling playerController.Die()...");
                            playerController.Die();
                            // Player died - exit coroutine early (game will restart)
                            isAttacking = false;
                            yield break;
                        }
                        else
                        {
                            Debug.LogError("PlayerController is NULL! Cannot kill player!");
                        }
                    }
                    else
                    {
                        Debug.Log($"=== PLAYER SURVIVES === Distance ({distanceToPlayer}) > Range ({scareRange}) OR was not in range during window");
                    }
                }
            }
            else
            {
                Debug.Log("=== DEATH CHECK SKIPPED === Player already scared Cappa (wasScaredDuringJump=true)");
            }
        }
        
        // Phase 3: Handle outcome
        if (wasScaredDuringJump)
        {
            // Player scared cappa - trigger fall animation and play jump down animation
            TriggerJumpAttackFallAnimation();
            
            // Update animation state to fall animation (prevents Cappa.Update from overriding)
            if (cappaScript != null)
            {
                cappaScript.SetAnimationState("JumpAttackFall");
            }
            
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
            
            // Reset jump down movement flags for the fall animation
            shouldStartJumpDown = false;
            jumpDownComplete = false;
            
            // Wait for animation event to start falling
            while (!shouldStartJumpDown && !jumpDownComplete)
            {
                yield return null;
            }
            
            Debug.Log($"=== Scared early: Jump down phase started. shouldStartJumpDown={shouldStartJumpDown}, jumpDownComplete={jumpDownComplete}");
            
            // Move down (movement controlled by animation events) - same as normal jump down
            float currentYOffsetDown = jumpHeight;
            // If we're not at peak yet, use current height
            if (transform.position.y < jumpStartY + jumpHeight)
            {
                currentYOffsetDown = transform.position.y - jumpStartY;
            }
            
            while (!jumpDownComplete)
            {
                // Move down smoothly if movement has started
                if (shouldStartJumpDown)
                {
                    // Smoothly move down towards underwater position
                    currentYOffsetDown = Mathf.MoveTowards(currentYOffsetDown, 0f, (jumpHeight / 0.5f) * Time.deltaTime);
                    
                    // Interpolate between current position and underwater position
                    Vector2 startPos = transform.position;
                    float fallProgress = 1f - (currentYOffsetDown / jumpHeight); // Progress from peak to underwater
                    Vector2 currentPos = Vector2.Lerp(startPos, underwaterPos, fallProgress * 0.5f); // Only move partway during animation
                    transform.position = new Vector3(currentPos.x, jumpStartY + currentYOffsetDown, transform.position.z);
                }
                
                yield return null;
            }
            
            Debug.Log($"=== Scared early: Jump down movement loop exited. jumpDownComplete={jumpDownComplete}");
            
            // After jump down animation completes, smoothly move to final underwater position
            Vector2 finalStartPos = transform.position;
            float finalFallTime = 0.2f;
            float finalFallElapsed = 0f;
            
            while (finalFallElapsed < finalFallTime)
            {
                finalFallElapsed += Time.deltaTime;
                float finalFallProgress = finalFallElapsed / finalFallTime;
                
                // Smoothly move to underwater position
                Vector2 currentPos = Vector2.Lerp(finalStartPos, underwaterPos, finalFallProgress);
                transform.position = currentPos;
                
                yield return null;
            }
            
            // Ensure we're back at underwater position
            transform.position = underwaterPos;
            
            // Return to underwater idle animation
            ReturnToUnderwaterIdle();
            
            Debug.Log("Cappa returned to underwater position after being scared (with jump down animation)");
        }
        else
        {
            // Player didn't scare - death check already happened when scare window closed
            // Now just continue with the fall animation
            
            // Trigger fall animation and wait for animation events to control movement
            TriggerJumpAttackFallAnimation();
            
            // Update animation state to fall animation (prevents Cappa.Update from overriding)
            if (cappaScript != null)
            {
                cappaScript.SetAnimationState("JumpAttackFall");
            }
            
            shouldStartJumpDown = false;
            jumpDownComplete = false;
            
            // Wait for animation event to start falling
            while (!shouldStartJumpDown && !jumpDownComplete)
            {
                yield return null;
            }
            
            Debug.Log($"Jump down phase started. shouldStartJumpDown={shouldStartJumpDown}, jumpDownComplete={jumpDownComplete}");
            
            // Move down (movement controlled by animation events)
            float currentYOffsetDown = jumpHeight;
            while (!jumpDownComplete)
            {
                // Move down smoothly if movement has started
                if (shouldStartJumpDown)
                {
                    // Smoothly move down
                    currentYOffsetDown = Mathf.MoveTowards(currentYOffsetDown, 0f, (jumpHeight / 0.5f) * Time.deltaTime);
                Vector3 currentPos = transform.position;
                    transform.position = new Vector3(currentPos.x, jumpStartY + currentYOffsetDown, currentPos.z);
                }
                
                yield return null;
            }
            
            Debug.Log($"Jump down movement loop exited. jumpDownComplete={jumpDownComplete}");
            
            // Ensure we're back at original position (set by animation event)
            Vector3 finalPos = transform.position;
            transform.position = new Vector3(finalPos.x, jumpStartY, finalPos.z);
            
            // Return to underwater idle animation
            ReturnToUnderwaterIdle();
            
            Debug.Log("About to exit jump attack coroutine (not scared path)");
        }
        
        Debug.Log("Jump attack coroutine reached final reset section");
        
        // Reset scared state after jump attack completes (for next attack)
        if (cappaScript != null)
        {
            cappaScript.ResetScaredState();
        }
        
        }
        finally
        {
            // ALWAYS reset isAttacking and animation state, even if coroutine exits early or throws exception
            isAttacking = false;
            
            // Return to underwater idle animation now that attack is complete
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
                if (HasParameter("IsJumpAttackingUp"))
                {
                    animator.SetBool("IsJumpAttackingUp", false);
                }
                if (HasParameter("IsJumpAttackingFall"))
                {
                    animator.SetBool("IsJumpAttackingFall", false);
                }
                if (HasParameter("IsUnderwaterIdle"))
                {
                    animator.SetBool("IsUnderwaterIdle", true);
                }
            }
            
            Debug.Log($"Jump attack coroutine ended. isAttacking={isAttacking}, underwater={underwater}, attackTimer={_attackTimer}");
        }
    }
    
    // Trigger jump attack up animation (Phase 1: Jumping up)
    private void TriggerJumpAttackUpAnimation()
    {
        if (animator == null) return;
        
        // Try trigger parameter first (recommended for one-time animations)
        if (HasParameter("JumpAttackUp"))
        {
            animator.SetTrigger("JumpAttackUp");
        }
        else if (HasParameter("JumpUp"))
        {
            animator.SetTrigger("JumpUp");
        }
        // Fallback to bool parameter
        else if (HasParameter("IsJumpAttackingUp"))
        {
            animator.SetBool("IsJumpAttackingUp", true);
        }
        
        Debug.Log("Jump attack up animation triggered");
    }
    
    // Trigger jump attack fall animation (Phase 2: Falling back down)
    private void TriggerJumpAttackFallAnimation()
    {
        if (animator == null) return;
        
        // Reset up animation bool if using bools
        if (HasParameter("IsJumpAttackingUp"))
        {
            animator.SetBool("IsJumpAttackingUp", false);
        }
        
        // Try trigger parameter first
        if (HasParameter("JumpAttackFall"))
        {
            animator.SetTrigger("JumpAttackFall");
        }
        else if (HasParameter("JumpFall"))
        {
            animator.SetTrigger("JumpFall");
        }
        // Fallback to bool parameter
        else if (HasParameter("IsJumpAttackingFall"))
        {
            animator.SetBool("IsJumpAttackingFall", true);
        }
        
        Debug.Log("Jump attack fall animation triggered");
    }
    
    // Called by Animation Event when scare window starts (player can now scare Cappa)
    public void StartScareWindow()
    {
        canBeScared = true;
        Debug.Log($"=== StartScareWindow CALLED === canBeScared={canBeScared}, scareWindowEnded={scareWindowEnded}");
    }
    
    // Called by Animation Event when scare window ends (player can no longer scare)
    public void EndScareWindow()
    {
        Debug.Log($"=== EndScareWindow CALLED === canBeScared={canBeScared}, scareWindowEnded={scareWindowEnded}");
        canBeScared = false;
        scareWindowEnded = true;
        Debug.Log("Scare window ended - player can no longer scare Cappa");
    }
    
    // Called by Animation Event when Cappa should start moving up
    public void StartJumpUpMovement()
    {
        shouldStartJumpUp = true;
        
        // Play jump attack sound effect when movement starts
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayKapHand();
        }
        
        Debug.Log("Jump up movement started - Cappa begins moving up");
    }
    
    // Called by Animation Event when jump up movement should complete
    public void CompleteJumpUp()
    {
        jumpUpComplete = true;
        // Set position to peak
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, jumpStartY + jumpHeight, currentPos.z);
        Debug.Log("Jump up movement complete - Cappa at peak");
    }
    
    // Called by Animation Event when Cappa should start moving down
    public void StartJumpDownMovement()
    {
        shouldStartJumpDown = true;
        Debug.Log("Jump down movement started - Cappa begins moving down");
    }
    
    // Called by Animation Event when jump down movement should complete
    public void CompleteJumpDown()
    {
        jumpDownComplete = true;
        // Set position back to start
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, jumpStartY, currentPos.z);
        Debug.Log("Jump down movement complete - Cappa back to start");
        
        // CRITICAL: Perform death check here as fallback if coroutine didn't check it
        // This ensures player dies if they were in range and didn't scare
        if (!wasScaredDuringJump && player != null && cappaScript != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            float scareRange = cappaScript.GetScareRange();
            
            // Use the range check from during the scare window OR current distance
            bool playerInRange = playerWasInRangeDuringScareWindow || (distanceToPlayer <= scareRange);
            
            Debug.Log($"=== CompleteJumpDown: DEATH CHECK === Distance: {distanceToPlayer}, Range: {scareRange}, Was in range during window: {playerWasInRangeDuringScareWindow}, Was scared: {wasScaredDuringJump}");
            
            if (playerInRange)
            {
                Debug.Log("=== CompleteJumpDown: PLAYER IS IN RANGE AND DIDN'T SCARE - PLAYER DIES ===");
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.Die();
                    isAttacking = false;
                    return; // Exit early, don't continue cleanup
                }
            }
            else
            {
                Debug.Log($"=== CompleteJumpDown: Player survives (not in range) ===");
            }
        }
        
        // Complete the jump attack - reset everything here to ensure it always happens
        // This ensures the attack state is reset even if the coroutine doesn't complete normally
        Debug.Log("CompleteJumpDown: Completing jump attack cleanup");
        
        // Reset scared state after jump attack completes (for next attack)
        if (cappaScript != null)
        {
            cappaScript.ResetScaredState();
        }
        
        // Reset isAttacking flag - this is critical for allowing future attacks
        isAttacking = false;
        
        // Return to underwater idle animation now that attack is complete
        if (cappaScript != null)
        {
            cappaScript.SetAnimationState("UnderwaterIdle");
        }
        else if (animator != null)
        {
            // Fallback: directly set underwater idle if Cappa script not available
            if (HasParameter("IsJumpAttackingUp"))
            {
                animator.SetBool("IsJumpAttackingUp", false);
            }
            if (HasParameter("IsJumpAttackingFall"))
            {
                animator.SetBool("IsJumpAttackingFall", false);
            }
            if (HasParameter("IsUnderwaterIdle"))
            {
                animator.SetBool("IsUnderwaterIdle", true);
            }
        }
        
        Debug.Log($"CompleteJumpDown: Jump attack fully complete. isAttacking={isAttacking}, underwater={underwater}");
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
    
    // Public method to check if currently attacking
    public bool IsAttacking()
    {
        return isAttacking;
    }
    
}
