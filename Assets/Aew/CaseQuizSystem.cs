using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class CaseQuizItem
{
    [TextArea(2, 4)]
    public string question;
    public string[] choices = new string[3];
    public int correctIndex; // 0, 1, or 2
}

public class CaseQuizSystem : MonoBehaviour
{
    public static CaseQuizSystem Instance;

    [Header("Quiz UI")]
    public GameObject quizPanel;
    public TMP_Text roundTitleText;
    public TMP_Text questionText;
    public TMP_Text progressText;
    public Button[] choiceButtons;
    public TMP_Text[] choiceLabels;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button continueButton;
    public TMP_Text continueButtonLabel;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip answerClickSound;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip finishClickSound;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("HUD & UI To Hide During Quiz")]
    public GameObject taskHudObject;
    public GameObject[] extraObjectsToHide;

    [Header("Round 1: Arm Fracture (4 Questions)")]
    public List<CaseQuizItem> armFractureQuestions = new List<CaseQuizItem>();

    [Header("Round 2: Finger Amputation (Optional / Backup)")]
    public List<CaseQuizItem> fingerAmputationQuestions = new List<CaseQuizItem>();

    [Header("Menu Scene Settings (Fill When Ready)")]
    public string menuSceneName = "Main Menu";

    private List<CaseQuizItem> currentList = new List<CaseQuizItem>();
    private int index = 0;
    private int score = 0;
    private int roundNumber = 1;
    private bool isLoadingMenuScene = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Each case owns its question set and UI; keep the quiz scene-local.

        EnsureAudioSource();
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshSceneUiReferences();

        if (quizPanel != null) quizPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.playOnAwake = false;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSource();
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoadingMenuScene = false;
        EnsureAudioSource();
        RefreshSceneUiReferences();

        if (quizPanel != null) quizPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void RefreshSceneUiReferences()
    {
        GameObject foundQuizBox = FindObjectInActiveScene("QuizBox");
        if (foundQuizBox != null) quizPanel = foundQuizBox;

        GameObject foundResultPanel = FindObjectInActiveScene("ResultPanel");
        if (foundResultPanel != null) resultPanel = foundResultPanel;

        GameObject foundTaskCanvas = FindObjectInActiveScene("TaskCanvas");
        if (foundTaskCanvas != null) taskHudObject = foundTaskCanvas;

        roundTitleText = FindTmpTextInObjectOrChildren("RoundTitleText");
        questionText = FindTmpTextInObjectOrChildren("QuestionText");
        progressText = FindTmpTextInObjectOrChildren("ProgressText");
        resultText = FindTmpTextInObjectOrChildren("ResultText");

        Button foundContinueBtn = FindComponentInActiveScene<Button>("ContinueButton");
        if (foundContinueBtn != null)
        {
            continueButton = foundContinueBtn;
            continueButtonLabel = FindBestTmpTextInChildren(foundContinueBtn.gameObject);
        }

        string[] choiceNames = { "Choice0", "Choice1", "Choice2" };
        Button[] foundButtons = new Button[choiceNames.Length];
        TMP_Text[] foundLabels = new TMP_Text[choiceNames.Length];
        int foundCount = 0;

        for (int i = 0; i < choiceNames.Length; i++)
        {
            Button btn = FindComponentInActiveScene<Button>(choiceNames[i]);
            if (btn != null)
            {
                foundButtons[i] = btn;
                foundLabels[i] = FindBestTmpTextInChildren(btn.gameObject);
                foundCount++;
            }
        }

        if (foundCount > 0)
        {
            choiceButtons = foundButtons;
            choiceLabels = foundLabels;
        }
    }

    private TMP_Text FindTmpTextInObjectOrChildren(string targetName)
    {
        GameObject obj = FindObjectInActiveScene(targetName);
        if (obj == null) return null;

        TMP_Text direct = obj.GetComponent<TMP_Text>();
        if (direct != null) return direct;

        return FindBestTmpTextInChildren(obj);
    }

    private TMP_Text FindBestTmpTextInChildren(GameObject parent)
    {
        TMP_Text[] allTexts = parent.GetComponentsInChildren<TMP_Text>(true);
        if (allTexts == null || allTexts.Length == 0) return null;

        for (int i = 0; i < allTexts.Length; i++)
        {
            if (allTexts[i].gameObject.activeInHierarchy) return allTexts[i];
        }
        return allTexts[0];
    }

    private GameObject FindObjectInActiveScene(string targetName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == targetName) return roots[i];

            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j].name == targetName) return children[j].gameObject;
            }
        }
        return null;
    }

    private T FindComponentInActiveScene<T>(string targetName) where T : Component
    {
        GameObject obj = FindObjectInActiveScene(targetName);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    // Called when Arm Fracture case is completed
    public void StartArmFractureQuiz()
    {
        RefreshSceneUiReferences();
        roundNumber = 1;
        isLoadingMenuScene = false;
        currentList = armFractureQuestions;
        BeginRound("Round 1: Arm Fracture Quiz");
    }

    // Called when Finger Amputation case is completed
    public void StartFingerAmputationQuiz()
    {
        RefreshSceneUiReferences();
        roundNumber = 2;
        isLoadingMenuScene = false;
        currentList = fingerAmputationQuestions;
        BeginRound("Round 2: Finger Amputation Quiz");
    }

    private void BeginRound(string title)
    {
        index = 0;
        score = 0;
        if (roundTitleText != null) roundTitleText.text = title;
        if (resultPanel != null) resultPanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);

        SetTaskHudActive(false);
        HideConflictingSceneUi();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowCurrentQuestion();
    }

    private void HideConflictingSceneUi()
    {
        string[] commonUiNames = { "PhoneDialCanvas", "PauseCanvas", "GameOverCanvas", "CompleteCanvas" };
        for (int i = 0; i < commonUiNames.Length; i++)
        {
            GameObject uiObj = FindObjectInActiveScene(commonUiNames[i]);
            if (uiObj != null)
            {
                uiObj.SetActive(false);
            }
        }

        if (extraObjectsToHide != null)
        {
            for (int i = 0; i < extraObjectsToHide.Length; i++)
            {
                if (extraObjectsToHide[i] != null)
                {
                    extraObjectsToHide[i].SetActive(false);
                }
            }
        }

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Canvas[] canvases = roots[i].GetComponentsInChildren<Canvas>(false);
            for (int j = 0; j < canvases.Length; j++)
            {
                GameObject canvasObj = canvases[j].gameObject;
                if (canvasObj.name == "QuizOverlayCanvas") continue;
                if (quizPanel != null && canvasObj.transform.IsChildOf(quizPanel.transform)) continue;
                if (resultPanel != null && canvasObj.transform.IsChildOf(resultPanel.transform)) continue;

                canvasObj.SetActive(false);
            }
        }
    }

    private void ShowCurrentQuestion()
    {
        if (currentList == null || index >= currentList.Count)
        {
            ShowRoundResult();
            return;
        }

        CaseQuizItem q = currentList[index];
        if (questionText != null) questionText.text = q.question;
        if (progressText != null) progressText.text = "Question " + (index + 1) + " / " + currentList.Count;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null) continue;

            bool active = q.choices != null && i < q.choices.Length;
            choiceButtons[i].gameObject.SetActive(active);
            if (!active) continue;

            SetButtonText(choiceButtons[i], i, q.choices[i]);

            int picked = i;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => SelectAnswer(picked));
        }
    }

    private void SetButtonText(Button btn, int idx, string textValue)
    {
        if (choiceLabels != null && idx < choiceLabels.Length && choiceLabels[idx] != null)
        {
            choiceLabels[idx].text = textValue;
        }

        TMP_Text[] tmps = btn.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            tmps[i].text = textValue;
        }

        Text[] legacyTexts = btn.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyTexts.Length; i++)
        {
            legacyTexts[i].text = textValue;
        }
    }

    private void SelectAnswer(int picked)
    {
        if (currentList == null || index >= currentList.Count) return;

        PlaySfx(answerClickSound);

        if (picked == currentList[index].correctIndex)
        {
            score++;
            PlaySfx(correctSound);
        }
        else
        {
            PlaySfx(wrongSound);
        }

        index++;
        ShowCurrentQuestion();
    }

    private void ShowRoundResult()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);

        SetTaskHudActive(false);
        HideConflictingSceneUi();

        int total = currentList != null ? currentList.Count : 0;
        string status = score >= 3 ? "Pass" : "Review Needed";

        if (resultText != null)
        {
            resultText.text = roundNumber == 1
                ? "Round 1 Summary (Arm Fracture)\nScore: " + score + " / " + total + " (" + status + ")"
                : "Round 2 Summary (Finger Amputation)\nScore: " + score + " / " + total + " (" + status + ")";
        }

        if (continueButton != null)
        {
            continueButton.interactable = true;
            continueButton.onClick.RemoveAllListeners();
            ApplyFinishButtonStyleAndText(continueButton, continueButtonLabel, "Finish");
            continueButton.onClick.AddListener(OnFinishQuizClicked);
        }
    }

    private void ApplyFinishButtonStyleAndText(Button btn, TMP_Text label, string textValue)
    {
        TMP_FontAsset referenceFont = resultText != null ? resultText.font : null;

        if (label != null)
        {
            if (referenceFont != null) label.font = referenceFont;
            label.text = textValue;
        }

        TMP_Text[] tmps = btn.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (referenceFont != null) tmps[i].font = referenceFont;
            tmps[i].text = textValue;
        }

        Text[] legacyTexts = btn.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyTexts.Length; i++)
        {
            legacyTexts[i].text = textValue;
        }
    }

    public void OnFinishQuizClicked()
    {
        PlaySfx(finishClickSound);

        if (resultPanel != null) resultPanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            SetTaskHudActive(true);
            return;
        }

        if (isLoadingMenuScene) return;
        isLoadingMenuScene = true;

        string targetScene = menuSceneName.Trim();
        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning("[CaseQuizSystem] Menu scene '" + targetScene + "' is not in Build Settings or not created yet. Hiding result panel instead.");
            isLoadingMenuScene = false;
            SetTaskHudActive(true);
        }
    }

    void Update()
    {
        if (resultPanel != null && resultPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnFinishQuizClicked();
            }
        }
    }

    private void SetTaskHudActive(bool active)
    {
        if (taskHudObject != null)
        {
            taskHudObject.SetActive(active);
        }
    }
}