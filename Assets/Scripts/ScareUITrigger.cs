using UnityEngine;
using UnityEngine.UI;

public class ShowTextOnTrigger : MonoBehaviour
{
    public GameObject textUI;  // Drag your UI Text object here in Inspector
    private bool playerInside = false;

    void Start()
    {
        // Check if textUI is assigned
        if (textUI == null)
        {
            Debug.LogError($"ShowTextOnTrigger on {gameObject.name}: textUI is not assigned! Please assign it in the Inspector.");
            return;
        }
        
        // Check if this GameObject has a trigger collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"ShowTextOnTrigger on {gameObject.name}: No Collider2D found! Please add a Collider2D component.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"ShowTextOnTrigger on {gameObject.name}: Collider2D is not set as Trigger! Please enable 'Is Trigger' in the Collider2D component.");
        }
        
        textUI.SetActive(false); // Hide at start
    }

    void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            if (textUI != null)
            {
                textUI.SetActive(false); // Hide when pressing E
            }
            playerInside = false;    // Disable further interaction
            gameObject.SetActive(false); // Remove trigger so it never shows again
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"ShowTextOnTrigger: OnTriggerEnter2D called with {other.name}, Tag: {other.tag}");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log($"ShowTextOnTrigger: Player entered trigger on {gameObject.name}");
            
            if (textUI == null)
            {
                Debug.LogError($"ShowTextOnTrigger on {gameObject.name}: textUI is null! Cannot show text.");
                return;
            }
            
            textUI.SetActive(true);
            playerInside = true;
        }
        else
        {
            Debug.LogWarning($"ShowTextOnTrigger: Object {other.name} entered trigger but doesn't have 'Player' tag. Current tag: {other.tag}");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"ShowTextOnTrigger: Player exited trigger on {gameObject.name}");
            
            if (textUI != null)
            {
                textUI.SetActive(false);
            }
            playerInside = false;
        }
    }
}
