using UnityEngine;

public class SplashEffect : MonoBehaviour
{
    private Cappa cappaReference;
    
    /// <summary>
    /// Sets the reference to the Cappa script so we can notify it when splash is done.
    /// </summary>
    public void SetCappaReference(Cappa cappa)
    {
        cappaReference = cappa;
    }
    
    /// <summary>
    /// Called by Animation Event when splash animation finishes.
    /// Notifies Cappa that splash is complete, turns on Cappa's sprite renderer, and deactivates the splash GameObject.
    /// </summary>
    public void DeactivateSplash()
    {
        // Notify Cappa that splash animation is complete (this also turns on sprite renderer)
        if (cappaReference != null)
        {
            cappaReference.OnSplashAnimationComplete();
        }
        
        // Deactivate the splash GameObject
        gameObject.SetActive(false);
    }
}

