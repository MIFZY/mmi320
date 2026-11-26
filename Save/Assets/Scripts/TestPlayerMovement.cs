using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float gravity = -9.81f;

    [Header("Mouse Look Settings")]
    public bool enableMouseLook = true;
    public float mouseSensitivity = 2.0f;
    public Transform cameraTransform;

    private CharacterController controller;
    private float pitch = 0f;
    private float verticalVelocity = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
    }

    private void HandleLook()
    {
        if (!enableMouseLook || cameraTransform == null) return;

        // กดคลิกซ้ายหรือคลิกขวาค้าง แล้วขยับเมาส์เพื่อหันมุมกล้อง
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
            cameraTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }

    private void HandleMove()
    {
        float x = Input.GetAxis("Horizontal"); // ปุ่ม A, D
        float z = Input.GetAxis("Vertical");   // ปุ่ม W, S

        Vector3 moveDir = (transform.right * x + transform.forward * z).normalized;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = moveDir * moveSpeed;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }
}