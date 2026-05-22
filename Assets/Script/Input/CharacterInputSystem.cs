using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInputSystem : MonoBehaviour
{

    private InputController _inputController;

    public Vector2 playerMovement
    {
        get => _inputController.PlayerInput.Movement.ReadValue<Vector2>();
    }
    public Vector2 playerCamera
    {
        get => _inputController.PlayerInput.CameraLook.ReadValue<Vector2>();
    }
     public bool playerInteract
    {
        get => _inputController.PlayerInput.Interact.triggered;
    }

    public bool playerLAtk
    {
        get => _inputController.PlayerInput.LAtk.triggered;
    }
    public bool playerRAtk
    {
        get => _inputController.PlayerInput.RAtk.phase == InputActionPhase.Performed;
    }
    public bool playerDefen
    {
        get => _inputController.PlayerInput.Defen.phase == InputActionPhase.Performed;
    }

    public bool playerRun
    {
        get => _inputController.PlayerInput.Run.phase == InputActionPhase.Performed;
    }

    public bool playerRoll
    {
        get => _inputController.PlayerInput.Roll.triggered;
    }

    public bool playerCrouch
    {
        get => _inputController.PlayerInput.Crouch.triggered;
    }


    private void Awake()
    {
        if (_inputController == null)
            _inputController = new InputController();
    }

    private void OnEnable()
    {
        _inputController.Enable();
    }

    private void OnDisable()
    {
        _inputController.Disable();
    }
}
