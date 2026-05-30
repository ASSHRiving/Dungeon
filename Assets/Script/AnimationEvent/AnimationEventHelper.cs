using UnityEngine;
using System;

public class AnimationEventHelper : MonoBehaviour
{
    public Action OnAnimationFinish;
    public void OnAnimationFinishEvent()
    {
        OnAnimationFinish.Invoke();
    }
}
