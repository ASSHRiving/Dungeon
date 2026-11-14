using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))] // 🔑 強制要求物件要有 PlayerInput 組件
public class CharacterInputSystem : MonoBehaviour
{
    private PlayerInput playerInput;

    // 🔑 直接從 PlayerInput 讀取 Action
    public Vector2 playerMovement => playerInput.actions["Movement"].ReadValue<Vector2>();
    public Vector2 playerCamera   => playerInput.actions["CameraLook"].ReadValue<Vector2>();
    
    public bool playerInteract => playerInput.actions["Interact"].triggered;
    public bool playerPause    => playerInput.actions["Esc"].triggered;
    public bool playerLAtk     => playerInput.actions["LAtk"].triggered;

    public bool playerRoll  => playerInput.actions["RAtk"].triggered;
    public bool playerDefen => playerInput.actions["Defen"].phase == InputActionPhase.Performed;
    public bool playerRun   => playerInput.actions["Run"].phase == InputActionPhase.Performed;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
}