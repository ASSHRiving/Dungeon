using UnityEngine;

public static class MyTools
{
    public static bool CheckAnimationTag(this Animator animator, string tagName, int animationIndex = 0)
    {
        if (animator.IsInTransition(animationIndex))
        {
            return animator.GetNextAnimatorStateInfo(animationIndex).IsTag(tagName);
        }
        return animator.GetCurrentAnimatorStateInfo(animationIndex).IsTag(tagName);
    }

    public static bool CheckAnimationName(this Animator animator, string animationName, int animationIndex = 0)
    {
        return animator.GetCurrentAnimatorStateInfo(animationIndex).IsName(animationName);
    }

    public static Quaternion LockOnTarget(this Transform transform, Transform target,Transform self,float lerpTime)
    {
        if (target == null) return self.rotation;

        Vector3 targetDirection = target.position - self.position;
        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude < 0.0001f)
            return self.rotation;

        Quaternion newRotation = Quaternion.LookRotation(targetDirection.normalized);
        
        float t = 1f - Mathf.Exp(-Mathf.Max(0f, lerpTime) * Time.deltaTime);
        return Quaternion.Slerp(self.rotation, newRotation, t);
    }
}
