using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("Footstep Audio Clips (ใส่ไฟล์เสียงฝีเท้า 1 ไฟล์ขึ้นไป)")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Step Settings (ระยะก้าวและความเร็วขั้นต่ำ)")]
    [Tooltip("ระยะทางเป็นเมตรต่อ 1 ก้าว (เช่น 1.8 - 2.2 เมตร)")]
    [SerializeField] private float stepDistance = 2.0f;
    [Tooltip("ความเร็วขั้นต่ำที่เริ่มนับก้าว (กันเสียงดังตอนยืนนิ่ง)")]
    [SerializeField] private float minMoveSpeed = 0.2f;

    [Header("Volume & Pitch Variation")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.7f;
    [SerializeField] private float minPitch = 0.92f;
    [SerializeField] private float maxPitch = 1.08f;

    [Header("Ground Check (Optional)")]
    [SerializeField] private CharacterController characterController;

    private AudioSource audioSource;
    private Vector3 lastPosition;
    private float distanceAccumulated = 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound สำหรับเสียงของผู้เล่นเอง

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 currentPosition = transform.position;
        Vector3 horizontalDelta = new Vector3(
            currentPosition.x - lastPosition.x,
            0f,
            currentPosition.z - lastPosition.z
        );

        float movedDistance = horizontalDelta.magnitude;
        float currentSpeed = Time.deltaTime > 0f ? movedDistance / Time.deltaTime : 0f;

        lastPosition = currentPosition;

        // ถ้ามี CharacterController ให้เช็กว่ายืนอยู่บนพื้นด้วย
        if (characterController != null && !characterController.isGrounded)
        {
            return;
        }

        if (currentSpeed < minMoveSpeed)
        {
            return;
        }

        distanceAccumulated += movedDistance;

        if (distanceAccumulated >= stepDistance)
        {
            PlayFootstep();
            distanceAccumulated = 0f;
        }
    }

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int randomIndex = Random.Range(0, footstepClips.Length);
        AudioClip clip = footstepClips[randomIndex];
        if (clip == null) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
    }
}