using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BagFingerAudio : MonoBehaviour
{
    [Header("Finger In Bag Sound Clip (เสียงใส่ของลงถุงพลาสติก / เสียงวางลงถุง)")]
    [SerializeField] private AudioClip fingerInBagClip;

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

    // เรียกเมื่อเอานิ้วเข้าถุงสำเร็จ
    public void PlayFingerInBagSound()
    {
        if (fingerInBagClip == null || audioSource == null) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(fingerInBagClip, volume);
    }
}