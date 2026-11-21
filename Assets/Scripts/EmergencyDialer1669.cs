using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class EmergencyDialer1669 : MonoBehaviour
{
    [Header("Dial Settings")]
    public string targetNumber = "1669";
    public bool requirePhoneHeldFirst = true;
    public float wrongNumberResetDelay = 1.0f;

    [Header("End Game Settings")]
    [Tooltip("End game UI panel (optional) shown when 1669 is called")]
    public GameObject endGameUI;

    [Tooltip("End game label (optional)")]
    public TMP_Text endGameText;

    public string endGameMessage = "GAME OVER";

    [Tooltip("Stop Play Mode or quit application after delay when completed")]
    public bool quitOrStopPlayAfterEnd = false;
    public float endDelayBeforeStop = 1.5f;

    [Header("Phone UI References")]
    public GameObject dialPadUI;
    public TMP_Text dialedNumberText;
    public TMP_Text statusText;

    [Header("Events")]
    public UnityEvent onPhonePickedUp;
    public UnityEvent onWrongNumber;
    public UnityEvent onCaseCompleted;

    private readonly StringBuilder currentInput = new StringBuilder();
    private TestSimpleGrab grabSystem;
    private Coroutine resetRoutine;
    private bool isPhoneHeld = false;
    private bool isCompleted = false;

    public bool IsCompleted => isCompleted;

    private void Awake()
    {
        grabSystem = FindAnyObjectByType<TestSimpleGrab>();

        if (dialPadUI != null)
        {
            dialPadUI.SetActive(false);
        }

        if (endGameUI != null)
        {
            endGameUI.SetActive(false);
        }

        RefreshNumberDisplay();
        SetStatus("Press E to pick up phone");
    }

    private void Update()
    {
        if (isCompleted) return;

        bool heldNow = IsHoldingThisPhone();
        if (heldNow && !isPhoneHeld)
        {
            isPhoneHeld = true;
            if (dialPadUI != null)
            {
                dialPadUI.SetActive(true);
            }
            RefreshNumberDisplay();
            SetStatus($"Dial {targetNumber} and press CALL");
            onPhonePickedUp?.Invoke();
        }
        else if (!heldNow && isPhoneHeld)
        {
            isPhoneHeld = false;
            if (dialPadUI != null)
            {
                dialPadUI.SetActive(false);
            }
            ClearNumber();
        }

        if (requirePhoneHeldFirst && !isPhoneHeld) return;

        ReadKeyboardInput();
    }

    private bool IsHoldingThisPhone()
    {
        if (grabSystem == null) return true;
        return grabSystem.IsHolding(transform);
    }

    private void ReadKeyboardInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) PressDigit("1");
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) PressDigit("2");
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) PressDigit("3");
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) PressDigit("4");
        if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) PressDigit("5");
        if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) PressDigit("6");
        if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) PressDigit("7");
        if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) PressDigit("8");
        if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) PressDigit("9");
        if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) PressDigit("0");

        if (kb.backspaceKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame)
        {
            Backspace();
        }

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            CallNumber();
        }
    }

    // Called by keypad buttons 0-9
    public void PressDigit(string digit)
    {
        if (isCompleted || string.IsNullOrEmpty(digit)) return;
        if (requirePhoneHeldFirst && !isPhoneHeld) return;

        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
            currentInput.Clear();
        }

        currentInput.Append(digit);
        RefreshNumberDisplay();
        SetStatus("Press CALL");
    }

    // Called by CALL button
    public void CallNumber()
    {
        if (isCompleted) return;
        if (requirePhoneHeldFirst && !isPhoneHeld) return;

        if (currentInput.ToString() == targetNumber)
        {
            CompleteGame();
        }
        else
        {
            TriggerWrongNumber();
        }
    }

    // Called by X button (deletes 1 last digit)
    public void Backspace()
    {
        if (isCompleted) return;
        if (requirePhoneHeldFirst && !isPhoneHeld) return;

        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }

        if (currentInput.Length > 0)
        {
            currentInput.Length -= 1;
            RefreshNumberDisplay();
            SetStatus(currentInput.Length > 0 ? "Press CALL" : $"Dial {targetNumber} and press CALL");
        }
        else
        {
            RefreshNumberDisplay();
            SetStatus($"Dial {targetNumber} and press CALL");
        }
    }

    public void ClearNumber()
    {
        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }

        currentInput.Clear();
        RefreshNumberDisplay();
        SetStatus($"Dial {targetNumber} and press CALL");
    }

    private void TriggerWrongNumber()
    {
        onWrongNumber?.Invoke();
        SetStatus("worng number");

        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
        }
        resetRoutine = StartCoroutine(ResetAfterWrongNumber());
    }

    private IEnumerator ResetAfterWrongNumber()
    {
        yield return new WaitForSeconds(wrongNumberResetDelay);
        currentInput.Clear();
        RefreshNumberDisplay();
        SetStatus($"Dial {targetNumber} and press CALL");
        resetRoutine = null;
    }

    private void CompleteGame()
    {
        isCompleted = true;

        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }

        if (dialedNumberText != null)
        {
            dialedNumberText.text = targetNumber;
        }

        SetStatus(endGameMessage);

        if (endGameUI != null)
        {
            endGameUI.SetActive(true);
        }

        if (endGameText != null)
        {
            endGameText.text = endGameMessage;
        }

        onCaseCompleted?.Invoke();

        if (quitOrStopPlayAfterEnd)
        {
            StartCoroutine(StopGameRoutine());
        }
    }

    private IEnumerator StopGameRoutine()
    {
        yield return new WaitForSeconds(endDelayBeforeStop);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshNumberDisplay()
    {
        if (dialedNumberText != null)
        {
            dialedNumberText.text = currentInput.Length > 0 ? currentInput.ToString() : "----";
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}