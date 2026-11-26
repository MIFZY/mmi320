using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FridgeIceDispenser : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก Object_4 (ส่วนน้ำแข็งในถัง) มาใส่")]
    public GameObject iceInsideBucket;

    [Tooltip("ลาก IceContainerSocket มาใส่ (จะเปิดใช้งานเมื่อมีน้ำแข็งแล้ว)")]
    public GameObject iceContainerSocket;

    [Header("Held Bucket & VR Aim (Left / Right)")]
    [Tooltip("ลากออบเจกต์ถังน้ำแข็ง (IceBucket) มาใส่เพื่อเช็กว่ากำลังถืออยู่")]
    public Transform heldBucketObject;

    [Tooltip("ติ๊กไว้หากต้องถือถังน้ำแข็งอยู่ก่อนจึงจะกดเติมน้ำแข็งได้")]
    public bool requireHoldingBucket = true;

    [Tooltip("ระยะห่างสูงสุดที่ถือว่ากำลังถือถังน้ำแข็ง (ใช้กรณีไม่ได้ใช้ XRGrabInteractable)")]
    public float maxHeldDistance = 1.6f;

    [Tooltip("ลาก Left_NearFarInteractor / Right_NearFarInteractor (เว้นว่างไว้เพื่อให้ค้นหาอัตโนมัติได้)")]
    public Transform leftControllerAim;
    public Transform rightControllerAim;
    public LayerMask raycastLayers = ~0;

    [Header("Audio")]
    [Tooltip("ลากออบเจกต์ที่มีคอมโพเนนต์ FridgeIceAudio มาใส่")]
    public FridgeIceAudio fridgeIceAudio;

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
    private XRGrabInteractable bucketGrabInteractable;
    private readonly List<UnityEngine.XR.InputDevice> leftDevices = new List<UnityEngine.XR.InputDevice>();
    private readonly List<UnityEngine.XR.InputDevice> rightDevices = new List<UnityEngine.XR.InputDevice>();
    private bool wasTriggerPressedLastFrame = false;

    private void Start()
    {
        cam = Camera.main;

        if (taskManager == null)
        {
            taskManager = FindAnyObjectByType<Case2TaskManager>();
        }

        if (fridgeIceAudio == null)
        {
            fridgeIceAudio = GetComponentInChildren<FridgeIceAudio>();
            if (fridgeIceAudio == null)
            {
                fridgeIceAudio = FindAnyObjectByType<FridgeIceAudio>();
            }
        }

        Transform bucketRef = heldBucketObject != null ? heldBucketObject : iceBucketTransform;
        if (bucketRef != null)
        {
            bucketGrabInteractable = bucketRef.GetComponentInChildren<XRGrabInteractable>();
        }

        AutoFindControllerAims();

        if (iceInsideBucket != null)
            iceInsideBucket.SetActive(false);

        if (iceContainerSocket != null)
            iceContainerSocket.SetActive(false);
    }

    private void Update()
    {
        if (hasIce) return;

        bool triggerDown = ConsumeAnyTriggerDown();
        var kb = Keyboard.current;
        bool keyboardEDown = kb != null && kb.eKey.wasPressedThisFrame;

        if (triggerDown || keyboardEDown)
        {
            TryGetIceFromFridge(triggerDown);
        }
    }

    private void AutoFindControllerAims()
    {
        if (leftControllerAim == null)
        {
            var leftInteractor = FindRayInteractorByHand(true);
            if (leftInteractor != null)
                leftControllerAim = leftInteractor.rayOriginTransform != null ? leftInteractor.rayOriginTransform : leftInteractor.transform;
        }

        if (rightControllerAim == null)
        {
            var rightInteractor = FindRayInteractorByHand(false);
            if (rightInteractor != null)
                rightControllerAim = rightInteractor.rayOriginTransform != null ? rightInteractor.rayOriginTransform : rightInteractor.transform;
        }
    }

    private XRRayInteractor FindRayInteractorByHand(bool isLeft)
    {
        var interactors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
        foreach (var interactor in interactors)
        {
            string n = interactor.gameObject.name.ToLower();
            if (isLeft && n.Contains("left")) return interactor;
            if (!isLeft && n.Contains("right")) return interactor;
        }
        return null;
    }

    private bool ConsumeAnyTriggerDown()
    {
        bool pressedNow = IsHandTriggerPressed(UnityEngine.XR.InputDeviceCharacteristics.Left) ||
                          IsHandTriggerPressed(UnityEngine.XR.InputDeviceCharacteristics.Right);

        bool downThisFrame = pressedNow && !wasTriggerPressedLastFrame;
        wasTriggerPressedLastFrame = pressedNow;
        return downThisFrame;
    }

    private bool IsHandTriggerPressed(UnityEngine.XR.InputDeviceCharacteristics handCharacteristic)
    {
        var list = handCharacteristic == UnityEngine.XR.InputDeviceCharacteristics.Left ? leftDevices : rightDevices;
        list.Clear();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.Controller | handCharacteristic, list);

        for (int i = 0; i < list.Count; i++)
        {
            UnityEngine.XR.InputDevice device = list[i];
            if (!device.isValid) continue;

            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool isPressed) && isPressed)
                return true;

            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue) && triggerValue >= 0.5f)
                return true;
        }

        return false;
    }

    private bool IsHoldingIceBucket()
    {
        Transform bucket = heldBucketObject != null ? heldBucketObject : iceBucketTransform;
        if (bucket == null) return false;

        if (bucketGrabInteractable == null)
            bucketGrabInteractable = bucket.GetComponentInChildren<XRGrabInteractable>();

        if (bucketGrabInteractable != null && bucketGrabInteractable.isSelected)
            return true;

        Transform parent = bucket.parent;
        while (parent != null)
        {
            string lower = parent.name.ToLower();
            if (lower.Contains("hand") || lower.Contains("controller") || lower.Contains("attach") || lower.Contains("interactor"))
                return true;
            parent = parent.parent;
        }

        if (leftControllerAim != null && Vector3.Distance(leftControllerAim.position, bucket.position) <= maxHeldDistance)
            return true;

        if (rightControllerAim != null && Vector3.Distance(rightControllerAim.position, bucket.position) <= maxHeldDistance)
            return true;

        return false;
    }

    private bool CanFillIceNow()
    {
        if (!requireStep1BeforeFill || taskManager == null)
            return true;

        return taskManager.CanPerformSteps2To5("Turn off the stove first!");
    }

    private void TryGetIceFromFridge(bool fromVRTrigger)
    {
        bool hitFridge = false;

        if (fromVRTrigger)
        {
            AutoFindControllerAims();
            hitFridge = RaycastHitsFridge(leftControllerAim) || RaycastHitsFridge(rightControllerAim);
        }

        if (!hitFridge && cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, raycastLayers, QueryTriggerInteraction.Ignore))
            {
                hitFridge = IsTargetFridge(hit.transform);
            }
        }

        if (!hitFridge) return;

        if (requireHoldingBucket && !IsHoldingIceBucket())
        {
            onStatusMessage?.Invoke("Hold the ice bucket first before getting ice");
            return;
        }

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

    private bool RaycastHitsFridge(Transform aimOrigin)
    {
        if (aimOrigin == null) return false;

        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, raycastLayers, QueryTriggerInteraction.Ignore))
        {
            return IsTargetFridge(hit.transform);
        }
        return false;
    }

    private bool IsTargetFridge(Transform hitTransform)
    {
        if (hitTransform == null) return false;
        return hitTransform == transform ||
               hitTransform.IsChildOf(transform) ||
               transform.IsChildOf(hitTransform);
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

        if (fridgeIceAudio == null)
        {
            fridgeIceAudio = GetComponentInChildren<FridgeIceAudio>();
            if (fridgeIceAudio == null)
            {
                fridgeIceAudio = FindAnyObjectByType<FridgeIceAudio>();
            }
        }

        if (fridgeIceAudio != null)
        {
            fridgeIceAudio.PlayIceDispenseSound();
        }

        if (taskManager == null)
        {
            taskManager = FindAnyObjectByType<Case2TaskManager>();
        }
        taskManager?.CompleteStep4_IceInBucket();

        onStatusMessage?.Invoke("Ice added to bucket");
        onIceFilled?.Invoke();
    }
}
