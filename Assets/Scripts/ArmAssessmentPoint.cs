using UnityEngine;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ArmAssessmentPoint : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 3.5f;
    [SerializeField] private bool requireLookingAtArm = true;
    [SerializeField] private LayerMask lookRayMask = ~0;
    [SerializeField] private Camera playerCamera;

    [Header("Disable Collider After Assessment")]
    [SerializeField] private Collider assessCollider;

    [Header("Messages")]
    [SerializeField] private string lookPromptMessage = "Press [E] to assess injured arm";
    [SerializeField] private string assessmentCompleteMessage = "Assessment complete";

    [Header("Prompt UI (Shown ONLY while looking at arm)")]
    [SerializeField] private TMP_Text promptTextUI;
    [SerializeField] private GameObject promptPanelUI;

    [Header("Events")]
    public UnityEvent onArmAssessed;

    private bool isAssessed = false;
    private bool isLookingAtArm = false;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        assessCollider = col;
    }

    private void Awake()
    {
        if (assessCollider == null)
            assessCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (isAssessed)
        {
            SetPromptVisible(false);
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        isLookingAtArm = CheckLookingAtArm();

        if (isLookingAtArm)
        {
            SetPromptVisible(true, lookPromptMessage);

            if (Input.GetKeyDown(interactKey))
            {
                PerformAssessment();
            }
        }
        else
        {
            SetPromptVisible(false);
        }
    }

    private bool CheckLookingAtArm()
    {
        if (playerCamera == null) return false;

        if (!requireLookingAtArm)
        {
            float dist = Vector3.Distance(playerCamera.transform.position, transform.position);
            return dist <= interactDistance;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, lookRayMask, QueryTriggerInteraction.Collide))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform))
            {
                return true;
            }
        }

        return false;
    }

    public void PerformAssessment()
    {
        if (isAssessed) return;

        Case1TaskManager manager = Case1TaskManager.Instance;
        if (manager != null && manager.CompleteTask1_AssessArm())
        {
            isAssessed = true;
            isLookingAtArm = false;
            SetPromptVisible(false);
            DisableAssessCollider();
            manager.ShowWarning(assessmentCompleteMessage);
            onArmAssessed?.Invoke();
        }
    }

    private void DisableAssessCollider()
    {
        if (assessCollider == null)
            assessCollider = GetComponent<Collider>();

        if (assessCollider != null)
        {
            assessCollider.enabled = false;
        }
    }

    private void SetPromptVisible(bool visible, string message = "")
    {
        if (promptPanelUI != null)
            promptPanelUI.SetActive(visible);

        if (promptTextUI != null)
        {
            promptTextUI.gameObject.SetActive(visible);
            if (visible)
                promptTextUI.text = message;
        }
    }
}