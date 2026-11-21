using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class GasStoveController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลากออบเจกต์ FireAnimado จาก Hierarchy มาใส่")]
    public GameObject flameObject;

    [Tooltip("ลูกบิดเตาแก๊ส (ถ้ามี) เพื่อหมุนตอนปิด")]
    public Transform knobTransform;
    public Vector3 offKnobLocalEuler = new Vector3(0f, 0f, 90f);

    [Header("Task Link")]
    public Case2TaskManager taskManager;

    [Header("Interaction Settings")]
    public float interactRange = 4.0f;
    public bool isGasOn = true;

    [Header("Events")]
    public UnityEvent onGasTurnedOff;
    public UnityEvent<string> onStatusMessage;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        if (taskManager == null)
        {
            taskManager = FindAnyObjectByType<Case2TaskManager>();
        }

        if (flameObject != null)
        {
            flameObject.SetActive(isGasOn);
        }
    }

    private void Update()
    {
        if (!isGasOn || cam == null) return;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            TryTurnOffGas();
        }
    }

    private void TryTurnOffGas()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            bool hitStove = (hit.transform == transform || hit.transform.IsChildOf(transform));
            bool hitFlame = (flameObject != null && (hit.transform == flameObject.transform || hit.transform.IsChildOf(flameObject.transform)));

            if (hitStove || hitFlame)
            {
                TurnOffGas();
            }
        }
    }

    public void TurnOffGas()
    {
        if (!isGasOn) return;
        isGasOn = false;

        if (flameObject != null)
        {
            flameObject.SetActive(false);
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