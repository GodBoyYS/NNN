using System.Collections;
using UnityEngine;

public class BossStateRecovery : BossBaseState
{
    private Coroutine _recoveryCoroutine;

    public BossStateRecovery(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void OnEnter()
    {
        int skillIndex = _stateMachine.PendingSkillIndex;
        if (skillIndex == -1 || skillIndex >= _controller.Skills.Length)
        {
            if (_controller.IsServer) _controller.SetState(BossController.BossMotionState.Idle);
            return;
        }

        var skillData = _controller.Skills[skillIndex];

        // 播放 Recovery 动画
        string animName = !string.IsNullOrEmpty(skillData.recoveryAnimationName) ? skillData.recoveryAnimationName : "Idle";
        PlayAnimation(animName, 0.05f);

        if (_controller.IsServer)
        {
            // 开启计时
            _recoveryCoroutine = _controller.StartCoroutine(EndRecoveryRoutine(skillData.recoveryDuration));
        }
    }

    public override void OnExit()
    {
        if (_recoveryCoroutine != null)
        {
            _controller.StopCoroutine(_recoveryCoroutine);
            _recoveryCoroutine = null;
        }
    }

    private IEnumerator EndRecoveryRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        // 后摇结束，清理上下文，切回 Idle
        _stateMachine.PendingSkillIndex = -1;
        _controller.SetState(BossController.BossMotionState.Idle);
    }
}
