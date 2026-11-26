using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("ชื่อไฟล์ Scene (ต้องตรงกับใน Assets/Scenes)")]
    public string mainMenuSceneName = "Main Menu";
    public string selectCaseSceneName = "Select Case";
    public string howToPlaySceneName = "How to Play";
    public string case1SceneName = "Case1_ArmFracture";
    public string case2SceneName = "Case2_FingerAmputation";

    [Header("ตั้งค่าเสียงกดปุ่ม")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    [Range(0f, 1f)]
    public float clickVolume = 1f;
    [Tooltip("หน่วงเวลารอเสียงกดปุ่มก่อนเปลี่ยนฉากหรือออก (วินาที)")]
    [Range(0f, 0.5f)]
    public float clickSoundDelay = 0.15f;

    private bool isBusy = false;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // เรียกเล่นเสียงอย่างเดียว (ใช้กับปุ่มที่ไม่ได้เปลี่ยนฉากหรือออก)
    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound, clickVolume);
    }

    // กดปุ่ม BACK กลับไปหน้า Main Menu
    public void OnBackClicked()
    {
        LoadSceneWithSound(mainMenuSceneName);
    }

    public void OnPlayClicked()
    {
        LoadSceneWithSound(selectCaseSceneName);
    }

    public void OnHowToPlayClicked()
    {
        LoadSceneWithSound(howToPlaySceneName);
    }

    public void OnCase1Clicked()
    {
        LoadSceneWithSound(case1SceneName);
    }

    public void OnCase2Clicked()
    {
        LoadSceneWithSound(case2SceneName);
    }

    public void OnExitClicked()
    {
        if (isBusy) return;
        StartCoroutine(PlaySoundAndQuit());
    }

    private void LoadSceneWithSound(string sceneName)
    {
        if (isBusy || string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(PlaySoundAndLoadScene(sceneName));
    }

    private IEnumerator PlaySoundAndLoadScene(string sceneName)
    {
        isBusy = true;
        PlayClickSound();

        if (clickSoundDelay > 0f)
            yield return new WaitForSecondsRealtime(clickSoundDelay);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator PlaySoundAndQuit()
    {
        isBusy = true;
        PlayClickSound();

        if (clickSoundDelay > 0f)
            yield return new WaitForSecondsRealtime(clickSoundDelay);

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}