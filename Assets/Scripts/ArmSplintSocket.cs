using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ArmSplintSocket : MonoBehaviour
{
    [Header("Required Tags")]
    [SerializeField] private string newspaperTag = "NewPaper";
    [SerializeField] private string clothTag = "Cloth";

    [Header("Release Key (Must Press Q to Snap)")]
    [SerializeField] private KeyCode releaseKey = KeyCode.Q;
    [SerializeField] private float releaseSnapRadius = 1.2f;
    [SerializeField] private Transform playerRoot;

    [Header("Snap Points (Where items attach on the arm)")]
    [SerializeField] private Transform newspaperSnapPoint;
    [SerializeField] private Transform clothSnapPoint;

    [Header("Combined Final Model (Shown AFTER both Newspaper & Cloth snap)")]
    [SerializeField] private GameObject combinedSplintModel;
    [SerializeField] private bool hideSnappedItemsWhenCombinedShown = true;

    [Header("Behavior")]
    [SerializeField] private bool lockItemPhysicsOnSnap = true;
    [SerializeField] private bool disableGrabScriptOnSnap = true;

    [Header("Events")]
    public UnityEvent onNewspaperSnapped;
    public UnityEvent onClothWrapped;
    public UnityEvent onBothSnappedCombinedShown;

    private bool newspaperDone = false;
    private bool clothDone = false;
    private GameObject snappedNewspaperObj;
    private GameObject snappedClothObj;
    private GameObject itemInZone;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        if (combinedSplintModel != null)
            combinedSplintModel.SetActive(false);

        if (playerRoot == null && Camera.main != null)
            playerRoot = Camera.main.transform.root;
    }

    private void Update()
    {
        // บังคับต้องกดปุ่ม Q เท่านั้น ห้าม Snap จาก Trigger เอง
        if (!Input.GetKeyDown(releaseKey))
            return;

        TestSimpleGrab grabber = FindFirstObjectByType<TestSimpleGrab>();

        // 1. เช็กไอเทมที่อยู่ใน Trigger Zone ตอนที่กด Q
        if (itemInZone != null && IsTargetItem(itemInZone))
        {
            if (IsHeldOrValidForRelease(itemInZone, grabber))
            {
                TrySnapItem(itemInZone, grabber);
                return;
            }
        }

        // 2. เผื่อกรณีถือไอเทมอยู่ในระยะ releaseSnapRadius จากจุด Snap ตอนกด Q
        if (grabber != null && grabber.IsHoldingObject)
        {
            Rigidbody[] rbs = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (Rigidbody rb in rbs)
            {
                if (rb == null) continue;
                GameObject candidate = ResolveItemObject(rb.gameObject);
                if (!IsTargetItem(candidate)) continue;
                if (!grabber.IsHolding(candidate.transform)) continue;

                float dist = Vector3.Distance(rb.position, transform.position);
                if (dist <= releaseSnapRadius)
                {
                    TrySnapItem(candidate, grabber);
                    return;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.isTrigger) return;
        GameObject candidate = ResolveItemObject(other.gameObject);
        if (IsTargetItem(candidate))
        {
            itemInZone = candidate;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null || other.isTrigger) return;
        GameObject candidate = ResolveItemObject(other.gameObject);
        if (IsTargetItem(candidate))
        {
            itemInZone = candidate;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        GameObject candidate = ResolveItemObject(other.gameObject);
        if (itemInZone == candidate)
        {
            itemInZone = null;
        }
    }

    private bool IsHeldOrValidForRelease(GameObject itemObj, TestSimpleGrab grabber)
    {
        if (itemObj == null) return false;

        if (grabber != null && grabber.IsHolding(itemObj.transform))
            return true;

        if (playerRoot != null && itemObj.transform.IsChildOf(playerRoot))
            return true;

        if (Camera.main != null && itemObj.transform.IsChildOf(Camera.main.transform))
            return true;

        // กรณีผู้เล่นเพิ่งกด Q แล้ว TestSimpleGrab ปล่อยมือไปแล้วแต่ของยังอยู่ในโซน/ระยะใกล้แขน
        float dist = Vector3.Distance(itemObj.transform.position, transform.position);
        return dist <= releaseSnapRadius;
    }

    private GameObject ResolveItemObject(GameObject hitObj)
    {
        if (hitObj == null) return null;

        Rigidbody rb = hitObj.GetComponentInParent<Rigidbody>();
        if (rb != null)
            return rb.gameObject;

        return hitObj.transform.root != null ? hitObj.transform.root.gameObject : hitObj;
    }

    private bool IsTargetItem(GameObject itemObj)
    {
        if (itemObj == null) return false;
        if (itemObj == snappedNewspaperObj || itemObj == snappedClothObj) return false;

        bool isNewspaper = !newspaperDone && HasMatchingTagInHierarchy(itemObj, newspaperTag);
        bool isCloth = !clothDone && HasMatchingTagInHierarchy(itemObj, clothTag);
        return isNewspaper || isCloth;
    }

    private bool HasMatchingTagInHierarchy(GameObject obj, string targetTag)
    {
        if (obj == null || string.IsNullOrEmpty(targetTag)) return false;

        if (obj.CompareTag(targetTag)) return true;
        if (obj.transform.root != null && obj.transform.root.CompareTag(targetTag)) return true;

        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].CompareTag(targetTag))
                return true;
        }

        return false;
    }

    private void TrySnapItem(GameObject itemObj, TestSimpleGrab grabber)
    {
        if (itemObj == null) return;

        Case1TaskManager manager = Case1TaskManager.Instance;
        if (manager == null) return;

        bool isNewspaper = !newspaperDone && HasMatchingTagInHierarchy(itemObj, newspaperTag);
        bool isCloth = !clothDone && HasMatchingTagInHierarchy(itemObj, clothTag);
        if (!isNewspaper && !isCloth) return;

        if (isNewspaper)
        {
            if (!manager.CompleteTask2_PlaceNewspaper())
                return;

            newspaperDone = true;
            if (itemInZone == itemObj) itemInZone = null;
            if (grabber != null) grabber.ForceReleaseIfHolding(itemObj.transform);

            snappedNewspaperObj = AttachItem(
                itemObj,
                newspaperSnapPoint != null ? newspaperSnapPoint : transform
            );

            onNewspaperSnapped?.Invoke();
            CheckAndShowCombinedModel();
            return;
        }

        if (isCloth)
        {
            if (!manager.CompleteTask3_WrapCloth())
                return;

            clothDone = true;
            if (itemInZone == itemObj) itemInZone = null;
            if (grabber != null) grabber.ForceReleaseIfHolding(itemObj.transform);

            snappedClothObj = AttachItem(
                itemObj,
                clothSnapPoint != null ? clothSnapPoint : transform
            );

            onClothWrapped?.Invoke();
            CheckAndShowCombinedModel();
        }
    }

    private void CheckAndShowCombinedModel()
    {
        if (!newspaperDone || !clothDone) return;

        if (combinedSplintModel != null)
        {
            combinedSplintModel.SetActive(true);

            if (hideSnappedItemsWhenCombinedShown)
            {
                if (snappedNewspaperObj != null && snappedNewspaperObj != combinedSplintModel)
                    snappedNewspaperObj.SetActive(false);

                if (snappedClothObj != null && snappedClothObj != combinedSplintModel)
                    snappedClothObj.SetActive(false);
            }
        }

        onBothSnappedCombinedShown?.Invoke();
    }

    private GameObject AttachItem(GameObject itemObj, Transform targetPoint)
    {
        if (disableGrabScriptOnSnap)
        {
            TestSimpleGrab grab = itemObj.GetComponentInChildren<TestSimpleGrab>();
            if (grab != null)
                grab.enabled = false;
        }

        if (lockItemPhysicsOnSnap)
        {
            Rigidbody[] rbs = itemObj.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rbs)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        itemObj.transform.SetParent(targetPoint);
        itemObj.transform.localPosition = Vector3.zero;
        itemObj.transform.localRotation = Quaternion.identity;

        return itemObj;
    }
}