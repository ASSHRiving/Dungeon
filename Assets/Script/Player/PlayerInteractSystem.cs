using UnityEngine;

public class PlayerInteractSystem : MonoBehaviour
{
    [Header("檢測範圍")]
    [SerializeField] private Transform interactOrigin;
    [SerializeField] private float interactDistance;
    [SerializeField] private LayerMask interactMask;
    private CharacterInputSystem _inputSystem;
    private Collider[] colliders = new Collider[10];
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
        int count = Physics.OverlapSphereNonAlloc(interactOrigin.position, interactDistance, colliders, interactMask);

        IInteractable closestInteractable = null;
        float closestDistance = Mathf.Infinity;
        for (int i = 0; i < count; i++)
        {
            IInteractable interactable = colliders[i].GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                float distance = Vector3.Distance(interactOrigin.position, colliders[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }
        if(closestInteractable != null)
        {
            UIEvents.InteractableDetected(closestInteractable.interactableName);
            if (_inputSystem.playerInteract)
            {
                closestInteractable.Interact(transform);
                Debug.Log($"正在與 {closestInteractable} 互動，距離為 {closestDistance}");
            }
        }
        else
        {
            UIEvents.InteractableDetected(string.Empty);
        }
        System.Array.Clear(colliders, 0, colliders.Length);
        
    }

    private void OnDrawGizmosSelected()
    {
        if (interactOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(interactOrigin.position, interactDistance);
        }
    }
}
