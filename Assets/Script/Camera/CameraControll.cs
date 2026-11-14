using UnityEngine;
using Unity.Cinemachine;

public class CameraControll : MonoBehaviour
{
    [SerializeField] private CharacterInputSystem inputSystem;
    [SerializeField] private CinemachineInputAxisController axisController;
    private void Update()
    {
        //axisController.SetAxisValue(0, inputSystem.playerCamera.x); 
        //axisController.SetAxisValue(1, inputSystem.playerCamera.y);
    }
}
