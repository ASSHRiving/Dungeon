using UnityEngine;
using System;

public class AnimationEventHelper : MonoBehaviour
{
    public Action OnAnimationFinish;
    public Action OnFootstepEvent;
    public void OnAnimationFinishEvent()
    {
        OnAnimationFinish.Invoke();
    }
    public void PlayFootstepSound()
    {
        OnFootstepEvent?.Invoke();
    }
}
