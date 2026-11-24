using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FridgeIceAudio : MonoBehaviour
{
    [Header("Ice Dispense Sound Clips (เสียงกดน้ำแข็ง / เสียงน้ำแข็งไหลลงถัง)")]
    [SerializeField] private AudioClip iceDispenseClip;

    [Header("Playback Mode")]
    [Tooltip("ติ๊กถ้าอยากให้เล่นวนระหว่างกด และหยุดเมื่อหยุดกด / ปล่อยติ๊กถ้าอยากให้เล่น 1 ครั้งต่อการกด E")]
    [SerializeField] private bool loopWhileDispensing = false;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.75f;
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 0.85f;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loopWhileDispensing;
        audioSource.spatialBlend = spatialBlend;
    }

    // เรียกเมื่อถือถังไปกด E ที่ตู้เย็นเพื่อรับน้ำแข็ง
    public void PlayIceDispenseSound()
    {
        if (iceDispenseClip == null || audioSource == null) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        if (loopWhileDispensing)
        {
            audioSource.clip = iceDispenseClip;
            audioSource.loop = true;
            audioSource.volume = volume;
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(iceDispenseClip, volume);
        }
    }

    // เรียกเมื่อหยุดกด / เสร็จสิ้นการกดน้ำแข็ง (กรณีใช้โหมด loopWhileDispensing)
    public void StopIceDispenseSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}