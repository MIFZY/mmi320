using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestSimpleGrab : MonoBehaviour
{
    [Header("Grab & Drop Keys")]
    public Transform holdPoint;
    public float grabRange = 3.0f;
    public float holdFollowSpeed = 20.0f;
    public LayerMask grabbableLayerMask = ~0;

    [Header("Player Collision Avoidance")]
    public Transform playerRoot;

    [Header("Crosshair UI")]
    public bool showCrosshair = true;
    public float crosshairSize = 8.0f;

    private Camera cam;
    private Rigidbody heldRb;
    private Transform heldTransform;
    private bool prevUseGravity;
    private bool prevIsKinematic;
    private RigidbodyInterpolation prevInterpolation;
    private CollisionDetectionMode prevCollisionMode;

    private readonly List<Collider> playerColliders = new List<Collider>();
    private readonly List<Collider> heldColliders = new List<Collider>();

    public bool IsHoldingObject => heldRb != null;

    private void Awake()
    {
        cam = Camera.main;

        if (holdPoint == null && cam != null)
        {
            holdPoint = cam.transform;
        }

        if (playerRoot == null)
        {
            playerRoot = transform.root;
        }

        CachePlayerColliders();
    }

    private void CachePlayerColliders()
    {
        playerColliders.Clear();
        if (playerRoot != null)
        {
            playerColliders.AddRange(playerRoot.GetComponentsInChildren<Collider>(true));
        }
    }

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        // กด Q = วางของเมื่อกำลังถืออยู่เท่านั้น
        if (kb.qKey.wasPressedThisFrame && heldRb != null)
        {
            ReleaseHeldObject();
            return;
        }

        // กด E = อินเทอร์แรคหรือหยิบของ
        if (kb.eKey.wasPressedThisFrame)
        {
            TryInteractOrGrab();
        }
    }

    private void FixedUpdate()
    {
        if (heldRb == null || heldTransform == null || holdPoint == null) return;

        if (IsTargetLocked(heldTransform) || heldRb.isKinematic)
        {
            SetIgnorePlayerCollisions(heldTransform, false);
            ClearHeldReferenceWithoutPhysicsRestore();
            return;
        }

        Vector3 targetPos = holdPoint.position;
        Quaternion targetRot = holdPoint.rotation;

        Vector3 toTarget = targetPos - heldRb.position;
        heldRb.linearVelocity = toTarget * holdFollowSpeed;
        heldRb.angularVelocity = Vector3.zero;

        Quaternion smoothedRot = Quaternion.Slerp(
            heldRb.rotation,
            targetRot,
            Mathf.Clamp01(holdFollowSpeed * 0.5f * Time.fixedDeltaTime)
        );
        heldRb.MoveRotation(smoothedRot);
    }

    private void TryInteractOrGrab()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // 1. ตรวจสอบจุดอินเทอร์แรคเฉพาะ (รวม Trigger เช่น ปุ่มหรือจุดกดของเตาแก๊ส/ตู้ทำน้ำแข็ง/โทรศัพท์)
        if (Physics.Raycast(ray, out RaycastHit triggerHit, grabRange, grabbableLayerMask, QueryTriggerInteraction.Collide))
        {
            GasStoveController stove = triggerHit.collider.GetComponentInParent<GasStoveController>();
            if (stove != null)
            {
                stove.SendMessage("TurnOffGas", SendMessageOptions.DontRequireReceiver);
                stove.SendMessage("ToggleGasStove", SendMessageOptions.DontRequireReceiver);
                return;
            }

            FridgeIceDispenser iceDispenser = triggerHit.collider.GetComponentInParent<FridgeIceDispenser>();
            if (iceDispenser != null)
            {
                iceDispenser.SendMessage("DispenseIce", SendMessageOptions.DontRequireReceiver);
                iceDispenser.SendMessage("TryDispenseIce", SendMessageOptions.DontRequireReceiver);
                return;
            }

            EmergencyDialer1669 phoneDialer = triggerHit.collider.GetComponentInParent<EmergencyDialer1669>();
            if (phoneDialer != null)
            {
                phoneDialer.SendMessage("OpenDialPad", SendMessageOptions.DontRequireReceiver);
            }
        }

        // หากกำลังถือของอยู่แล้ว ไม่หยิบชิ้นใหม่ซ้อน
        if (heldRb != null) return;

        // 2. หยิบออบเจกต์ที่มี Rigidbody (ข้าม Trigger เพื่อไม่ให้เล็งพลาดทะลุจุดรับ)
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabbableLayerMask, QueryTriggerInteraction.Ignore))
        {
            Rigidbody targetRb = hit.rigidbody;
            Transform targetTf = targetRb != null ? targetRb.transform : hit.transform;

            if (targetRb == null || targetTf == null) return;
            if (targetRb.isKinematic) return;
            if (IsTargetLocked(targetTf)) return;

            heldRb = targetRb;
            heldTransform = targetTf;

            prevUseGravity = heldRb.useGravity;
            prevIsKinematic = heldRb.isKinematic;
            prevInterpolation = heldRb.interpolation;
            prevCollisionMode = heldRb.collisionDetectionMode;

            heldRb.useGravity = false;
            heldRb.isKinematic = false;
            heldRb.interpolation = RigidbodyInterpolation.Interpolate;
            heldRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;

            SetIgnorePlayerCollisions(heldTransform, true);
        }
    }

    public void ReleaseHeldObject()
    {
        if (heldRb == null) return;

        SetIgnorePlayerCollisions(heldTransform, false);

        if (!heldRb.isKinematic)
        {
            heldRb.useGravity = prevUseGravity;
            heldRb.isKinematic = prevIsKinematic;
            heldRb.interpolation = prevInterpolation;
            heldRb.collisionDetectionMode = prevCollisionMode;
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;
        }

        heldRb = null;
        heldTransform = null;
        heldColliders.Clear();
    }

    public void ForceReleaseIfHolding(Transform target)
    {
        if (heldRb == null || heldTransform == null || target == null) return;

        if (heldTransform == target || heldTransform == target.root || target.IsChildOf(heldTransform))
        {
            SetIgnorePlayerCollisions(heldTransform, false);
            ClearHeldReferenceWithoutPhysicsRestore();
        }
    }

    public bool IsHolding(Transform target)
    {
        if (heldTransform == null || target == null) return false;
        return heldTransform == target || heldTransform == target.root || target.IsChildOf(heldTransform) || heldTransform.IsChildOf(target);
    }

    private void ClearHeldReferenceWithoutPhysicsRestore()
    {
        heldRb = null;
        heldTransform = null;
        heldColliders.Clear();
    }

    private bool IsTargetLocked(Transform target)
    {
        KitchenSnapSocket[] sockets = FindObjectsByType<KitchenSnapSocket>(FindObjectsInactive.Exclude);
        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i].IsObjectLocked(target) || sockets[i].IsObjectLocked(target.root))
            {
                return true;
            }
        }
        return false;
    }

    private void SetIgnorePlayerCollisions(Transform target, bool ignore)
    {
        if (target == null) return;

        if (playerColliders.Count == 0)
        {
            CachePlayerColliders();
        }

        heldColliders.Clear();
        heldColliders.AddRange(target.GetComponentsInChildren<Collider>(true));

        for (int i = 0; i < heldColliders.Count; i++)
        {
            Collider hCol = heldColliders[i];
            if (hCol == null || hCol.isTrigger) continue;

            for (int j = 0; j < playerColliders.Count; j++)
            {
                Collider pCol = playerColliders[j];
                if (pCol == null) continue;

                Physics.IgnoreCollision(hCol, pCol, ignore);
            }
        }
    }

    private void OnGUI()
    {
        if (!showCrosshair) return;

        float x = (Screen.width - crosshairSize) * 0.5f;
        float y = (Screen.height - crosshairSize) * 0.5f;
        GUI.Box(new Rect(x, y, crosshairSize, crosshairSize), GUIContent.none);
    }
}