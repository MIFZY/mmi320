using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GasStoveController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลากออบเจกต์ FireAnimado จาก Hierarchy มาใส่")]
    public GameObject flameObject;

    [Tooltip("ลูกบิดเตาแก๊ส (ถ้ามี) เพื่อหมุนตอนปิด")]
    public Transform knobTransform;
    public Vector3 offKnobLocalEuler = new Vector3(0f, 0f, 90f);

    [Header("VR Aim & Trigger (Left / Right)")]
    [Tooltip("ลาก Left Controller / Left Ray Interactor (ถ้าเว้นว่างจะค้นหาอัตโนมัติ)")]
    public Transform leftControllerAim;

    [Tooltip("ลาก Right Controller / Right Ray Interactor (ถ้าเว้นว่างจะค้นหาอัตโนมัติ)")]
    public Transform rightControllerAim;

    [Tooltip("Layer ที่ใช้ยิง Raycast หาตัวเตาหรือไฟ")]
    public LayerMask raycastLayers = ~0;

    [Header("Audio")]
    [Tooltip("ลากออบเจกต์ที่มีคอมโพเนนต์ StoveFireAudio มาใส่")]
    public StoveFireAudio stoveFireAudio;

    [Header("Task Link")]
    public Case2TaskManager taskManager;

    [Header("Interaction Settings")]
    public float interactRange = 4.0f;
    public bool isGasOn = true;

    [Header("Events")]
    public UnityEvent onGasTurnedOff;
    public UnityEvent<string> onStatusMessage;

    private Camera cam;
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

        if (stoveFireAudio == null)
        {
            stoveFireAudio = GetComponentInChildren<StoveFireAudio>();
            if (stoveFireAudio == null)
            {
                stoveFireAudio = FindAnyObjectByType<StoveFireAudio>();
            }
        }

        AutoFindControllerAims();

        if (flameObject != null)
        {
            flameObject.SetActive(isGasOn);
        }

        if (stoveFireAudio != null)
        {
            stoveFireAudio.ToggleFireSound(isGasOn);
        }
    }

    private void Update()
    {
        if (!isGasOn) return;

        bool triggerDown = ConsumeAnyTriggerDown();
        var kb = Keyboard.current;
        bool keyboardEDown = kb != null && kb.eKey.wasPressedThisFrame;

        if (triggerDown || keyboardEDown)
        {
            TryTurnOffGas(triggerDown);
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

    private void TryTurnOffGas(bool fromVRTrigger)
    {
        if (fromVRTrigger)
        {
            AutoFindControllerAims();

            if (RaycastHitsStoveOrFlame(leftControllerAim) || RaycastHitsStoveOrFlame(rightControllerAim))
            {
                TurnOffGas();
                return;
            }
        }

        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, raycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsTargetStoveOrFlame(hit.transform))
                {
                    TurnOffGas();
                }
            }
        }
    }

    private bool RaycastHitsStoveOrFlame(Transform aimOrigin)
    {
        if (aimOrigin == null) return false;

        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, raycastLayers, QueryTriggerInteraction.Ignore))
        {
            return IsTargetStoveOrFlame(hit.transform);
        }
        return false;
    }

    private bool IsTargetStoveOrFlame(Transform hitTransform)
    {
        if (hitTransform == null) return false;

        bool hitStove = (hitTransform == transform || hitTransform.IsChildOf(transform));
        bool hitFlame = (flameObject != null && (hitTransform == flameObject.transform || hitTransform.IsChildOf(flameObject.transform)));

        return hitStove || hitFlame;
    }

    public void TurnOffGas()
    {
        if (!isGasOn) return;
        isGasOn = false;

        if (flameObject != null)
        {
            flameObject.SetActive(false);
        }

        if (stoveFireAudio != null)
        {
            stoveFireAudio.TurnOffFireSound();
        }

        if (knobTransform != null)
        {
            knobTransform.localEulerAngles = offKnobLocalEuler;
        }

        if (taskManager == null)
        {
            taskManager = FindAnyObjectByType<Case2TaskManager>();
        }

        if (taskManager != null)
        {
            taskManager.CompleteStep1_Safety();
        }

        onStatusMessage?.Invoke("ปิดเตาแก๊สเรียบร้อยแล้ว");
        onGasTurnedOff?.Invoke();
    }
}
