using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 5f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) //按e
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                Debug.Log("Hit: " + hit.collider.name);

                // 找互動物件
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }
}