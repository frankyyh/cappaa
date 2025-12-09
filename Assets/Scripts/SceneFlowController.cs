using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "MainGame";
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string endingSceneName = "EndingScene";

    // Called when the Start button is pressed on the title screen
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Called when Exit button is pressed in either Title or Ending
    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        // So you can test exit while in editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Call this when game ends to show ending screen
    public void GoToEndingScene()
    {
        SceneManager.LoadScene(endingSceneName);
    }

    // Optional: Call this if you want a restart from ending screen
    public void GoToTitleScene()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
