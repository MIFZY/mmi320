using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneAmbientAudio : MonoBehaviour
{
    [Header("Ambient Audio Clip (เสียงบรรยากาศฉาก เช่น เสียงในห้อง / ลมเบา ๆ / แอร์)")]
    [SerializeField] private AudioClip ambientClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float targetVolume = 0.25f;
    [Tooltip("ระยะเวลาค่อย ๆ เพิ่มเสียงตอนเริ่มฉาก (วินาที)")]
    [SerializeField] private float fadeInDuration = 1.5f;

    private AudioSource audioSource;
    private float timer = 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = ambientClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D ambient sound ได้ยินสม่ำเสมอทั่วฉาก
        audioSource.volume = 0f;
    }

    private void Start()
    {
        if (ambientClip != null)
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (ambientClip == null || !audioSource.isPlaying) return;

        if (fadeInDuration > 0f && timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / fadeInDuration);
        }
        else
        {
            audioSource.volume = targetVolume;
        }
    }
}