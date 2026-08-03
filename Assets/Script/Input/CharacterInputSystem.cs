using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))] // 🔑 強制要求物件要有 PlayerInput 組件
public class CharacterInputSystem : MonoBehaviour
{
    private PlayerInput _playerInput;

    // 🔑 直接從 PlayerInput 讀取 Action
    public Vector2 playerMovement => _playerInput.actions["Movement"].ReadValue<Vector2>();
    public Vector2 playerCamera   => _playerInput.actions["CameraLook"].ReadValue<Vector2>();
    
    public bool playerInteract => _playerInput.actions["Interact"].triggered;
    public bool playerPause    => _playerInput.actions["Esc"].triggered;
    public bool playerLAtk     => _playerInput.actions["LAtk"].triggered;
    public bool playerRoll     => _playerInput.actions["Roll"].triggered;

    public bool playerRAtk  => _playerInput.actions["RAtk"].phase == InputActionPhase.Performed;
    public bool playerDefen => _playerInput.actions["Defen"].phase == InputActionPhase.Performed;
    public bool playerRun   => _playerInput.actions["Run"].phase == InputActionPhase.Performed;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }
}