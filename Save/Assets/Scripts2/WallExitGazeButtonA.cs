using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

public class WallExitGazeButtonA : MonoBehaviour
{
    [Header("Exit Handler & Scene")]
    public VRTriggerExitToMain exitHandler;
    public string mainMenuSceneName = "Main Menu";

    [Header("Gaze Settings (Easier Aim)")]
    public Transform playerCamera;
    public float maxLookDistance = 15f;
    public float gazeRadius = 0.2f;
    public LayerMask raycastLayers = ~0;
    public bool showDebugRay = true;

    [Header("Optional Visual Feedback")]
    public Image buttonImage;
    public Color normalColor = Color.white;
    public Color lookAtColor = new Color(0.7f, 1f, 0.7f, 1f);

    [Header("Editor Test Key")]
    public bool allowEditorKeyA = true;

    private readonly List<UnityEngine.XR.InputDevice> rightDevices = new List<UnityEngine.XR.InputDevice>();
    private bool wasAPressedLastFrame = false;
    private bool isExiting = false;

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (exitHandler == null)
            exitHandler = FindFirstObjectByType<VRTriggerExitToMain>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (isExiting || !gameObject.activeInHierarchy)
            return;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (showDebugRay && playerCamera != null)
            Debug.DrawRay(playerCamera.position, playerCamera.forward * maxLookDistance, Color.green);

        bool isLookingAtButton = IsCameraLookingAtThis();

        if (buttonImage != null)
            buttonImage.color = isLookingAtButton ? lookAtColor : normalColor;

        if (ConsumeButtonADown() && isLookingAtButton)
        {
            isExiting = true;

            if (exitHandler != null)
            {
                exitHandler.ExitToMainMenu();
            }
            else if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
    }

    private bool IsCameraLookingAtThis()
    {
        if (playerCamera == null) return false;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (gazeRadius > 0.001f)
        {
            if (Physics.SphereCast(ray, gazeRadius, out RaycastHit sphereHit, maxLookDistance, raycastLayers, QueryTriggerInteraction.Collide))
            {
                return sphereHit.transform == transform || sphereHit.transform.IsChildOf(transform);
            }
            return false;
        }

        if (Physics.Raycast(ray, out RaycastHit hit, maxLookDistance, raycastLayers, QueryTriggerInteraction.Collide))
        {
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        return false;
    }

    private bool ConsumeButtonADown()
    {
        bool pressedNow = IsRightPrimaryButtonAPressed() ||
                          (allowEditorKeyA && IsKeyboardAPressed());

        bool downThisFrame = pressedNow && !wasAPressedLastFrame;
        wasAPressedLastFrame = pressedNow;
        return downThisFrame;
    }

    private bool IsRightPrimaryButtonAPressed()
    {
        rightDevices.Clear();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.Controller | UnityEngine.XR.InputDeviceCharacteristics.Right,
            rightDevices);

        for (int i = 0; i < rightDevices.Count; i++)
        {
            var device = rightDevices[i];
            if (!device.isValid) continue;

            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool isPressed) && isPressed)
                return true;
        }
        return false;
    }

    private bool IsKeyboardAPressed()
    {
        if (Keyboard.current != null && Keyboard.current.aKey.isPressed)
            return true;
        return Input.GetKey(KeyCode.A);
    }
}