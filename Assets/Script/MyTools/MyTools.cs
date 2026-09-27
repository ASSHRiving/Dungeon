using UnityEngine;

public static class MyTools
{
    public static bool CheckAnimationTag(this Animator animator, string tagName, int animationIndex = 0)
    {
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

        Quaternion newRotation = Quaternion.LookRotation(targetDirection.normalized);
        
        return  Quaternion.Lerp(self.rotation,newRotation,lerpTime * Time.deltaTime);
    }
}
