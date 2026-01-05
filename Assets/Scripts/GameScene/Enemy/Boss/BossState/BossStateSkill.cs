using System.Collections;
using UnityEngine;

public class BossStateSkill : BossBaseState
{
    private Coroutine _skillCoroutine;

    public BossStateSkill(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void OnEnter()
    {
        int skillIndex = _stateMachine.PendingSkillIndex;
        if (skillIndex == -1 || skillIndex >= _controller.Skills.Length)
        {
            if (_controller.IsServer) _controller.SetState(BossController.BossMotionState.Idle);
            return;
        }

        var skillData = _controller.Skills[skillIndex];

        // 播放 Active 动画
        string animName = !string.IsNullOrEmpty(skillData.activeAnimationName) ? skillData.activeAnimationName : "Attack";
        PlayAnimation(animName, 0.05f);

        if (_controller.IsServer)
        {
            // 执行技能逻辑 (Cast)
            Vector3 castPos = _controller.Target != null ? _controller.Target.transform.position : _controller.transform.position;
            if (skillData.isSelfCentered) castPos = _controller.transform.position;

            skillData.Cast(_controller.gameObject, _controller.Target != null ? _controller.Target.gameObject : null, castPos);

            // 开启计时
            _skillCoroutine = _controller.StartCoroutine(EndSkillRoutine(skillData.activeDuration));
        }
    }

    public override void OnUpdate()
    {
        if (!_controller.IsServer) return;
        // Active 阶段通常锁定旋转，或者根据需求 _controller.RotateTowardsTarget();
    }

    public override void OnExit()
    {
        if (_skillCoroutine != null)
        {
            _controller.StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }
    }

    private IEnumerator EndSkillRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        // Active 结束，切换到 Recovery 状态
        _controller.SetState(BossController.BossMotionState.Recovery);
    }
}
