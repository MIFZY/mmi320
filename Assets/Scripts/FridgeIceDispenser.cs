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

    [Header("Task Link & Sequence Guard")]
    public Case2TaskManager taskManager;

    [Tooltip("ติ๊กไว้เพื่อบล็อกการกดเอาน้ำแข็งก่อนปิดเตาแก๊ส (Step 1)")]
    public bool requireStep1BeforeFill = true;

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

    private bool CanFillIceNow()
    {
        if (!requireStep1BeforeFill || taskManager == null)
            return true;

        return taskManager.CanPerformSteps2To5("Turn off the stove first!");
    }

    private void TryGetIceFromFridge()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                if (!CanFillIceNow())
                {
                    onStatusMessage?.Invoke("Turn off the stove first before getting ice");
                    return;
                }

                if (requireBucketNearFridge && iceBucketTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, iceBucketTransform.position);
                    if (dist > maxBucketDistance)
                    {
                        onStatusMessage?.Invoke("Bring the ice bucket closer to the fridge first");
                        return;
                    }
                }

                FillIce();
            }
        }
    }

    public void FillIce()
    {
        if (hasIce) return;

        if (!CanFillIceNow())
        {
            onStatusMessage?.Invoke("Turn off the stove first before getting ice");
            return;
        }

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

        onStatusMessage?.Invoke("Ice added to bucket");
        onIceFilled?.Invoke();
    }
}