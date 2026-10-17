using UnityEngine;
using UnityEngine.InputSystem;


public class UIController : MonoBehaviour
{
    private Transform player;
    private Transform cameraTransform;
    private PlayerInput inputSystem;

    public PlayerInput GetPlayerInput()
    {
        if(inputSystem == null)
        {
            inputSystem = FindFirstObjectByType<PlayerInput>();
        }
        return inputSystem;
    }
    public Transform GetPlayer()
    {
        if(player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        return player;
    }
    public Transform GetCameraTransform()
    {
        if(cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        return cameraTransform;
    }
}
