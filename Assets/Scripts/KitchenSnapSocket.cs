using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class KitchenSnapSocket : MonoBehaviour
{
    public enum SocketMode
    {
        ZipBagSocket,
        IceContainerSocket
    }

    [Header("Socket Settings")]
    public SocketMode socketMode = SocketMode.ZipBagSocket;
    public Transform snapPoint;
    public float snapSpeed = 25f;
    public bool lockAfterSnap = true;
    public bool disableSolidCollidersOnSnap = true;
    public bool lockParentContainerOnIceSnap = true;

    [Header("Task Link")]
    public Case2TaskManager taskManager;

    [Header("Tags & Bag Validation")]
    public string bareFingerTag = "SeveredFinger";
    public string baggedFingerTag = "BaggedFinger";
    public string emptyBagTag = "Untagged";

    [Tooltip("กันไม่ให้เอาถุงเปล่า (ที่ยังไม่มีนิ้ว) ไปล็อคแช่ในถังน้ำแข็งก่อนใส่นิ้ว")]
    public bool requireFingerInsideBagBeforeIceSnap = true;

    [Header("Optional Visual Swap (e.g., Open Bag -> Sealed Bag)")]
    public GameObject openBagObject;
    public GameObject sealedBagObject;

    [Header("Events")]
    public UnityEvent onSnappedToZipBag;
    public UnityEvent onSnappedToIceContainer;
    public UnityEvent<string> onWarningMessage;

    private readonly HashSet<EntityId> lockedObjectIds = new HashSet<EntityId>();
    private readonly HashSet<EntityId> fingerBaggedIds = new HashSet<EntityId>();
    private Transform activeSnappingTarget;
    private Rigidbody activeSnappingRb;
    private bool isSnappingInProgress = false;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (snapPoint == null)
        {
            snapPoint = transform;
        }

        if (taskManager == null)
        {
            taskManager = FindAnyObjectByType<Case2TaskManager>();
        }

        if (socketMode == SocketMode.ZipBagSocket)
        {
            if (openBagObject != null) openBagObject.SetActive(true);
            if (sealedBagObject != null) sealedBagObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHandleTrigger(other);
    }

    private bool SafeCompareTag(Component target, string tagName)
    {
        if (target == null || string.IsNullOrEmpty(tagName)) return false;
        try
        {
            return target.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private void ZeroVelocitySafely(Rigidbody rb)
    {
        if (rb == null || rb.isKinematic) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void SetSolidCollidersEnabled(Transform targetRoot, bool enabled)
    {
        if (targetRoot == null) return;
        Collider[] cols = targetRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && !cols[i].isTrigger)
            {
                cols[i].enabled = enabled;
            }
        }
    }

    private bool HasBaggedFingerInside(Transform root, Collider other)
    {
        if (root != null && fingerBaggedIds.Contains(root.GetEntityId()))
        {
            return true;
        }

        if (other != null && fingerBaggedIds.Contains(other.transform.root.GetEntityId()))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(baggedFingerTag))
        {
            if (SafeCompareTag(other, baggedFingerTag) || SafeCompareTag(root, baggedFingerTag))
                return true;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i] != root && SafeCompareTag(children[i], baggedFingerTag))
                    return true;
            }
        }

        return false;
    }

    private void TryHandleTrigger(Collider other)
    {
        if (other == null) return;

        if (taskManager == null)
        {
            taskManager = FindAnyObjectByType<Case2TaskManager>();
        }

        if (taskManager != null && !taskManager.CanPerformSteps2To5("Turn off the stove first!"))
        {
            onWarningMessage?.Invoke("Turn off the stove first!");
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        Transform targetRoot = rb != null ? rb.transform : other.transform.root;

        if (targetRoot == null) return;
        if (lockedObjectIds.Contains(targetRoot.GetEntityId())) return;

        if (socketMode == SocketMode.ZipBagSocket)
        {
            if (SafeCompareTag(other, bareFingerTag) || SafeCompareTag(targetRoot, bareFingerTag))
            {
                BeginSnap(targetRoot, rb, isIceContainer: false);
            }
        }
        else if (socketMode == SocketMode.IceContainerSocket)
        {
            bool hasFinger = HasBaggedFingerInside(targetRoot, other);

            if (hasFinger)
            {
                BeginSnap(targetRoot, rb, isIceContainer: true);
            }
            else if (SafeCompareTag(other, bareFingerTag) || SafeCompareTag(targetRoot, bareFingerTag))
            {
                onWarningMessage?.Invoke("Put the severed finger in the bag before soaking in ice!");
            }
            else if (requireFingerInsideBagBeforeIceSnap)
            {
                onWarningMessage?.Invoke("Put the severed finger in the bag first before placing it in the ice bucket!");
            }
        }
    }

    private void BeginSnap(Transform targetRoot, Rigidbody rb, bool isIceContainer)
    {
        if (isSnappingInProgress && activeSnappingTarget != targetRoot) return;

        activeSnappingTarget = targetRoot;
        activeSnappingRb = rb;
        isSnappingInProgress = true;

        TestSimpleGrab grab = FindAnyObjectByType<TestSimpleGrab>();
        if (grab != null)
        {
            grab.ForceReleaseIfHolding(targetRoot);
        }

        if (rb != null)
        {
            ZeroVelocitySafely(rb);
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        if (disableSolidCollidersOnSnap)
        {
            SetSolidCollidersEnabled(targetRoot, false);
        }

        if (snapSpeed <= 0f || Vector3.Distance(targetRoot.position, snapPoint.position) <= 0.05f)
        {
            CompleteSnap(targetRoot, rb, isIceContainer);
        }
    }

    private void Update()
    {
        if (!isSnappingInProgress || activeSnappingTarget == null || snapPoint == null) return;

        float step = snapSpeed * Time.deltaTime;
        activeSnappingTarget.position = Vector3.MoveTowards(
            activeSnappingTarget.position,
            snapPoint.position,
            step
        );

        activeSnappingTarget.rotation = Quaternion.RotateTowards(
            activeSnappingTarget.rotation,
            snapPoint.rotation,
            step * 20f
        );

        if (Vector3.Distance(activeSnappingTarget.position, snapPoint.position) <= 0.01f &&
            Quaternion.Angle(activeSnappingTarget.rotation, snapPoint.rotation) <= 1.5f)
        {
            bool isIce = (socketMode == SocketMode.IceContainerSocket);
            CompleteSnap(activeSnappingTarget, activeSnappingRb, isIce);
        }
    }

    private void CompleteSnap(Transform targetRoot, Rigidbody rb, bool isIceContainer)
    {
        lockedObjectIds.Add(targetRoot.GetEntityId());
        isSnappingInProgress = false;

        if (rb != null)
        {
            ZeroVelocitySafely(rb);
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        if (disableSolidCollidersOnSnap)
        {
            SetSolidCollidersEnabled(targetRoot, false);
        }

        if (lockAfterSnap && snapPoint != null)
        {
            targetRoot.SetParent(snapPoint, worldPositionStays: false);
            targetRoot.localPosition = Vector3.zero;
            targetRoot.localRotation = Quaternion.identity;
        }

        if (!isIceContainer)
        {
            fingerBaggedIds.Add(targetRoot.GetEntityId());

            if (!string.IsNullOrEmpty(baggedFingerTag))
            {
                try
                {
                    targetRoot.tag = baggedFingerTag;
                }
                catch (UnityException) { }
            }

            if (openBagObject != null) openBagObject.SetActive(false);
            if (sealedBagObject != null) sealedBagObject.SetActive(true);

            if (taskManager == null)
            {
                taskManager = FindAnyObjectByType<Case2TaskManager>();
            }
            taskManager?.CompleteStep3_BagFinger();

            onSnappedToZipBag?.Invoke();
        }
        else
        {
            if (lockParentContainerOnIceSnap)
            {
                Rigidbody bucketRb = GetComponentInParent<Rigidbody>();
                if (bucketRb != null)
                {
                    ZeroVelocitySafely(bucketRb);
                    bucketRb.isKinematic = true;
                }
            }

            if (taskManager == null)
            {
                taskManager = FindAnyObjectByType<Case2TaskManager>();
            }
            taskManager?.CompleteStep5_SoakInIce();

            onSnappedToIceContainer?.Invoke();
        }

        activeSnappingTarget = null;
        activeSnappingRb = null;
    }

    public bool IsObjectLocked(Transform target)
    {
        if (target == null) return false;
        return lockedObjectIds.Contains(target.GetEntityId());
    }
}