using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference clickAction;

    private void OnEnable()
    {
        pointAction.action.Enable();
        clickAction.action.Enable();

        clickAction.action.performed += OnClick;
    }

    private void OnDisable()
    {
        clickAction.action.performed -= OnClick;

        pointAction.action.Disable();
        clickAction.action.Disable();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = pointAction.action.ReadValue<Vector2>();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            CharacterSelect character =
                hit.collider.GetComponentInParent<CharacterSelect>();

            if (character != null)
            {
                character.Select();
            }
        }
    }
}