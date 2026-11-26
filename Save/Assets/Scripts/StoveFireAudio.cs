using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class StoveFireAudio : MonoBehaviour
{
    [Header("Fire / Stove Sound Clip (เสียงไฟลุก / เสียงเตาแก๊สแบบวนซ้ำ)")]
    [SerializeField] private AudioClip fireLoopClip;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float fireVolume = 0.65f;
    [Tooltip("ความดัง 3D: 0 = ได้ยินเท่ากันทั้งฉาก, 1 = ดังเฉพาะตอนอยู่ใกล้เตา")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 0.85f;
    [SerializeField] private float minDistance = 1.0f;
    [SerializeField] private float maxDistance = 8.0f;

    [Header("Fade Settings (ช่วยให้ตอนกดปิดเสียงค่อย ๆ เงียบลงไม่ตัดกระตุก)")]
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private bool playOnStartIfStoveOn = true;

    private AudioSource audioSource;
    private bool isFireOn = false;
    private bool isFadingOut = false;
    private float fadeTimer = 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = fireLoopClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.volume = 0f;
    }

    private void Start()
    {
        if (playOnStartIfStoveOn && fireLoopClip != null)
        {
            TurnOnFireSound();
        }
    }

    private void Update()
    {
        if (!isFadingOut || audioSource == null) return;

        if (fadeOutDuration <= 0f)
        {
            StopFireImmediately();
            return;
        }

        fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fadeTimer / fadeOutDuration);
        audioSource.volume = Mathf.Lerp(fireVolume, 0f, t);

        if (t >= 1f)
        {
            StopFireImmediately();
        }
    }

    // เรียกเมื่อเปิดเตา / ไฟติด
    public void TurnOnFireSound()
    {
        if (fireLoopClip == null) return;

        isFireOn = true;
        isFadingOut = false;
        fadeTimer = 0f;

        if (audioSource.clip != fireLoopClip)
            audioSource.clip = fireLoopClip;

        audioSource.loop = true;
        audioSource.volume = fireVolume;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    // เรียกเมื่อกดปิดเตา / ดับไฟ
    public void TurnOffFireSound()
    {
        if (!isFireOn && !audioSource.isPlaying) return;

        isFireOn = false;
        fadeTimer = 0f;

        if (fadeOutDuration > 0f)
        {
            isFadingOut = true;
        }
        else
        {
            StopFireImmediately();
        }
    }

    // เรียกสลับเปิด/ปิดเสียงไฟในปุ่มเดียวได้
    public void ToggleFireSound(bool stoveIsOn)
    {
        if (stoveIsOn)
            TurnOnFireSound();
        else
            TurnOffFireSound();
    }

    private void StopFireImmediately()
    {
        isFadingOut = false;
        audioSource.volume = 0f;
        audioSource.Stop();
    }
}