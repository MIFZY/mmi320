using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PhoneHoldDialUI : MonoBehaviour
{
    [Header("Dial UI To Show Only While Holding Phone")]
    [Tooltip("ลาก PhoneDialCanvas (หรือ DialPadPanel) มาใส่ที่นี่")]
    [SerializeField] private GameObject dialCanvasOrPanel;

    [Tooltip("ซ่อนหน้าจอโทรตอนเริ่มเล่นเกม")]
    [SerializeField] private bool hideOnStart = true;

    [Tooltip("ถ้าโทรติด/เสร็จสิ้นภารกิจแล้ว ให้ค้างหน้าจอไว้ต่อ (เว้นไว้หรือตั้งเป็น false ถ้าอยากให้ซ่อนตอนวาง)")]
    [SerializeField] private bool keepOpenAfterCallConnected = false;

    [Header("Physics & Hold Detection")]
    [SerializeField] private Rigidbody phoneRigidbody;

    [Tooltip("เปิด Is Kinematic ชั่วคราวตอนถือ เพื่อไม่ให้ฟิสิกส์ดีด/ร่วงขณะกำลังยกกดเบอร์")]
    [SerializeField] private bool kinematicWhileHeld = true;

    [Tooltip("ระยะห่างสูงสุดจากจุดถือ/ผู้เล่น ที่ถือว่ากำลังถือโทรศัพท์อยู่ (เผื่อสคริปต์หยิบไม่ได้เก็บอ้างอิงตรงๆ)")]
    [SerializeField] private float fallbackHoldMaxDistance = 1.6f;

    private XRGrabInteractable xrGrab;
    private TestSimpleGrab simpleGrab;
    private bool wasHeldLastFrame = false;
    private bool callConnected = false;

    private void Awake()
    {
        if (phoneRigidbody == null)
            phoneRigidbody = GetComponent<Rigidbody>();

        xrGrab = GetComponent<XRGrabInteractable>();
        simpleGrab = FindFirstObjectByType<TestSimpleGrab>();
    }

    private void OnEnable()
    {
        if (xrGrab != null)
        {
            xrGrab.selectEntered.AddListener(OnXRGrabbed);
            xrGrab.selectExited.AddListener(OnXRReleased);
        }
    }

    private void OnDisable()
    {
        if (xrGrab != null)
        {
            xrGrab.selectEntered.RemoveListener(OnXRGrabbed);
            xrGrab.selectExited.RemoveListener(OnXRReleased);
        }
    }

    private void Start()
    {
        if (dialCanvasOrPanel != null && hideOnStart)
            dialCanvasOrPanel.SetActive(false);

        if (phoneRigidbody != null)
        {
            phoneRigidbody.useGravity = true;
            if (!IsPhoneCurrentlyHeld())
                phoneRigidbody.isKinematic = false;
        }
    }

    private void Update()
    {
        if (simpleGrab == null)
            simpleGrab = FindFirstObjectByType<TestSimpleGrab>();

        bool isHeldNow = IsPhoneCurrentlyHeld();

        if (isHeldNow != wasHeldLastFrame)
        {
            ApplyHoldState(isHeldNow);
            wasHeldLastFrame = isHeldNow;
        }
    }

    /// <summary>
    /// เรียกฟังก์ชันนี้จากปุ่ม CALL หรือ EmergencyDialer1669 เมื่อกดโทรติดได้หากต้องการค้างหน้าจอ
    /// </summary>
    public void NotifyCallConnected()
    {
        callConnected = true;
        if (dialCanvasOrPanel != null)
            dialCanvasOrPanel.SetActive(true);
    }

    private void OnXRGrabbed(SelectEnterEventArgs args)
    {
        ApplyHoldState(true);
        wasHeldLastFrame = true;
    }

    private void OnXRReleased(SelectExitEventArgs args)
    {
        ApplyHoldState(false);
        wasHeldLastFrame = false;
    }

    private bool IsPhoneCurrentlyHeld()
    {
        // 1. เช็กจาก XRGrabInteractable ก่อน
        if (xrGrab != null && xrGrab.isSelected)
            return true;

        // 2. เช็กว่าตัว Phone ถูกย้ายไปเป็นลูก (Parent) ของกล้อง/จอย/มือ/PlayerRig หรือไม่
        Transform currentParent = transform.parent;
        while (currentParent != null)
        {
            string lowerName = currentParent.name.ToLower();
            if (lowerName.Contains("hand") ||
                lowerName.Contains("controller") ||
                lowerName.Contains("hold") ||
                lowerName.Contains("attach") ||
                lowerName.Contains("camera") ||
                lowerName.Contains("playerrig"))
            {
                return true;
            }
            currentParent = currentParent.parent;
        }

        // 3. เช็กผ่าน TestSimpleGrab แบบยืดหยุ่น (รองรับทั้งเมธอด/ฟิลด์ชื่อต่างกันในสคริปต์)
        if (simpleGrab != null && IsHeldBySimpleGrab(simpleGrab, transform))
            return true;

        return false;
    }

    private bool IsHeldBySimpleGrab(TestSimpleGrab grab, Transform target)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = grab.GetType();

        string[] objectFieldOrPropNames =
        {
            "heldObject", "currentHeldObject", "grabbedObject", "selectedObject",
            "HeldObject", "CurrentHeldObject", "GrabbedObject", "targetObject"
        };

        foreach (string name in objectFieldOrPropNames)
        {
            var field = type.GetField(name, flags);
            if (field != null)
            {
                object val = field.GetValue(grab);
                if (MatchesTarget(val, target))
                    return true;
            }

            var prop = type.GetProperty(name, flags);
            if (prop != null && prop.CanRead)
            {
                object val = prop.GetValue(grab);
                if (MatchesTarget(val, target))
                    return true;
            }
        }

        // กรณีมี bool บอกว่ากำลังถืออยู่ และระยะห่างอยู่ใกล้พอ
        string[] boolNames = { "isHolding", "IsHolding", "hasHeldObject", "isGrabbing" };
        foreach (string bName in boolNames)
        {
            var field = type.GetField(bName, flags);
            if (field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(grab))
            {
                float dist = Vector3.Distance(grab.transform.position, target.position);
                if (dist <= fallbackHoldMaxDistance)
                    return true;
            }

            var prop = type.GetProperty(bName, flags);
            if (prop != null && prop.CanRead && prop.PropertyType == typeof(bool) && (bool)prop.GetValue(grab))
            {
                float dist = Vector3.Distance(grab.transform.position, target.position);
                if (dist <= fallbackHoldMaxDistance)
                    return true;
            }
        }

        return false;
    }

    private bool MatchesTarget(object candidate, Transform target)
    {
        if (candidate == null || target == null)
            return false;

        Transform candidateTransform = null;
        if (candidate is Transform t)
            candidateTransform = t;
        else if (candidate is GameObject go)
            candidateTransform = go.transform;
        else if (candidate is Component comp)
            candidateTransform = comp.transform;

        if (candidateTransform == null)
            return false;

        return candidateTransform == target ||
               target.IsChildOf(candidateTransform) ||
               candidateTransform.IsChildOf(target);
    }

    private void ApplyHoldState(bool held)
    {
        if (phoneRigidbody != null)
        {
            phoneRigidbody.useGravity = true;
            if (kinematicWhileHeld)
                phoneRigidbody.isKinematic = held;
        }

        if (dialCanvasOrPanel == null)
            return;

        if (held)
        {
            dialCanvasOrPanel.SetActive(true);
        }
        else
        {
            if (keepOpenAfterCallConnected && callConnected)
                return;

            dialCanvasOrPanel.SetActive(false);
        }
    }
}
