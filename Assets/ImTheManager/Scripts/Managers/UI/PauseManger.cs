using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject hud;

    bool isPaused = false;

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
            TogglePause();
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        pausePanel.SetActive(isPaused);
        if (hud != null) hud.SetActive(!isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        if (hud != null) hud.SetActive(true);
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}