using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class WoundHemostasisSocket : MonoBehaviour
{
    [Header("Hemostasis Settings")]
    public string requiredClothTag = "BleedingCloth";
    public Transform snapPoint;
    public float snapSpeed = 25f;
    public bool lockClothAfterSnap = true;
    public bool hideClothAfterSnap = true;
    public bool disableClothCollidersOnSnap = true;

    [Header("Optional Blood / Wound Visuals")]
    [Tooltip("ลากออบเจกต์เอฟเฟกต์เลือดหรือรอยแผลสดมาใส่ เพื่อซ่อน/ลดตอนห้ามเลือดสำเร็จ")]
    public GameObject bleedingVisualObject;

    [Tooltip("ลากออบเจกต์ผ้าพันแผล/ผ้าซับเลือดที่เตรียมไว้ (ถ้ามี) มาเปิดตอนห้ามเลือดสำเร็จ")]
    public GameObject bandagedVisualObject;

    [Header("Events")]
    public UnityEvent onHemostasisCompleted;
    public UnityEvent<string> onStatusMessage;

    private readonly HashSet<EntityId> snappedClothIds = new HashSet<EntityId>();
    private Transform activeCloth;
    private Rigidbody activeClothRb;
    private bool isSnapping = false;
    private bool isCompleted = false;

    public bool IsHemostasisCompleted => isCompleted;

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

        if (bandagedVisualObject != null)
        {
            bandagedVisualObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartSnap(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartSnap(other);
    }

    private void TryStartSnap(Collider other)
    {
        if (other == null || isCompleted) return;

        Rigidbody rb = other.attachedRigidbody;
        Transform targetRoot = rb != null ? rb.transform : other.transform.root;

        if (targetRoot == null) return;
        if (snappedClothIds.Contains(targetRoot.GetEntityId())) return;

        bool hasTag = other.CompareTag(requiredClothTag) || targetRoot.CompareTag(requiredClothTag);
        if (!hasTag) return;

        BeginSnap(targetRoot, rb);
    }

    private void BeginSnap(Transform clothRoot, Rigidbody rb)
    {
        if (isSnapping && activeCloth != clothRoot) return;

        activeCloth = clothRoot;
        activeClothRb = rb;
        isSnapping = true;

        TestSimpleGrab grab = FindAnyObjectByType<TestSimpleGrab>();
        if (grab != null)
        {
            grab.ForceReleaseIfHolding(clothRoot);
        }

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        if (disableClothCollidersOnSnap)
        {
            Collider[] cols = clothRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null && !cols[i].isTrigger)
                {
                    cols[i].enabled = false;
                }
            }
        }

        if (snapSpeed <= 0f || Vector3.Distance(clothRoot.position, snapPoint.position) <= 0.05f)
        {
            CompleteSnap(clothRoot, rb);
        }
    }

    private void Update()
    {
        if (!isSnapping || activeCloth == null || snapPoint == null) return;

        float step = snapSpeed * Time.deltaTime;
        activeCloth.position = Vector3.MoveTowards(activeCloth.position, snapPoint.position, step);
        activeCloth.rotation = Quaternion.RotateTowards(activeCloth.rotation, snapPoint.rotation, step * 20f);

        if (Vector3.Distance(activeCloth.position, snapPoint.position) <= 0.01f &&
            Quaternion.Angle(activeCloth.rotation, snapPoint.rotation) <= 1.5f)
        {
            CompleteSnap(activeCloth, activeClothRb);
        }
    }

    private void CompleteSnap(Transform clothRoot, Rigidbody rb)
    {
        snappedClothIds.Add(clothRoot.GetEntityId());
        isSnapping = false;
        isCompleted = true;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        if (disableClothCollidersOnSnap)
        {
            Collider[] cols = clothRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null && !cols[i].isTrigger)
                {
                    cols[i].enabled = false;
                }
            }
        }

        if (lockClothAfterSnap && snapPoint != null)
        {
            clothRoot.SetParent(snapPoint, worldPositionStays: false);
            clothRoot.localPosition = Vector3.zero;
            clothRoot.localRotation = Quaternion.identity;
        }

        if (hideClothAfterSnap)
        {
            clothRoot.gameObject.SetActive(false);
        }

        if (bleedingVisualObject != null)
        {
            bleedingVisualObject.SetActive(false);
        }

        if (bandagedVisualObject != null)
        {
            bandagedVisualObject.SetActive(true);
        }

        onStatusMessage?.Invoke("ห้ามเลือดด้วยผ้าเรียบร้อยแล้ว");
        onHemostasisCompleted?.Invoke();

        activeCloth = null;
        activeClothRb = null;
    }
}