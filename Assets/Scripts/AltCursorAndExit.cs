using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AltCursorAndExit : MonoBehaviour
{
    [Header("ชื่อฉากหน้าหลัก (ต้องตรงกับใน Assets/Scenes)")]
    public string mainMenuSceneName = "Main Menu";

    [Header("เริ่มฉากมาปลดล็อกเมาส์เลยไหม")]
    public bool unlockCursorOnStart = true;

    [Header("ตั้งค่าเสียงกดปุ่ม")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    [Range(0f, 1f)]
    public float clickVolume = 1f;

    private bool isCursorUnlocked = true;
    private bool isExiting = false;

    void Start()
    {
        Time.timeScale = 1f;
        SetCursorState(unlockCursorOnStart);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (WasPPressed())
        {
            SetCursorState(!isCursorUnlocked);
        }
    }

    private bool WasPPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            return true;
        }
#endif
        return Input.GetKeyDown(KeyCode.P);
    }

    public void SetCursorState(bool unlocked)
    {
        isCursorUnlocked = unlocked;
        Cursor.lockState = unlocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlocked;
    }

    // ใช้เรียกกับปุ่มทั่วไปที่ไม่ได้เปลี่ยนฉาก
    public void PlayButtonSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }

    // ผูกกับปุ่ม EXIT TO MAIN: เล่นเสียงแล้วค่อยเปลี่ยนฉาก
    public void ExitToMainMenu()
    {
        if (isExiting) return;
        isExiting = true;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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