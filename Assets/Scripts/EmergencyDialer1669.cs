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
    public bool completeCaseOnDial = true;

    [Header("Optional UI References")]
    [Tooltip("แผงหน้าปัดโทรศัพท์ (จะเปิดเมื่อถือโทรศัพท์อยู่)")]
    public GameObject dialPadUI;

    [Tooltip("ข้อความแสดงตัวเลขที่กำลังกด (TMP)")]
    public TMP_Text dialedNumberText;

    [Tooltip("ข้อความสถานะ/จบเคส (TMP)")]
    public TMP_Text statusText;

    [Header("Events")]
    public UnityEvent onPhonePickedUp;
    public UnityEvent onCaseCompleted;

    private readonly StringBuilder currentInput = new StringBuilder();
    private TestSimpleGrab grabSystem;
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

        RefreshUI("กด E เพื่อหยิบโทรศัพท์");
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
            RefreshUI("กดเลข 1669 เพื่อโทรขอความช่วยเหลือ");
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

        ReadKeyboardDigits();
    }

    private bool IsHoldingThisPhone()
    {
        if (grabSystem == null) return true;
        return grabSystem.IsHolding(transform);
    }

    private void ReadKeyboardDigits()
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

        if (kb.backspaceKey.wasPressedThisFrame)
        {
            Backspace();
        }
    }

    // เรียกจากปุ่ม UI ตัวเลข 0-9 ได้โดยตรง
    public void PressDigit(string digit)
    {
        if (isCompleted || string.IsNullOrEmpty(digit)) return;
        if (requirePhoneHeldFirst && !isPhoneHeld) return;

        currentInput.Append(digit);
        string typed = currentInput.ToString();

        if (typed == targetNumber)
        {
            CompleteEmergencyCall();
            return;
        }

        // ถ้ากดผิดลำดับเมื่อครบความยาวเบอร์เป้าหมาย ให้รีเซ็ตเริ่มกดใหม่
        if (!targetNumber.StartsWith(typed))
        {
            currentInput.Clear();
            RefreshUI($"กดผิดลำดับ ({typed}) กรุณากด {targetNumber} ใหม่");
            return;
        }

        RefreshUI($"กำลังกดเบอร์: {typed}");
    }

    public void Backspace()
    {
        if (isCompleted || currentInput.Length == 0) return;
        currentInput.Length -= 1;
        RefreshUI(currentInput.Length > 0 ? $"กำลังกดเบอร์: {currentInput}" : $"กดเลข {targetNumber}");
    }

    public void ClearNumber()
    {
        currentInput.Clear();
        RefreshUI($"กดเลข {targetNumber}");
    }

    private void CompleteEmergencyCall()
    {
        isCompleted = true;
        RefreshUI($"โทร {targetNumber} สำเร็จ! จบภารกิจ Case 2");

        if (completeCaseOnDial)
        {
            onCaseCompleted?.Invoke();
        }
    }

    private void RefreshUI(string msg)
    {
        if (dialedNumberText != null)
        {
            dialedNumberText.text = currentInput.Length > 0 ? currentInput.ToString() : "----";
        }

        if (statusText != null)
        {
            statusText.text = msg;
        }
    }
}