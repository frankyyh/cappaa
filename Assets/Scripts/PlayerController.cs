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
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
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
        if (isDead) return;
        
        // Get keyboard input
        keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        // Read movement input
        horizontalInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontalInput = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontalInput = 1f;
        
        // Read jump input
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpPressed = true;
            jumpBufferCounter = jumpBufferTime;
        }
        
        if (keyboard.spaceKey.wasReleasedThisFrame)
        {
            jumpReleased = true;
        }
        
        CheckGrounded();
        HandleJumpBuffer();
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
        
        // Apply acceleration or deceleration
        float velocityChange;
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
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
        rb.linearVelocity = new Vector2(currentVelocityX, rb.linearVelocity.y);
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
            // Reset Y velocity first to ensure consistent jump height
            // This prevents any downward velocity from affecting the jump
            Vector2 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector2(currentVelocity.x, 0f);
            
            // Apply jump force - using velocity for immediate response
            rb.linearVelocity = new Vector2(currentVelocity.x, jumpForce);
            
            hasJumped = true;
            jumpBufferCounter = 0;
            jumpPressed = false;
            jumpReleased = false; // Reset jump released flag when jumping
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
        rb.isKinematic = true; // Prevent physics interactions
        
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
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}

