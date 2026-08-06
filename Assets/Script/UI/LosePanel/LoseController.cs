using UnityEngine;

public class LoseController : MonoBehaviour
{
    [SerializeField] private GameObject losePanel;
    private void OnEnable()
    {
        UIEvents.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        UIEvents.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        losePanel.SetActive(true);
    }
    public void QuitGame()
    {
        GameManager.Instance.QuitGame();
    }
   
}
