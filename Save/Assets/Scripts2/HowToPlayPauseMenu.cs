using UnityEngine;

public class HowToPlayPauseMenu : MonoBehaviour
{
    public GameObject pauseExitPanel;
    public string mainMenuSceneName = "Main Menu";
    private bool isPaused = false;

    void Start()
    {
        Debug.Log("[HowToPlayPauseMenu] Start() ทำงานแล้วบน: " + gameObject.name);
        if (pauseExitPanel != null)
            pauseExitPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;

            if (pauseExitPanel != null)
                pauseExitPanel.SetActive(isPaused);

            if (isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Debug.Log("[HowToPlayPauseMenu] กด P แล้ว! isPaused = " + isPaused + " | panel = " + (pauseExitPanel != null ? pauseExitPanel.name : "NULL"));
        }
    }
}