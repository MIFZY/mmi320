using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class FridgeIceDispenser : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก Object_4 (ส่วนน้ำแข็งในถัง) มาใส่")]
    public GameObject iceInsideBucket;

    [Tooltip("ลาก IceContainerSocket มาใส่ (จะเปิดใช้งานเมื่อมีน้ำแข็งแล้ว)")]
    public GameObject iceContainerSocket;

    [Header("Task Link")]
    public Case2TaskManager taskManager;

    [Header("Interaction Settings")]
    public float interactRange = 4.0f;
    public bool requireBucketNearFridge = false;
    public Transform iceBucketTransform;
    public float maxBucketDistance = 2.5f;

    [Header("Events")]
    public UnityEvent onIceFilled;
    public UnityEvent<string> onStatusMessage;

    private Camera cam;
    private bool hasIce = false;

    private void Start()
    {
        cam = Camera.main;

        if (taskManager == null)
        {
            taskManager = FindAnyObjectByType<Case2TaskManager>();
        }

        // เริ่มเกมให้ซ่อนน้ำแข็งในถัง (เป็นถังเปล่า) และปิดจุดแช่นิ้วไว้ก่อน
        if (iceInsideBucket != null)
            iceInsideBucket.SetActive(false);

        if (iceContainerSocket != null)
            iceContainerSocket.SetActive(false);
    }

    private void Update()
    {
        if (hasIce || cam == null) return;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            TryGetIceFromFridge();
        }
    }

    private void TryGetIceFromFridge()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            // เช็กว่าเล็งโดนตัวตู้เย็นหรือชิ้นส่วนลูกของตู้เย็นหรือไม่
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                if (requireBucketNearFridge && iceBucketTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, iceBucketTransform.position);
                    if (dist > maxBucketDistance)
                    {
                        onStatusMessage?.Invoke("กรุณานำถังน้ำแข็งมาใกล้ตู้เย็นก่อนกดเอาน้ำแข็ง");
                        return;
                    }
                }

                FillIce();
            }
        }
    }

    public void FillIce()
    {
        hasIce = true;

        if (iceInsideBucket != null)
            iceInsideBucket.SetActive(true);

        if (iceContainerSocket != null)
            iceContainerSocket.SetActive(true);

        if (taskManager == null)
        {
            taskManager = FindAnyObjectByType<Case2TaskManager>();
        }
        taskManager?.CompleteStep4_IceInBucket();

        onStatusMessage?.Invoke("เติมน้ำแข็งลงถังเรียบร้อยแล้ว");
        onIceFilled?.Invoke();
    }
}