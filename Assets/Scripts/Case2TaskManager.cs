using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Case2TaskManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text taskHeaderText;
    public TMP_Text taskListText;

    [Header("Audio")]
    [Tooltip("ลากออบเจกต์ที่มีคอมโพเนนต์ GauzeBleedingAudio มาใส่ (ตอนเอาผ้าก๊อซสแนปห้ามเลือดที่มือ Step 2)")]
    public GauzeBleedingAudio gauzeBleedingAudio;

    [Tooltip("ลากออบเจกต์ที่มีคอมโพเนนต์ BagFingerAudio มาใส่ (ตอนเอานิ้วเข้าถุง Step 3)")]
    public BagFingerAudio bagFingerAudio;

    [Tooltip("ลากออบเจกต์ที่มีคอมโพเนนต์ BagInIceBucketAudio มาใส่ (ตอนเอาถุงสแนปลงถังน้ำแข็ง Step 5)")]
    public BagInIceBucketAudio bagInIceBucketAudio;

    [Header("Case 2 Task States (1-6)")]
    public bool step1SafetyDone = false;
    public bool step2BleedingDone = false;
    public bool step3FingerBaggedDone = false;
    public bool step4IceInBucketDone = false;
    public bool step5SoakedInIceDone = false;
    public bool step6Called1669Done = false;

    [Header("Rule Settings")]
    [Tooltip("ล็อคเด็ดขาดให้ทำ Step 1 ก่อนเสมอ ไม่งั้นบล็อกการทำขั้น 2-6")]
    public bool lockStep1First = true;

    [Tooltip("ขั้น 2-5 ทำสลับลำดับกันได้อิสระหลัง Step 1 เสร็จ")]
    public bool allowFlexibleOrderForSteps2To5 = true;

    [Tooltip("ล็อคให้โทร 1669 (Step 6) เป็นขั้นตอนสุดท้ายหลัง Step 1-5 ครบ")]
    public bool lockStep6Last = true;

    [Header("Events")]
    public UnityEvent onAllTasksCompleted;
    public UnityEvent<string> onBlockedActionMessage;

    public bool IsStep1SafetyDone => step1SafetyDone;

    public bool AreSteps1To5Done =>
        step1SafetyDone &&
        step2BleedingDone &&
        step3FingerBaggedDone &&
        step4IceInBucketDone &&
        step5SoakedInIceDone;

    public bool CanCallEmergencyNow => !lockStep6Last || AreSteps1To5Done;

    private void Start()
    {
        if (gauzeBleedingAudio == null)
        {
            gauzeBleedingAudio = GetComponentInChildren<GauzeBleedingAudio>();
            if (gauzeBleedingAudio == null)
                gauzeBleedingAudio = FindAnyObjectByType<GauzeBleedingAudio>();
        }

        if (bagFingerAudio == null)
        {
            bagFingerAudio = GetComponentInChildren<BagFingerAudio>();
            if (bagFingerAudio == null)
                bagFingerAudio = FindAnyObjectByType<BagFingerAudio>();
        }

        if (bagInIceBucketAudio == null)
        {
            bagInIceBucketAudio = GetComponentInChildren<BagInIceBucketAudio>();
            if (bagInIceBucketAudio == null)
                bagInIceBucketAudio = FindAnyObjectByType<BagInIceBucketAudio>();
        }

        RefreshUI();
    }

    public bool CanPerformSteps2To5(string blockedMessage = "Turn off the stove first!")
    {
        if (lockStep1First && !step1SafetyDone)
        {
            onBlockedActionMessage?.Invoke(blockedMessage);
            return false;
        }
        return true;
    }

    public void CompleteStep1_Safety()
    {
        if (step1SafetyDone) return;
        step1SafetyDone = true;
        RefreshUI();
    }

    public void CompleteStep2_StopBleeding()
    {
        if (step2BleedingDone) return;
        if (!CanPerformSteps2To5("Turn off the stove before stopping bleeding!")) return;

        step2BleedingDone = true;

        if (gauzeBleedingAudio == null)
        {
            gauzeBleedingAudio = GetComponentInChildren<GauzeBleedingAudio>();
            if (gauzeBleedingAudio == null)
                gauzeBleedingAudio = FindAnyObjectByType<GauzeBleedingAudio>();
        }

        if (gauzeBleedingAudio != null)
        {
            gauzeBleedingAudio.PlayGauzeSound();
        }

        RefreshUI();
    }

    public void CompleteStep3_BagFinger()
    {
        if (step3FingerBaggedDone) return;
        if (!CanPerformSteps2To5("Turn off the stove before bagging the finger!")) return;

        step3FingerBaggedDone = true;

        if (bagFingerAudio == null)
        {
            bagFingerAudio = GetComponentInChildren<BagFingerAudio>();
            if (bagFingerAudio == null)
                bagFingerAudio = FindAnyObjectByType<BagFingerAudio>();
        }

        if (bagFingerAudio != null)
        {
            bagFingerAudio.PlayFingerInBagSound();
        }

        RefreshUI();
    }

    public void CompleteStep4_IceInBucket()
    {
        if (step4IceInBucketDone) return;
        if (!CanPerformSteps2To5("Turn off the stove before getting ice!")) return;

        step4IceInBucketDone = true;
        RefreshUI();
    }

    public void CompleteStep5_SoakInIce()
    {
        if (step5SoakedInIceDone) return;
        if (!CanPerformSteps2To5("Turn off the stove before soaking in ice!")) return;

        step5SoakedInIceDone = true;

        if (bagInIceBucketAudio == null)
        {
            bagInIceBucketAudio = GetComponentInChildren<BagInIceBucketAudio>();
            if (bagInIceBucketAudio == null)
                bagInIceBucketAudio = FindAnyObjectByType<BagInIceBucketAudio>();
        }

        if (bagInIceBucketAudio != null)
        {
            bagInIceBucketAudio.PlayBagInIceSound();
        }

        RefreshUI();
    }

    public void CompleteStep6_Call1669()
    {
        if (step6Called1669Done) return;

        if (lockStep6Last && !AreSteps1To5Done)
        {
            onBlockedActionMessage?.Invoke("Complete steps 1-5 before calling 1669!");
            return;
        }

        step6Called1669Done = true;
        RefreshUI();
        onAllTasksCompleted?.Invoke();
    }

    public void RefreshUI()
    {
        if (taskHeaderText != null)
        {
            taskHeaderText.text = "CASE 2: SEVERED FINGER";
        }

        if (taskListText == null) return;

        string s1 = step1SafetyDone ? "done" : "current";
        string s2 = step2BleedingDone ? "done" : (!step1SafetyDone && lockStep1First ? "locked" : "available");
        string s3 = step3FingerBaggedDone ? "done" : (!step1SafetyDone && lockStep1First ? "locked" : "available");
        string s4 = step4IceInBucketDone ? "done" : (!step1SafetyDone && lockStep1First ? "locked" : "available");
        string s5 = step5SoakedInIceDone ? "done" : (!step1SafetyDone && lockStep1First ? "locked" : "available");
        string s6 = step6Called1669Done ? "done" : (AreSteps1To5Done ? "current" : "locked");

        taskListText.text =
            $"1. Safety Check (Turn off stove) - {s1}\n" +
            $"2. Stop Bleeding (Gauze) - {s2}\n" +
            $"3. Put Severed Finger in Bag - {s3}\n" +
            $"4. Put Ice into Bucket - {s4}\n" +
            $"5. Place Bag in Ice Bucket - {s5}\n" +
            $"6. Call 1669 - {s6}";
    }
}