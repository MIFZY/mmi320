using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class HowToPlayDualSectionController : MonoBehaviour
{
    [Header("Main Window Root (แผงหน้าต่าง How to Play ทั้งหมดที่ต้องการเปิด/ปิด)")]
    [SerializeField] private GameObject howToPlayWindowRoot;
    [SerializeField] private Button closeWindowButton;
    [SerializeField] private KeyCode toggleWindowKey = KeyCode.H;

    [Header("Section 1: Grab (การจับ)")]
    [SerializeField] private GameObject grabSectionPanel;
    [SerializeField] private Image grabImage;
    [SerializeField] private Sprite grabSprite;
    [SerializeField] private TMP_Text grabTitleText;
    [SerializeField] private TMP_Text grabDetailText;

    [SerializeField] private string grabHeaderLabel = "1. GRAB & BUTTONS";
    [TextArea(3, 8)]
    [SerializeField]
    private string grabDetailMessage =
        "• Top Button (Trigger): Curls index & thumb\n" +
        "• Bottom Button (Grip): Curls middle, ring & pinky\n" +
        "• Grab and release items with either button\n" +
        "• [A] Select    • [B] Back";

    [Header("Section 2: Move & Warp (ซ้ายเดิน / ขวาวาป)")]
    [SerializeField] private GameObject moveSectionPanel;
    [SerializeField] private Image moveImage;
    [SerializeField] private Sprite moveSprite;
    [SerializeField] private TMP_Text moveTitleText;
    [SerializeField] private TMP_Text moveDetailText;

    [SerializeField] private string moveHeaderLabel = "2. MOVE & WARP";
    [TextArea(3, 8)]
    [SerializeField]
    private string moveDetailMessage =
        "• Left Thumbstick (Walk): Push to move around\n" +
        "• Right Thumbstick (Warp): Aim target & release to warp\n" +
        "• Close this window to practice grabbing items in the room!";

    [Header("Player Control Lock While Window Open (Optional)")]
    [SerializeField] private bool lockCursorWhenWindowClosed = true;
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour playerGrabScript;

    [Header("Scene Names")]
    [SerializeField] private string case1SceneName = "Case1_ArmFracture";
    [SerializeField] private string case2SceneName = "Case2_FingerAmputation";
    [SerializeField] private string backSceneName = "";

    private bool isWindowOpen = true;
    private bool isLoadingScene = false;

    private void Awake()
    {
        if (closeWindowButton != null)
        {
            closeWindowButton.onClick.RemoveListener(CloseHowToPlayWindow);
            closeWindowButton.onClick.AddListener(CloseHowToPlayWindow);
        }

        RefreshAllSections();
    }

    private void Start()
    {
        OpenHowToPlayWindow();
    }

    private void Update()
    {
        if (isLoadingScene) return;

        if (Keyboard.current != null && toggleWindowKey != KeyCode.None)
        {
            // กดปุ่ม H เพื่อเปิด/ปิดหน้าต่าง How to Play ซ้ำได้ตลอดเวลา
            if (Input.GetKeyDown(toggleWindowKey))
            {
                if (isWindowOpen)
                    CloseHowToPlayWindow();
                else
                    OpenHowToPlayWindow();
            }
        }
    }

    public void OpenHowToPlayWindow()
    {
        isWindowOpen = true;

        if (howToPlayWindowRoot != null)
            howToPlayWindowRoot.SetActive(true);

        RefreshAllSections();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerGrabScript != null) playerGrabScript.enabled = false;
    }

    public void CloseHowToPlayWindow()
    {
        isWindowOpen = false;

        if (howToPlayWindowRoot != null)
            howToPlayWindowRoot.SetActive(false);

        if (lockCursorWhenWindowClosed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerGrabScript != null) playerGrabScript.enabled = true;
    }

    public void RefreshAllSections()
    {
        if (grabSectionPanel != null) grabSectionPanel.SetActive(true);
        if (grabTitleText != null) grabTitleText.text = grabHeaderLabel;
        if (grabDetailText != null) grabDetailText.text = grabDetailMessage;
        if (grabImage != null)
        {
            grabImage.preserveAspect = true;
            grabImage.sprite = grabSprite;
            grabImage.gameObject.SetActive(grabSprite != null);
        }

        if (moveSectionPanel != null) moveSectionPanel.SetActive(true);
        if (moveTitleText != null) moveTitleText.text = moveHeaderLabel;
        if (moveDetailText != null) moveDetailText.text = moveDetailMessage;
        if (moveImage != null)
        {
            moveImage.preserveAspect = true;
            moveImage.sprite = moveSprite;
            moveImage.gameObject.SetActive(moveSprite != null);
        }
    }

    public void LoadCase1Scene()
    {
        if (isLoadingScene || string.IsNullOrEmpty(case1SceneName)) return;
        isLoadingScene = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(case1SceneName);
    }

    public void LoadCase2Scene()
    {
        if (isLoadingScene || string.IsNullOrEmpty(case2SceneName)) return;
        isLoadingScene = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(case2SceneName);
    }

    public void LoadBackScene()
    {
        if (isLoadingScene || string.IsNullOrEmpty(backSceneName)) return;
        isLoadingScene = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(backSceneName);
    }
}