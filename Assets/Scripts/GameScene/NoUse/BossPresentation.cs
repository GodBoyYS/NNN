using UnityEngine;
using UnityEngine.AI;

// [DEPRECATED] 该类已弃用，逻辑已迁移至 BossController 和 BossStates
[System.Obsolete("Logic moved to BossController. Please reference Animator/Agent directly in Controller.")] 
public class BossPresentation : MonoBehaviour { 
    [Header("Component References")] 
    public Animator Animator; public NavMeshAgent Agent;

private void Awake()
{
    if (Animator == null) Animator = GetComponent<Animator>();
    if (Agent == null) Agent = GetComponent<NavMeshAgent>();
}

public void PlayAnimation(string animName, float transitionDuration = 0.1f)
{
    if (Animator != null && !string.IsNullOrEmpty(animName))
    {
        Animator.CrossFade(animName, transitionDuration);
    }
}
}
