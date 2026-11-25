using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class ArmSplintSocket : MonoBehaviour
{
    [Header("Required Tags")]
    [SerializeField] private string newspaperTag = "NewPaper";
    [SerializeField] private string clothTag = "Cloth";

    [Header("Auto Snap On Release")]
    [SerializeField] private bool snapOnRelease = true;
    [SerializeField] private float releaseSnapRadius = 2.2f;
    [SerializeField] private Transform playerRoot;

    [Header("Optional Manual Trigger (Keyboard Q / VR Button)")]
    [SerializeField] private KeyCode releaseKey = KeyCode.Q;
    [SerializeField] private InputActionProperty vrSnapButtonAction;

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

    private TestSimpleGrab cachedGrabber;
    private bool wasHoldingLastFrame = false;
    private Transform heldItemLastFrame = null;
    private readonly List<XRGrabInteractable> registeredXRGrabs = new List<XRGrabInteractable>();

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (vrSnapButtonAction.action != null)
        {
            vrSnapButtonAction.action.Enable();
            vrSnapButtonAction.action.performed += OnVRSnapPressed;
        }

        RegisterXRGrabListeners();
    }

    private void OnDisable()
    {
        if (vrSnapButtonAction.action != null)
        {
            vrSnapButtonAction.action.performed -= OnVRSnapPressed;
            vrSnapButtonAction.action.Disable();
        }

        UnregisterXRGrabListeners();
    }

    private void Start()
    {
        if (combinedSplintModel != null)
            combinedSplintModel.SetActive(false);

        if (playerRoot == null && Camera.main != null)
            playerRoot = Camera.main.transform.root;

        cachedGrabber = FindFirstObjectByType<TestSimpleGrab>();
        RegisterXRGrabListeners();
    }

    private void RegisterXRGrabListeners()
    {
        UnregisterXRGrabListeners();

        XRGrabInteractable[] grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
        foreach (XRGrabInteractable grab in grabs)
        {
            if (grab == null) continue;
            GameObject candidate = ResolveItemObject(grab.gameObject);
            if (!IsTargetItem(candidate)) continue;

            grab.selectExited.AddListener(OnXRItemReleased);
            registeredXRGrabs.Add(grab);
        }
    }

    private void UnregisterXRGrabListeners()
    {
        foreach (XRGrabInteractable grab in registeredXRGrabs)
        {
            if (grab != null)
                grab.selectExited.RemoveListener(OnXRItemReleased);
        }
        registeredXRGrabs.Clear();
    }

    private void OnXRItemReleased(SelectExitEventArgs args)
    {
        if (!snapOnRelease || args.interactableObject == null) return;

        GameObject releasedObj = ResolveItemObject(args.interactableObject.transform.gameObject);
        if (IsTargetItem(releasedObj) && IsWithinSnapRange(releasedObj))
        {
            if (debugLogs)
                Debug.Log($"[ArmSplintSocket] XR released & snapped '{releasedObj.name}'!");

            TrySnapItem(releasedObj, cachedGrabber);
        }
    }

    private void Update()
    {
        if (cachedGrabber == null)
            cachedGrabber = FindFirstObjectByType<TestSimpleGrab>();

        if (snapOnRelease && cachedGrabber != null)
        {
            bool isHoldingNow = cachedGrabber.IsHoldingObject;

            if (isHoldingNow)
            {
                Transform currentHeld = FindCurrentlyHeldTarget(cachedGrabber);
                if (currentHeld != null)
                {
                    heldItemLastFrame = currentHeld;
                }
            }
            else if (wasHoldingLastFrame && heldItemLastFrame != null)
            {
                GameObject releasedObj = ResolveItemObject(heldItemLastFrame.gameObject);
                heldItemLastFrame = null;

                if (IsTargetItem(releasedObj) && IsWithinSnapRange(releasedObj))
                {
                    if (debugLogs)
                        Debug.Log($"[ArmSplintSocket] Auto-snapped '{releasedObj.name}' on release!");

                    TrySnapItem(releasedObj, cachedGrabber);
                }
            }

            wasHoldingLastFrame = isHoldingNow;
        }

        if (Input.GetKeyDown(releaseKey))
        {
            HandleManualSnap();
        }
    }

    private Transform FindCurrentlyHeldTarget(TestSimpleGrab grabber)
    {
        Rigidbody[] rbs = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        foreach (Rigidbody rb in rbs)
        {
            if (rb == null) continue;
            if (grabber.IsHolding(rb.transform))
            {
                GameObject candidate = ResolveItemObject(rb.gameObject);
                if (IsTargetItem(candidate))
                    return rb.transform;
            }
        }
        return null;
    }

    private bool IsWithinSnapRange(GameObject candidate)
    {
        if (candidate == null) return false;
        if (itemInZone == candidate) return true;

        Transform refPoint = GetReferenceSnapPoint(candidate);
        float distItemPoint = Vector3.Distance(candidate.transform.position, refPoint.position);
        float distItemSocket = Vector3.Distance(candidate.transform.position, transform.position);
        float distPlayerPoint = playerRoot != null
            ? Vector3.Distance(playerRoot.position, refPoint.position)
            : float.MaxValue;

        return distItemPoint <= releaseSnapRadius ||
               distItemSocket <= releaseSnapRadius ||
               distPlayerPoint <= releaseSnapRadius;
    }

    private void OnVRSnapPressed(InputAction.CallbackContext context)
    {
        HandleManualSnap();
    }

    private void HandleManualSnap()
    {
        if (cachedGrabber == null)
            cachedGrabber = FindFirstObjectByType<TestSimpleGrab>();

        if (itemInZone != null && IsTargetItem(itemInZone))
        {
            TrySnapItem(itemInZone, cachedGrabber);
            return;
        }

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform t in allTransforms)
        {
            if (t == null) continue;
            GameObject candidate = ResolveItemObject(t.gameObject);
            if (!IsTargetItem(candidate)) continue;

            if (IsWithinSnapRange(candidate))
            {
                TrySnapItem(candidate, cachedGrabber);
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
        if (other == null) return;
        GameObject candidate = ResolveItemObject(other.gameObject);
        if (IsTargetItem(candidate))
        {
            itemInZone = candidate;
            if (debugLogs) Debug.Log($"[ArmSplintSocket] OnTriggerEnter -> itemInZone = {candidate.name}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null) return;
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
