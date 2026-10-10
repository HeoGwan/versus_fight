using UnityEngine;

public static class AnimatorExtensions
{
    /// <summary>
    /// Animator에 특정 이름의 파라미터가 존재하는지 확인합니다.
    /// </summary>
    public static bool HasParameter(this Animator animator, string paramName)
    {
        if (string.IsNullOrEmpty(paramName) || animator == null) 
            return false;

        // Animator의 모든 파라미터를 조사합니다.
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }

        return false;
    }
}