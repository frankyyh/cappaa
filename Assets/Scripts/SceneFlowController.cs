using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowController : MonoBehaviour
{
    public static SceneFlowController Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string gameSceneName = "MainGame";
    [SerializeField] private string endingSceneName = "EndingScene";

    private void Awake()
    {
        // Simple singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // optional, so it persists across scenes
    }

    // ---- Button Methods ----

    // Called by Start button on Title screen
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Called by Exit button (Title or Ending)
    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Optional: From Ending screen, go back to title
    public void GoToTitleScene()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    // ---- Game Flow Methods ----

    // Call this when your gameplay is finished
    public void TriggerGameEnd()
    {
        SceneManager.LoadScene(endingSceneName);
    }
}
