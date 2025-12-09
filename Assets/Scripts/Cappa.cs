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
    
    [Header("Movement References")]
    [SerializeField] private Transform horizontalEndPosition; // Where Cappa stops moving right
    [SerializeField] private Transform finalWaterPosition; // Final position in water where Cappa stops
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private string waterTag = "Water";
    [SerializeField] private CappaAttacks cappaAttacks;
    
    private bool isScared = false;
    private bool hasReachedHorizontalEnd = false;
    private bool hasReachedFinalPosition = false;
    private Rigidbody2D rb;
    private Collider2D cappaCollider;
    private Vector2 originalPosition;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cappaCollider = GetComponent<Collider2D>();
        originalPosition = transform.position;
        
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
        // Handle scared movement
        if (isScared)
        {
            HandleScaredMovement();
            return;
        }
        
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Check if player is in attack range
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
        
        isScared = true;
        Debug.Log("Cappa is scared! Running away!");
        
        // Make cappa fall through ground (disable collider temporarily)
        if (cappaCollider != null)
        {
            cappaCollider.isTrigger = true;
        }
        
        // Validate transform references
        if (horizontalEndPosition == null)
        {
            Debug.LogError("Cappa: Horizontal End Position transform is not assigned!");
        }
        if (finalWaterPosition == null)
        {
            Debug.LogError("Cappa: Final Water Position transform is not assigned!");
        }
    }
    
    private void HandleScaredMovement()
    {
        // Don't move if underwater
        if (cappaAttacks != null && cappaAttacks.IsUnderwater())
        {
            // Stop all movement when underwater
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }
        
        if (hasReachedFinalPosition)
        {
            // Stop all movement
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }
        
        // First phase: Move to horizontal end position
        if (!hasReachedHorizontalEnd && horizontalEndPosition != null)
        {
            Vector2 targetPos = horizontalEndPosition.position;
            Vector2 currentPos = transform.position;
            
            // Check if we've reached the horizontal end position
            if (Vector2.Distance(currentPos, targetPos) < 0.1f)
            {
                hasReachedHorizontalEnd = true;
                transform.position = targetPos; // Snap to exact position
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                FindAnyObjectByType<BGMover>().canMoveBeginning = true;
                Debug.Log("Cappa reached horizontal end position");
            }
            else
            {
                // Move towards horizontal end position
                Vector2 direction = (targetPos - currentPos).normalized;
                if (rb != null)
                {
                    rb.linearVelocity = direction * runSpeed;
                }
                else
                {
                    // Fallback if no rigidbody
                    transform.position = Vector2.MoveTowards(currentPos, targetPos, runSpeed * Time.deltaTime);
                }
            }
        }
        // Second phase: Move to final water position
        else if (hasReachedHorizontalEnd && finalWaterPosition != null)
        {
            Vector2 targetPos = finalWaterPosition.position;
            Vector2 currentPos = transform.position;
            
            // Check if we've reached the final position
            if (Vector2.Distance(currentPos, targetPos) < 0.1f)
            {
                hasReachedFinalPosition = true;
                transform.position = targetPos; // Snap to exact position
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                
                // Set underwater = true when Cappa reaches water
                if (cappaAttacks != null)
                {
                    cappaAttacks.SetUnderwater(true);
                }
                
                Debug.Log("Cappa reached final water position - now underwater");
            }
            else
            {
                // Move towards final water position
                Vector2 direction = (targetPos - currentPos).normalized;
                if (rb != null)
                {
                    rb.linearVelocity = direction * fallSpeed;
                }
                else
                {
                    // Fallback if no rigidbody
                    transform.position = Vector2.MoveTowards(currentPos, targetPos, fallSpeed * Time.deltaTime);
                }
            }
        }
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

