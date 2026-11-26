using UnityEngine;
using UnityEngine.SceneManagement;

public class ForceExitToMenu : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Main Menu";
    [SerializeField] private GameObject resultPanelToWatch;
    [SerializeField] private KeyCode fallbackKey = KeyCode.M;

    private bool triggered = false;

    private void Update()
    {
        if (triggered) return;

        bool resultIsOpen = resultPanelToWatch == null || resultPanelToWatch.activeInHierarchy;
        if (!resultIsOpen) return;

        if (Input.GetKeyDown(fallbackKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ExitNow();
        }
    }

    public void ExitNow()
    {
        if (triggered) return;
        triggered = true;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CaseQuizSystem.Instance != null)
        {
            Destroy(CaseQuizSystem.Instance.gameObject);
        }

        SceneManager.LoadScene(menuSceneName.Trim());
    }
}