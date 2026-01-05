using UnityEngine;

public abstract class BossBaseState
{
    protected BossController _controller; protected BossStateMachine _stateMachine;

    public BossBaseState(BossController controller, BossStateMachine stateMachine)
    {
        _controller = controller;
        _stateMachine = stateMachine;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }

    // 供子类使用的辅助方法：安全的切换动画
    protected void PlayAnimation(string animName, float fadeTime = 0.1f)
    {
        if (_controller.Animator != null && !string.IsNullOrEmpty(animName))
        {
            _controller.Animator.Play(animName);
        }
    }
}
