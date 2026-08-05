using UnityEngine;

public class FinalController : MonoBehaviour
{
    public void Exit()
    {
        LoadingController.Instance.LoadLevel("MainMenu");
        GameAssets.Instance.PlayMenuMusic();
    }
}
