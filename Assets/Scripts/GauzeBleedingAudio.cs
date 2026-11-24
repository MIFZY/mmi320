using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GauzeBleedingAudio : MonoBehaviour
{
    [Header("Gauze Stop Bleeding Sound Clip (เสียงแปะผ้าก๊อซ / เสียงพันผ้าห้ามเลือด)")]
    [SerializeField] private AudioClip gauzeClip;

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
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;
    }

    // เรียกเมื่อเอาผ้าก๊อซสแนปที่มือคนเจ็บเพื่อห้ามเลือดสำเร็จ
    public void PlayGauzeSound()
    {
        if (gauzeClip == null || audioSource == null) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(gauzeClip, volume);
    }
}