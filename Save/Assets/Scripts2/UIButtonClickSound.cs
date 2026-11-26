using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButtonClickSound : MonoBehaviour
{
    [Header("ตัวเล่นเสียงและไฟล์เสียงกดปุ่ม")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("หน่วงเวลาก่อนเปลี่ยนฉาก (วินาที)")]
    [Range(0f, 0.5f)]
    public float sceneLoadDelay = 0.15f;

    private bool isTransitioning = false;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // ใช้กับปุ่มทั่วไปที่ไม่ได้เปลี่ยนฉากทันที
    public void PlayClick()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound, volume);
    }

    // ใช้กับปุ่มที่กดแล้วเปลี่ยนไปอีกฉาก เช่น PLAY หรือ How to play
    public void PlayClickAndLoadScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(PlayAndLoadRoutine(sceneName));
    }

    // ใช้กับปุ่ม Exit ออกจากแอป
    public void PlayClickAndQuit()
    {
        if (isTransitioning) return;
        StartCoroutine(PlayAndQuitRoutine());
    }

    private IEnumerator PlayAndLoadRoutine(string sceneName)
    {
        isTransitioning = true;
        PlayClick();
        if (sceneLoadDelay > 0f)
            yield return new WaitForSecondsRealtime(sceneLoadDelay);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator PlayAndQuitRoutine()
    {
        isTransitioning = true;
        PlayClick();
        if (sceneLoadDelay > 0f)
            yield return new WaitForSecondsRealtime(sceneLoadDelay);
        Application.Quit();
    }
}