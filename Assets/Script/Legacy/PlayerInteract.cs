using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 5f;
    public Transform interactOrigin; // 建議設在角色胸口或頭
    public LayerMask interactMask;
    private CharacterInputSystem _inputSystem;

    private void Awake()
    {
        _inputSystem = GetComponent<CharacterInputSystem>();
    }
    private void Update()
    {
        OnInteract();
    }
    private void OnInteract()
    {
        if (_inputSystem.playerInteract)
        {
            Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit camHit;

            Vector3 targetPoint;

            if (Physics.Raycast(cameraRay, out camHit, 100f, interactMask))
            {
                targetPoint = camHit.point;
            }
            else
            {
                targetPoint = cameraRay.GetPoint(100f);
            }

            // ② 從角色發射 Ray（比較合理）
            Vector3 dir = (targetPoint - interactOrigin.position).normalized;

            Ray playerRay = new Ray(interactOrigin.position, dir);
            RaycastHit hit;

            if (Physics.Raycast(playerRay, out hit, interactDistance, interactMask))
            {
                Debug.Log("Hit: " + hit.collider.name);

                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    interactable.Interact(transform);
                }
            }
        }
    }
}