using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BagInIceBucketAudio : MonoBehaviour
{
    [Header("Bag In Ice Bucket Sound Clip (เสียงวางถุงลงน้ำแข็ง / เสียงน้ำแข็งกระทบกัน)")]
    [SerializeField] private AudioClip bagInIceClip;

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

    // เรียกเมื่อเอาถุงนิ้วสแนปลงถังน้ำแข็งสำเร็จ
    public void PlayBagInIceSound()
    {
        if (bagInIceClip == null || audioSource == null) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(bagInIceClip, volume);
    }
}