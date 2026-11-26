using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class QuizFinishToMenuButton : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string menuSceneName = "Main Menu";

    [Header("Optional UI To Hide Before Leaving")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private bool destroyQuizSystemOnExit = true;

    private Button button;
    private bool isTransitioning = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(GoToMainMenu);
    }

    private void Update()
    {
        if (resultPanel != null && resultPanel.activeInHierarchy && !isTransitioning)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GoToMainMenu();
            }
        }
    }

    public void GoToMainMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (quizPanel != null) quizPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (destroyQuizSystemOnExit && CaseQuizSystem.Instance != null)
        {
            Destroy(CaseQuizSystem.Instance.gameObject);
        }

        string targetScene = menuSceneName.Trim();
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[QuizFinishToMenuButton] Menu Scene Name is empty.");
            isTransitioning = false;
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning("[QuizFinishToMenuButton] Scene '" + targetScene + "' is not in Build Profiles / Build Settings.");
            isTransitioning = false;
        }
    }
}