using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ArmSplintAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip newspaperSnapClip;
    [SerializeField] private AudioClip clothWrapClip;

    [Header("Playback Settings")]
    [SerializeField][Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void PlayNewspaperSnapSound()
    {
        PlayOneShotClip(newspaperSnapClip);
    }

    public void PlayClothWrapSound()
    {
        PlayOneShotClip(clothWrapClip);
    }

    private void PlayOneShotClip(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.loop = false;
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume);
    }
}