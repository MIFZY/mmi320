using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VRTriggerExitToMain : MonoBehaviour
{
    [Header("ชื่อฉากหน้าหลัก (ต้องตรงกับใน Assets/Scenes)")]
    public string mainMenuSceneName = "Main Menu";

    [Header("ตั้งค่าเสียงกดปุ่ม")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    [Range(0f, 1f)]
    public float clickVolume = 1f;

    private bool isExiting = false;

    private void Start()
    {
        Time.timeScale = 1f;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayButtonSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }

    // ผูกกับปุ่ม EXIT ใน On Click () หรือเรียกจากสคริปต์ Trigger
    public void ExitToMainMenu()
    {
        if (isExiting) return;
        isExiting = true;

        Time.timeScale = 1f;
        StartCoroutine(PlaySoundAndLoadScene());
    }

    private IEnumerator PlaySoundAndLoadScene()
    {
        float delay = 0f;

        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
            delay = Mathf.Min(clickSound.length, 0.35f);
        }

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }
}