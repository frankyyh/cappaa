using UnityEngine;
using System.Collections;
using System.Linq.Expressions;

public class Raft : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("Destination")]
    [SerializeField] private Transform destination; // Where the raft moves to
    
    [Header("References")]
    [SerializeField] private Collider2D raftDetectionCollider; // Child collider to detect player
    [SerializeField] private Transform player;
    [SerializeField] private Transform cappa;
    [SerializeField] BGMover bgMover;
    [SerializeField] Rigidbody2D _rb;
    
    private bool isMoving = false;
    private bool hasReachedDestination = false;
    private Vector3 previousRaftPosition;
    public bool canMove;
    
    public float duration = 6;
    
    private void Awake()
    {
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        // Find cappa if not assigned
        if (cappa == null)
        {
            GameObject cappaObj = GameObject.Find("Cappa");
            if (cappaObj != null)
            {
                cappa = cappaObj.transform;
            }
        }
    }
    
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            canMove = true;
        }
        // Check if player is on raft to start movement (one-time check)
        if (!isMoving && canMove)
        {
            CheckPlayerOnRaft();
            
            if (playerOnRaft && !isMoving)
            {
                StartMovement();
            }
        }
        
        // Handle movement
        // if (! && !hasReachedDestination)
        // {
        //     // MoveToDestination();
        //     StartCoroutine(ElevatorMovement());
        // }
    }
    
    private bool playerOnRaft = false;
    
    private void CheckPlayerOnRaft()
    {
        if (raftDetectionCollider == null || player == null)
        {
            playerOnRaft = false;
            return;
        }
        
        // Check if player's collider overlaps with the raft detection collider
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            playerOnRaft = false;
            return;
        }
        
        // Use ContactFilter2D to check for overlap
        ContactFilter2D filter = new ContactFilter2D();
        filter = ContactFilter2D.noFilter;
        Collider2D[] results = new Collider2D[10];
        int count = raftDetectionCollider.Overlap(filter, results);
        
        playerOnRaft = false;
        
        // Check if player collider is in the results
        for (int i = 0; i < count; i++)
        {
            if (results[i] == playerCollider)
            {
                playerOnRaft = true;
                break;
            }
        }
    }
    
    private void StartMovement()
    {
        isMoving = true;
        
        // Store initial raft position
        previousRaftPosition = transform.position;
        
        if (player != null)
        {
            // Enable player WASD movement during raft movement
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.CanMove = true;
                // player.transform.parent = gameObject.transform;
            }
            // bgMover.enabled = true;
        }
        StartCoroutine(MoveRaft());
        Debug.Log("Raft movement started");
    }

    IEnumerator MoveRaft()
    {
        float progress;
        float time = 0f;
        Vector3 curPos;
        player.GetComponent<RelativeJoint2D>().enabled = true;
        player.GetComponent<PlayerController>().animator.SetFloat("Speed", 0);
        player.GetComponent<PlayerController>().enabled = false;
        
        Parallax[] para = FindObjectsByType<Parallax>(FindObjectsSortMode.None);
        foreach(Parallax p in para)
        {
            p.enabled = false;
        }
        while (time<duration)
        {
            progress = time / duration;
            float smoothProgress;
            smoothProgress = SmoothProgress(progress);
            curPos = Vector3.Lerp(previousRaftPosition, destination.position, smoothProgress);
            _rb.MovePosition(curPos);
            time += Time.deltaTime;
            yield return null;
        }
    }

    public float SmoothProgress(float progress)
    {
        progress = Mathf.Lerp(-Mathf.PI / 2, Mathf.PI / 2, progress);
        progress = Mathf.Sin(progress);
        progress = (progress / 2f) + .5f;

        return progress;
    }
    // private void MoveToDestination()
    // {
    //     if (destination == null)
    //     {
    //         Debug.LogWarning("Raft: Destination not assigned!");
    //         return;
    //     }

    //     // Calculate distance to destination
    //     float distance = Vector3.Distance(transform.position, destination.position);

    //     if (distance > 0.1f)
    //     {
    //         // Calculate movement direction
    //         Vector3 direction = (destination.position - transform.position).normalized;
    //         Vector3 movement = direction * moveSpeed * Time.deltaTime;

    //         // Move raft
    //         transform.position += movement;

    //         // Move cappa with raft
    //         if (cappa != null)
    //         {
    //             cappa.position += movement;
    //         }

    //         // Player X position will be synchronized in LateUpdate
    //     }
    //     else
    //     {
    //         // Reached destination
    //         hasReachedDestination = true;

    //         // Re-enable player movement
    //         if (player != null)
    //         {
    //             PlayerController playerController = player.GetComponent<PlayerController>();
    //             if (playerController != null)
    //             {
    //                 playerController.CanMove = true;
    //             }
    //         }
    //         
    //         Debug.Log("Raft reached destination");
    //     }
    // }

    private void LateUpdate()
    {
        // Add raft's movement to player, preserving player's own movement and jumping
        // if (isMoving && !hasReachedDestination && player != null)
        // {
        //     // Calculate how much the raft moved this frame
        //     Vector3 raftMovement = transform.position - previousRaftPosition;
            
        //     // Add raft's movement to player's current position
        //     // This preserves player's own movement (WASD) and jumping (Y physics)
        //     player.position += raftMovement;
            
        //     // Update previous position for next frame
        //     previousRaftPosition = transform.position;
        // }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw raft detection area
        if (raftDetectionCollider != null)
        {
            Gizmos.color = Color.green;
            Bounds bounds = raftDetectionCollider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        
        // Draw destination
        if (destination != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(destination.position, 0.5f);
            Gizmos.DrawLine(transform.position, destination.position);
        }
    }

    

}

