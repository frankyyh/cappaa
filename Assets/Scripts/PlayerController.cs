using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] bool isWalking;
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private bool debugGroundCheck = false;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    
    [Header("Death Settings")]
    [SerializeField] private string waterTag = "Water";
    [SerializeField] private float restartDelay = 1f;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    private Rigidbody2D rb;
    private Keyboard keyboard;
    
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float jumpBufferCounter;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool hasJumped;
    private bool isDead;
    [SerializeField] private bool canMove = true; // Control for WASD movement input
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component not found!");
            return;
        }
        
        Debug.Log($"Rigidbody2D found. Body Type: {rb.bodyType}, Constraints: {rb.constraints}");
        
        // Get Animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Disable Apply Root Motion to prevent animator from interfering with movement
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        // Enable interpolation to prevent camera jittering
        // This smooths the movement between FixedUpdate frames
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Set up zero friction physics material
        SetupZeroFriction();
        
        // Create ground check point if not assigned
        if (groundCheckPoint == null)
        {
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(transform);
            groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheckPoint = groundCheck.transform;
        }
        
        Debug.Log($"PlayerController initialized. canMove: {canMove}, isDead: {isDead}");
    }
    
    private void SetupZeroFriction()
    {
        // Create or get physics material with zero friction
        PhysicsMaterial2D material = new PhysicsMaterial2D("ZeroFriction");
        material.friction = 0f;
        material.bounciness = 0f;
        
        // Apply to all colliders on this GameObject
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.sharedMaterial = material;
        }
    }
    
    private void Start()
    {
        // Adjust ground check position based on collider if available
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && groundCheckPoint != null)
        {
            Bounds bounds = col.bounds;
            float offsetY = -(bounds.extents.y + groundCheckRadius);
            groundCheckPoint.localPosition = new Vector3(0, offsetY, 0);
        }
    }
    
    private void Update()
    {
        // Don't process input if dead
        if (isDead)
        {
            Debug.LogWarning("Player is dead! Cannot process input.");
            return;
        }
        
        // Get keyboard input
        keyboard = Keyboard.current;
        if (keyboard == null)
        {
            Debug.LogError("Keyboard.current is null! Make sure Input System is enabled in Project Settings > Player > Active Input Handling");
            return;
        }
        if (isWalking && isGrounded && SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayWalk();
        }
        // Read movement input (only if canMove is true)
        horizontalInput = 0f;
        if (canMove)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                horizontalInput = -1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                horizontalInput = 1f;
        }
        else
        {
            Debug.LogWarning($"canMove is FALSE! Movement disabled.");
        }
        
        // Read jump input (always enabled)
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("SPACE KEY PRESSED!");
            jumpPressed = true;
            jumpBufferCounter = jumpBufferTime;
        }
        
        if (keyboard.spaceKey.wasReleasedThisFrame)
        {
            jumpReleased = true;
        }
        
        // Read scare input (E key)
        if (keyboard.eKey.wasPressedThisFrame)
        {
            Debug.Log("E KEY PRESSED - Scaring!");
            // Trigger scare animation
            if (animator != null)
            {
                // Check if the trigger parameter exists
                bool hasScareTrigger = false;
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == "Scare" && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        hasScareTrigger = true;
                        break;
                    }
                }
                
                if (hasScareTrigger)
                {
                    animator.SetTrigger("Scare");
                    SFXManager.Instance.PlayScare();
                    Debug.Log("Scare animation triggered!");
                }
                else
                {
                    Debug.LogWarning("'Scare' trigger parameter not found in Animator Controller! Please add it in the Animator window.");
                }
            }
            else
            {
                Debug.LogWarning("Animator component is null! Make sure it's assigned in the PlayerController.");
            }
        }
        
        CheckGrounded();
        HandleJumpBuffer();
        UpdateAnimations();
    }
    
    private void FixedUpdate()
    {
        // Don't process movement if dead
        if (isDead) return;
        
        HandleMovement();
        HandleJump();
    }
    
    private void HandleMovement()
    {
        // Get current horizontal velocity
        float currentVelocityX = rb.linearVelocity.x;
        float targetVelocityX = horizontalInput * moveSpeed;
        
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            Debug.Log($"Movement input detected! Input: {horizontalInput}, Target: {targetVelocityX}, Current: {currentVelocityX}");
        }
        
        // Apply acceleration or deceleration
        float velocityChange;
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            isWalking = true;
            // Accelerating towards target speed
            velocityChange = acceleration * Time.fixedDeltaTime;
            
            // Move towards target velocity
            if (targetVelocityX > currentVelocityX)
            {
                currentVelocityX = Mathf.Min(currentVelocityX + velocityChange, targetVelocityX);
            }
            else if (targetVelocityX < currentVelocityX)
            {
                currentVelocityX = Mathf.Max(currentVelocityX - velocityChange, targetVelocityX);
            }
        }
        else
        {
            // Decelerating (no input)
            velocityChange = deceleration * Time.fixedDeltaTime;
            isWalking = false;
            if (currentVelocityX > 0)
            {
                currentVelocityX = Mathf.Max(0, currentVelocityX - velocityChange);
            }
            else if (currentVelocityX < 0)
            {
                currentVelocityX = Mathf.Min(0, currentVelocityX + velocityChange);
            }
        }
        
        // Apply the new horizontal velocity (preserve Y velocity)
        Vector2 newVel = new Vector2(currentVelocityX, rb.linearVelocity.y);
        rb.linearVelocity = newVel;
        
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            Debug.Log($"Applied velocity: {newVel}, Rigidbody velocity after: {rb.linearVelocity}");
        }
    }
    
    private void CheckGrounded()
    {
        wasGrounded = isGrounded;
        
        // If no layer mask is set, check all layers (for easier setup)
        Collider2D hitCollider;
        if (groundLayerMask.value == 0)
        {
            hitCollider = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius);
        }
        else
        {
            hitCollider = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayerMask);
        }
        
        isGrounded = hitCollider != null;
        
        // Reset jump ability whenever grounded (allows jumping again after landing)
        if (isGrounded)
        {
            hasJumped = false;
        }
        
        // Debug output
        if (debugGroundCheck)
        {
            Debug.Log($"Grounded: {isGrounded}, Position: {groundCheckPoint.position}, Hit: {hitCollider?.name ?? "None"}");
        }
    }
    
    private void HandleJumpBuffer()
    {
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        else
        {
            // Reset jump pressed flag if buffer expired and we haven't jumped
            jumpPressed = false;
        }
    }
    
    private void HandleJump()
    {
        // Can only jump if grounded and haven't already jumped
        bool canJump = isGrounded && !hasJumped && jumpBufferCounter > 0;
        
        if (canJump && jumpPressed)
        {
            Debug.Log($"JUMPING! Grounded: {isGrounded}, HasJumped: {hasJumped}, Buffer: {jumpBufferCounter}");
            
            // Reset Y velocity first to ensure consistent jump height
            // This prevents any downward velocity from affecting the jump
            Vector2 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector2(currentVelocity.x, 0f);
            
            // Apply jump force - using velocity for immediate response
            rb.linearVelocity = new Vector2(currentVelocity.x, jumpForce);
            // Play Jump sound effect
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayJump();

            }
            Debug.Log($"Applied jump force. New velocity: {rb.linearVelocity}");
            
            hasJumped = true;
            jumpBufferCounter = 0;
            jumpPressed = false;
            jumpReleased = false; // Reset jump released flag when jumping
            
            // Trigger jump animation
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }
        else if (jumpPressed)
        {
            Debug.LogWarning($"Cannot jump! Grounded: {isGrounded}, HasJumped: {hasJumped}, Buffer: {jumpBufferCounter}");
        }
        
        // Variable jump height - reduce velocity when jump button is released early
        // Only apply if we're in the air and moving up (not on ground)
        if (jumpReleased && !isGrounded && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            jumpReleased = false;
        }
        
        // Reset jumpReleased if we're grounded (prevents carryover to next jump)
        if (isGrounded)
        {
            jumpReleased = false;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if collided with water
        if (!isDead && collision.CompareTag(waterTag))
        {
            Die();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if collided with water (in case water uses collision instead of trigger)
        if (!isDead && collision.gameObject.CompareTag(waterTag))
        {
            Die();
        }
    }
    
    public void Die()
    {
        if (isDead) return; // Prevent multiple death calls
        
        isDead = true;
        
        // Stop player movement
        rb.linearVelocity = Vector2.zero;
        
        // Disable player input
        horizontalInput = 0f;
        jumpPressed = false;
        
        // Restart the game after delay
        Invoke(nameof(RestartGame), restartDelay);
    }
    
    private void RestartGame()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Public method to enable/disable movement (jumping remains enabled)
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
        if (!enabled)
        {
            // Clear horizontal input when movement is disabled
            horizontalInput = 0f;
        }
    }
    
    // Public property to get/set canMove directly
    public bool CanMove
    {
        get { return canMove; }
        set 
        { 
            canMove = value;
            if (!canMove)
            {
                horizontalInput = 0f;
            }
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Update speed parameter for walk animation
        float speed = Mathf.Abs(horizontalInput);
        animator.SetFloat("Speed", speed);
        
        // Update grounded state
        animator.SetBool("IsGrounded", isGrounded);
        
        // Handle sprite flipping based on movement direction
        if (horizontalInput > 0.1f)
        {
            // Moving right - face right
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (horizontalInput < -0.1f)
        {
            // Moving left - face left
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}

