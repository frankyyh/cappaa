using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScareUITrigger : MonoBehaviour
{
    [Header("UI Settings")]
    public CanvasGroup uiCanvasGroup;    // Assign your UI panel here
    public KeyCode scareKey = KeyCode.E;
    public float fadeDuration = 0.5f;

    private bool isPlayerInside = false;
    private bool hasBeenUsed = false;

    private void Start()
    {
        uiCanvasGroup.alpha = 0f; // Hide at start
        uiCanvasGroup.interactable = false;
        uiCanvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (isPlayerInside && !hasBeenUsed)
        {
            if (Input.GetKeyDown(scareKey))
            {
                hasBeenUsed = true;
                StartCoroutine(FadeOutUI());
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasBeenUsed)
        {
            isPlayerInside = true;
            StartCoroutine(FadeInUI());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasBeenUsed)
        {
            isPlayerInside = false;
            StartCoroutine(FadeOutUI());
        }
    }

    private IEnumerator FadeInUI()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            uiCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        uiCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutUI()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            uiCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        uiCanvasGroup.alpha = 0f;

        if (hasBeenUsed)
        {
            uiCanvasGroup.gameObject.SetActive(false); // permanently disable
        }
    }
}
