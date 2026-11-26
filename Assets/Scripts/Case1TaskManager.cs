using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Case1TaskManager : MonoBehaviour
{
    public static Case1TaskManager Instance { get; private set; }

    [Header("Task Completion States (Case 1: Arm Fracture)")]
    [SerializeField] private bool task1AssessedArm = false;
    [SerializeField] private bool task2NewspaperPlaced = false;
    [SerializeField] private bool task3ClothWrapped = false;
    [SerializeField] private bool task4Called1669 = false;

    [Header("UI References (Optional)")]
    [SerializeField] private TMP_Text taskHeaderText;
    [SerializeField] private TMP_Text taskListText;
    [SerializeField] private TMP_Text warningTextUI;
    [SerializeField] private GameObject warningPanelUI;
    [SerializeField] private float hideWarningAfterSeconds = 2.5f;

    [Header("Events")]
    public UnityEvent onTask1Completed;
    public UnityEvent onTask2Completed;
    public UnityEvent onTask3Completed;
    public UnityEvent onTask4Completed;
    public UnityEvent onAllTasksCompleted;

    private float warningTimer = 0f;

    public bool IsTask1Completed => task1AssessedArm;
    public bool IsTask2Completed => task2NewspaperPlaced;
    public bool IsTask3Completed => task3ClothWrapped;
    public bool IsTask4Completed => task4Called1669;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (warningPanelUI != null)
            warningPanelUI.SetActive(false);

        RefreshTaskUI();
    }

    private void Update()
    {
        if (warningTimer > 0f)
        {
            warningTimer -= Time.deltaTime;
            if (warningTimer <= 0f)
            {
                HideWarning();
            }
        }
    }

    public bool CanPerformTask(int taskNumber)
    {
        switch (taskNumber)
        {
            case 1:
                if (task1AssessedArm)
                {
                    ShowWarning("Arm assessment is already completed.");
                    return false;
                }
                return true;

            case 2:
                if (!task1AssessedArm)
                {
                    ShowWarning("Step 1 required: Press [A] on the injured arm to assess first!");
                    return false;
                }
                if (task2NewspaperPlaced)
                {
                    ShowWarning("Newspaper splint is already placed under the arm.");
                    return false;
                }
                return true;

            case 3:
                if (!task1AssessedArm)
                {
                    ShowWarning("Step 1 required: Assess the injured arm first!");
                    return false;
                }
                if (!task2NewspaperPlaced)
                {
                    ShowWarning("Wrong order! Place newspaper under the arm before wrapping with cloth.");
                    return false;
                }
                if (task3ClothWrapped)
                {
                    ShowWarning("Cloth wrap is already applied.");
                    return false;
                }
                return true;

            case 4:
                if (!task1AssessedArm || !task2NewspaperPlaced || !task3ClothWrapped)
                {
                    ShowWarning("Complete steps 1–3 (Assess -> Newspaper -> Cloth wrap) before calling 1669!");
                    return false;
                }
                if (task4Called1669)
                {
                    ShowWarning("Emergency 1669 has already been called.");
                    return false;
                }
                return true;

            default:
                return false;
        }
    }

    public bool CompleteTask1_AssessArm()
    {
        if (!CanPerformTask(1)) return false;

        task1AssessedArm = true;
        HideWarning();
        RefreshTaskUI();
        onTask1Completed?.Invoke();
        return true;
    }

    public bool CompleteTask2_PlaceNewspaper()
    {
        if (!CanPerformTask(2)) return false;

        task2NewspaperPlaced = true;
        HideWarning();
        RefreshTaskUI();
        onTask2Completed?.Invoke();
        return true;
    }

    public bool CompleteTask3_WrapCloth()
    {
        if (!CanPerformTask(3)) return false;

        task3ClothWrapped = true;
        HideWarning();
        RefreshTaskUI();
        onTask3Completed?.Invoke();
        return true;
    }

    public bool CompleteTask4_Call1669()
    {
        if (!CanPerformTask(4)) return false;

        task4Called1669 = true;
        HideWarning();
        RefreshTaskUI();
        onTask4Completed?.Invoke();
        onAllTasksCompleted?.Invoke();

        if (CaseQuizSystem.Instance != null)
        {
            CaseQuizSystem.Instance.StartArmFractureQuiz();
        }
        else
        {
            Debug.LogWarning("[Case1TaskManager] CaseQuizSystem.Instance not found in scene.");
        }

        return true;
    }

    public void ShowWarning(string message)
    {
        if (warningPanelUI != null)
            warningPanelUI.SetActive(true);

        if (warningTextUI != null)
        {
            warningTextUI.gameObject.SetActive(true);
            warningTextUI.text = message;
        }

        warningTimer = hideWarningAfterSeconds;
    }

    public void HideWarning()
    {
        warningTimer = 0f;

        if (warningPanelUI != null)
            warningPanelUI.SetActive(false);
        else if (warningTextUI != null)
            warningTextUI.gameObject.SetActive(false);
    }

    public void RefreshTaskUI()
    {
        if (taskHeaderText != null)
            taskHeaderText.text = "CASE 1: ARM FRACTURE";

        if (taskListText == null) return;

        string s1 = task1AssessedArm ? "done" : "current";
        string s2 = task2NewspaperPlaced ? "done" : (task1AssessedArm ? "available" : "locked");
        string s3 = task3ClothWrapped ? "done" : (task2NewspaperPlaced ? "available" : "locked");
        string s4 = task4Called1669 ? "done" : (task3ClothWrapped ? "available" : "locked");

        taskListText.text =
            $"1. Assess Injured Arm [A] - {s1}\n" +
            $"2. Place Newspaper Splint - {s2}\n" +
            $"3. Wrap Cloth to Splint - {s3}\n" +
            $"4. Call 1669 - {s4}";
    }
}
