using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] Image fadeImage; // The black fade overlay image
    [SerializeField] GameObject firstPart;
    [SerializeField] GameObject secondPart;
    [SerializeField] CinemachineCamera cam;
    [SerializeField] GameObject newPlayer;
    [SerializeField] Transform secondRaft;
    
    [Header("Fade Timing")]
    [SerializeField] private float fadeOutDuration = 0.5f; // Duration of fade out animation
    [SerializeField] private float cameraTransitionDelay = 0.1f; // Additional time to keep screen black during camera switch
    [SerializeField] private float fadeInDuration = 0.5f; // Duration of fade in animation
    [SerializeField] private float fadeInDelay = 0.1f; // Delay before starting fade in
    
    private Vector3 originalPositionDamping;
    private CinemachineFollow followComponent;
    private bool isTransitioning = false; // Prevent multiple transitions
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the CinemachineFollow component
        if (cam != null)
        {
            followComponent = cam.GetComponent<CinemachineFollow>();
            if (followComponent != null)
            {
                originalPositionDamping = followComponent.TrackerSettings.PositionDamping;
            }
        }
        
        // Initialize fade image - make sure it's transparent at start
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true); // Make sure it's active
            
            // Make sure it's on top layer
            Canvas canvas = fadeImage.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 999; // Put it on top
            }
            
            Debug.Log("SceneTransition: Fade image initialized. Alpha set to 0.");
        }
        else
        {
            Debug.LogWarning("SceneTransition: fadeImage is not assigned! Fade effects will not work. Please assign the fade Image in the Inspector.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player") && !isTransitioning)
        {
            isTransitioning = true;
            StartCoroutine(SceneChange());
        }
    }
    
    IEnumerator SceneChange()
    {
        // Fade out (alpha 0 -> 1)
        yield return StartCoroutine(FadeOut());
        
        // Switch scene parts while screen is black
        firstPart.SetActive(false);
        secondPart.SetActive(true);
        
        // Make camera switch instant by setting damping to zero
        if (followComponent != null)
        {
            // Store original damping
            originalPositionDamping = followComponent.TrackerSettings.PositionDamping;
            
            // Set damping to zero for instant transition
            var trackerSettings = followComponent.TrackerSettings;
            trackerSettings.PositionDamping = Vector3.zero;
            followComponent.TrackerSettings = trackerSettings;
        }
        
        // Switch camera target while screen is black
        cam.Follow = newPlayer.transform;
        
        // Wait one frame to let camera snap to new position
        yield return null;
        
        // Restore original damping (optional - you can keep it at zero if you want instant following always)
        if (followComponent != null)
        {
            var trackerSettings = followComponent.TrackerSettings;
            trackerSettings.PositionDamping = originalPositionDamping;
            followComponent.TrackerSettings = trackerSettings;
        }
        
        // Additional delay to keep screen black during camera transition (covers any weird transition)
        yield return new WaitForSeconds(cameraTransitionDelay);
        
        // Update Cappa and other objects while screen is still black
        Cappa cap = FindAnyObjectByType<Cappa>();
        cap.transform.position = new Vector3(secondRaft.transform.position.x, cap.transform.position.y, cap.transform.position.z);
        cap.player = newPlayer.transform;
        FindAnyObjectByType<CappaAttacks>().player = newPlayer.transform;
        
        // Small delay before fading in
        yield return new WaitForSeconds(fadeInDelay);
        
        // Fade in (alpha 1 -> 0)
        yield return StartCoroutine(FadeIn());
        
        yield return new WaitForSeconds(10);
        // cap.spriteRenderer.enabled = true;
        cap.stalling = false;
    }
    
    // Fade out: gradually increase alpha from 0 to 1
    IEnumerator FadeOut()
    {
        if (fadeImage == null)
        {
            Debug.LogError("SceneTransition: fadeImage is NULL! Cannot fade out. Please assign the fade Image in the Inspector.");
            yield break;
        }
        
        // Make sure the image is active and visible
        fadeImage.gameObject.SetActive(true);
        
        // Ensure it's on top (if using Canvas, make sure it has highest sorting order)
        Canvas canvas = fadeImage.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 999; // Put it on top
        }
        
        float elapsedTime = 0f;
        Color startColor = fadeImage.color;
        startColor.a = 0f;
        Color targetColor = fadeImage.color;
        targetColor.a = 1f;
        
        Debug.Log($"FadeOut: Starting fade from alpha {startColor.a} to {targetColor.a} over {fadeOutDuration} seconds");
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        // Ensure it's fully opaque
        fadeImage.color = targetColor;
        Debug.Log($"FadeOut: Complete. Final alpha: {fadeImage.color.a}");
    }
    
    // Fade in: gradually decrease alpha from 1 to 0
    IEnumerator FadeIn()
    {
        if (fadeImage == null)
        {
            Debug.LogError("SceneTransition: fadeImage is NULL! Cannot fade in. Please assign the fade Image in the Inspector.");
            yield break;
        }
        
        float elapsedTime = 0f;
        Color startColor = fadeImage.color;
        startColor.a = 1f; // Start from fully opaque (black)
        Color targetColor = fadeImage.color;
        targetColor.a = 0f; // End at fully transparent
        
        Debug.Log($"FadeIn: Starting fade from alpha {startColor.a} to {targetColor.a} over {fadeInDuration} seconds");
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeInDuration);
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        // Ensure it's fully transparent
        fadeImage.color = targetColor;
        Debug.Log($"FadeIn: Complete. Final alpha: {fadeImage.color.a}");
    }
}
