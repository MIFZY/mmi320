using System.Text;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Case2TaskManager : MonoBehaviour
{
    [Header("Panel UI References")]
    public GameObject taskPanelUI;
    public TMP_Text taskHeaderText;
    public TMP_Text taskListText;

    [Header("Step Labels (English to avoid missing Thai glyphs)")]
    public string headerLabel = "CASE 2: SEVERED FINGER";
    public string step1Label = "1. Safety Check (Turn off stove)";
    public string step2Label = "2. Stop Bleeding (Gauze)";
    public string step3Label = "3. Put Severed Finger in Bag";
    public string step4Label = "4. Put Ice into Bucket";
    public string step5Label = "5. Place Bag in Ice Bucket";
    public string step6Label = "6. Call 1669";

    [Header("Order Rules")]
    [Tooltip("บังคับทำเรียงทีละขั้น 1 -> 2 -> 3 -> 4 -> 5 -> 6 ห้ามข้ามขั้น")]
    public bool enforceStepOrder = true;

    [Header("Completion States")]
    public bool step1SafetyDone = false;
    public bool step2BleedingDone = false;
    public bool step3BagFingerDone = false;
    public bool step4IceInBucketDone = false;
    public bool step5SoakInIceDone = false;
    public bool step6Call1669Done = false;

    [Header("Events")]
    public UnityEvent onAllTasksCompleted;

    private readonly StringBuilder sb = new StringBuilder();

    public bool CanCallEmergencyNow =>
        !enforceStepOrder || (step1SafetyDone && step2BleedingDone && step3BagFingerDone && step4IceInBucketDone && step5SoakInIceDone);

    private void Start()
    {
        if (taskPanelUI != null)
        {
            taskPanelUI.SetActive(true);
        }

        if (taskHeaderText != null)
        {
            taskHeaderText.text = headerLabel;
        }

        RefreshTaskUI();
    }

    // ขั้นที่ 1: ประเมินความปลอดภัย / ปิดเตาแก๊ส
    public void CompleteStep1_Safety()
    {
        if (step1SafetyDone) return;
        step1SafetyDone = true;
        RefreshTaskUI();
    }

    // ขั้นที่ 2: ใช้ผ้าก๊อซกดห้ามเลือด
    public void CompleteStep2_StopBleeding()
    {
        if (step2BleedingDone) return;
        if (enforceStepOrder && !step1SafetyDone) return;
        step2BleedingDone = true;
        RefreshTaskUI();
    }

    // ขั้นที่ 3: เก็บนิ้วใส่ถุง
    public void CompleteStep3_BagFinger()
    {
        if (step3BagFingerDone) return;
        if (enforceStepOrder && !step2BleedingDone) return;
        step3BagFingerDone = true;
        RefreshTaskUI();
    }

    // ขั้นที่ 4: เอาน้ำแข็งใส่ถัง
    public void CompleteStep4_IceInBucket()
    {
        if (step4IceInBucketDone) return;
        if (enforceStepOrder && !step3BagFingerDone) return;
        step4IceInBucketDone = true;
        RefreshTaskUI();
    }

    // ขั้นที่ 5: แช่ในภาชนะน้ำแข็ง
    public void CompleteStep5_SoakInIce()
    {
        if (step5SoakInIceDone) return;
        if (enforceStepOrder && !step4IceInBucketDone) return;
        step5SoakInIceDone = true;
        RefreshTaskUI();
    }

    // ขั้นที่ 6: โทร 1669 (ขั้นตอนสุดท้าย ข้ามขั้นไม่ได้)
    public void CompleteStep6_Call1669()
    {
        if (step6Call1669Done) return;
        if (!CanCallEmergencyNow) return;

        step6Call1669Done = true;
        RefreshTaskUI();
        onAllTasksCompleted?.Invoke();
    }

    public void RefreshTaskUI()
    {
        if (taskListText == null) return;

        sb.Clear();
        sb.AppendLine(FormatStep(step1Label, step1SafetyDone, true));
        sb.AppendLine(FormatStep(step2Label, step2BleedingDone, step1SafetyDone));
        sb.AppendLine(FormatStep(step3Label, step3BagFingerDone, step2BleedingDone));
        sb.AppendLine(FormatStep(step4Label, step4IceInBucketDone, step3BagFingerDone));
        sb.AppendLine(FormatStep(step5Label, step5SoakInIceDone, step4IceInBucketDone));
        sb.AppendLine(FormatStep(step6Label, step6Call1669Done, step5SoakInIceDone));

        taskListText.text = sb.ToString();
    }

    private string FormatStep(string label, bool isDone, bool isUnlocked)
    {
        if (isDone)
        {
            return $"{label} - <b>done</b>";
        }
        if (!enforceStepOrder || isUnlocked)
        {
            return $"{label} - in progress";
        }
        return $"{label} - locked";
    }
}