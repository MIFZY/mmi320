using UnityEngine;

public class MainMenuButtonSound : MonoBehaviour
{
    [Header("ตัวเล่นเสียงและไฟล์เสียงกดปุ่ม")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound, volume);
    }
}