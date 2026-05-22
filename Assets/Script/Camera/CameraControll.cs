using UnityEngine;
using Unity.Cinemachine;

public class CameraControll : MonoBehaviour
{
    [SerializeField] private CharacterInputSystem _inputSystem;
    [SerializeField] private CinemachineInputAxisController _axisController;
    private void Update()
    {
        //_axisController.SetAxisValue(0, _inputSystem.playerCamera.x); 
        //_axisController.SetAxisValue(1, _inputSystem.playerCamera.y);
    }
}
