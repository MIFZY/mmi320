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
    [SerializeField] private float releaseSnapRadius = 2.0f;
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

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

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
        if (!Input.GetKeyDown(releaseKey))
            return;

        TestSimpleGrab grabber = FindFirstObjectByType<TestSimpleGrab>();

        if (debugLogs)
        {
            Debug.Log($"[ArmSplintSocket] Q pressed | newspaperDone={newspaperDone}, clothDone={clothDone}, itemInZone={(itemInZone ? itemInZone.name : "null")}, IsHoldingObject={(grabber != null && grabber.IsHoldingObject)}");
        }

        // 1. ไอเทมอยู่ใน Trigger Zone ตอนกด Q
        if (itemInZone != null && IsTargetItem(itemInZone))
        {
            TrySnapItem(itemInZone, grabber);
            return;
        }

        // 2. ไอเทมที่กำลังถืออยู่ (เช็กทั้งระยะของไอเทม และระยะของผู้เล่นถึงจุดสแนป)
        if (grabber != null && grabber.IsHoldingObject)
        {
            Rigidbody[] rbs = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (Rigidbody rb in rbs)
            {
                if (rb == null) continue;
                GameObject candidate = ResolveItemObject(rb.gameObject);
                if (!IsTargetItem(candidate)) continue;
                if (!grabber.IsHolding(candidate.transform)) continue;

                Transform refPoint = GetReferenceSnapPoint(candidate);
                float itemDistSocket = Vector3.Distance(rb.position, transform.position);
                float itemDistPoint = Vector3.Distance(rb.position, refPoint.position);
                float playerDistSocket = playerRoot != null ? Vector3.Distance(playerRoot.position, transform.position) : float.MaxValue;
                float playerDistPoint = playerRoot != null ? Vector3.Distance(playerRoot.position, refPoint.position) : float.MaxValue;

                if (debugLogs)
                {
                    Debug.Log($"[ArmSplintSocket] Holding '{candidate.name}' | itemDist={Mathf.Min(itemDistSocket, itemDistPoint):F2}, playerDist={Mathf.Min(playerDistSocket, playerDistPoint):F2}, radius={releaseSnapRadius}");
                }

                if (itemDistSocket <= releaseSnapRadius ||
                    itemDistPoint <= releaseSnapRadius ||
                    playerDistSocket <= releaseSnapRadius ||
                    playerDistPoint <= releaseSnapRadius)
                {
                    TrySnapItem(candidate, grabber);
                    return;
                }
            }
        }

        // 3. กรณีเพิ่งกด Q ปล่อยของไปแล้ว (IsHoldingObject = False) แต่อยู่ใกล้จุดสแนปหรือผู้เล่นเดินอยู่ใกล้
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform t in allTransforms)
        {
            if (t == null) continue;
            GameObject candidate = ResolveItemObject(t.gameObject);
            if (!IsTargetItem(candidate)) continue;

            Transform refPoint = GetReferenceSnapPoint(candidate);
            float distItemPoint = Vector3.Distance(candidate.transform.position, refPoint.position);
            float distItemSocket = Vector3.Distance(candidate.transform.position, transform.position);
            float distPlayerPoint = playerRoot != null ? Vector3.Distance(playerRoot.position, refPoint.position) : float.MaxValue;

            if (distItemPoint <= releaseSnapRadius || distItemSocket <= releaseSnapRadius || distPlayerPoint <= releaseSnapRadius)
            {
                if (debugLogs)
                {
                    Debug.Log($"[ArmSplintSocket] Fallback snap '{candidate.name}' | distItemPoint={distItemPoint:F2}, distPlayerPoint={distPlayerPoint:F2}");
                }
                TrySnapItem(candidate, grabber);
                return;
            }
        }
    }

    private Transform GetReferenceSnapPoint(GameObject itemObj)
    {
        if (!newspaperDone && HasMatchingTagInHierarchy(itemObj, newspaperTag) && newspaperSnapPoint != null)
            return newspaperSnapPoint;

        if (!clothDone && HasMatchingTagInHierarchy(itemObj, clothTag) && clothSnapPoint != null)
            return clothSnapPoint;

        return transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.isTrigger) return;
        GameObject candidate = ResolveItemObject(other.gameObject);
        if (IsTargetItem(candidate))
        {
            itemInZone = candidate;
            if (debugLogs) Debug.Log($"[ArmSplintSocket] OnTriggerEnter -> itemInZone = {candidate.name}");
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
        if (manager == null)
        {
            if (debugLogs) Debug.LogWarning("[ArmSplintSocket] Case1TaskManager.Instance is null!");
            return;
        }

        bool isNewspaper = !newspaperDone && HasMatchingTagInHierarchy(itemObj, newspaperTag);
        bool isCloth = !clothDone && HasMatchingTagInHierarchy(itemObj, clothTag);
        if (!isNewspaper && !isCloth) return;

        if (isNewspaper)
        {
            if (!manager.CompleteTask2_PlaceNewspaper())
            {
                if (debugLogs) Debug.LogWarning("[ArmSplintSocket] CompleteTask2_PlaceNewspaper() returned false!");
                return;
            }

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
            {
                if (debugLogs) Debug.LogWarning("[ArmSplintSocket] CompleteTask3_WrapCloth() returned false!");
                return;
            }

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
        itemObj.transform.localScale = Vector3.one;

        return itemObj;
    }
}