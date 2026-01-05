using System.Collections;
using UnityEngine;

public class BossStateCharge : BossBaseState
{
    private Coroutine _chargeCoroutine; private SkillDataSO _currentSkill;

    public BossStateCharge(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void OnEnter()
    {
        int skillIndex = _stateMachine.PendingSkillIndex;
        if (skillIndex == -1 || skillIndex >= _controller.Skills.Length)
        {
            // 异常保护：如果没有合法技能，直接切回 Idle
            if (_controller.IsServer) _controller.SetState(BossController.BossMotionState.Idle);
            return;
        }

        _currentSkill = _controller.Skills[skillIndex];

        // 播放蓄力动画
        string animName = !string.IsNullOrEmpty(_currentSkill.chargeAnimationName) ? _currentSkill.chargeAnimationName : "Idle";
        PlayAnimation(animName, 0.05f);

        if (_controller.IsServer)
        {
            if (_controller.Agent.isOnNavMesh)
                _controller.Agent.ResetPath();

            // 触发蓄力特效
            Vector3 spawnPos = _controller.transform.position;
            if (!_currentSkill.isSelfCentered && _controller.Target != null)
            {
                spawnPos = _controller.Target.transform.position;
            }
            _controller.TriggerChargeVisuals(spawnPos, _currentSkill.chargeDuration);

            // 开启计时
            _chargeCoroutine = _controller.StartCoroutine(EndChargeRoutine(_currentSkill.chargeDuration));
        }
    }

    public override void OnUpdate()
    {
        if (!_controller.IsServer) return;

        // 持续朝向目标
        _controller.RotateTowardsTarget();

        // 【新增逻辑】蓄力打断检测
        if (_currentSkill != null && _currentSkill.ifChargeInteruptable && _controller.Target != null)
        {
            float dist = Vector3.Distance(_controller.transform.position, _controller.Target.transform.position);
            // 如果玩家逃出了技能范围（加上 1.0f 的宽容度），取消蓄力
            if (dist > _currentSkill.castRadius + 1.0f)
            {
                Debug.Log("[Boss] Target escaped charge range, switching to Move.");
                _controller.SetState(BossController.BossMotionState.Chase);
                return;
            }
        }
    }

    public override void OnExit()
    {
        // 退出状态时，必须杀死协程
        if (_chargeCoroutine != null)
        {
            _controller.StopCoroutine(_chargeCoroutine);
            _chargeCoroutine = null;
        }
    }

    private IEnumerator EndChargeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        // 蓄力结束，切换到 Skill (Active) 状态
        _controller.SetState(BossController.BossMotionState.Skill);
    }
}
