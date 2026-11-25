using UnityEngine;
using UnityEngine.InputSystem;

public class VRPanelCloseByButtonA : MonoBehaviour
{
    [SerializeField] private GameObject targetUI;
    [SerializeField] private InputActionProperty closeButtonAction;

    private void OnEnable()
    {
        if (closeButtonAction.action != null)
        {
            closeButtonAction.action.Enable();
            closeButtonAction.action.performed += OnClosePressed;
        }
    }

    private void OnDisable()
    {
        if (closeButtonAction.action != null)
        {
            closeButtonAction.action.performed -= OnClosePressed;
            closeButtonAction.action.Disable();
        }
    }

    private void OnClosePressed(InputAction.CallbackContext context)
    {
        if (targetUI != null)
        {
            targetUI.SetActive(false);
        }
    }
}
