using UnityEngine;
using UnityEngine.UI;

public class ShowTextOnTrigger : MonoBehaviour
{
    public GameObject textUI;  // Drag your UI Text object here in Inspector
    private bool playerInside = false;

    void Start()
    {
        textUI.SetActive(false); // Hide at start
    }

    void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            textUI.SetActive(false); // Hide when pressing E
            playerInside = false;    // Disable further interaction
            gameObject.SetActive(false); // Remove trigger so it never shows again
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            textUI.SetActive(true);
            playerInside = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            textUI.SetActive(false);
            playerInside = false;
        }
    }
}
