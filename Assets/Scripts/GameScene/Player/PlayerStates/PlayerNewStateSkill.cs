using UnityEngine;

public class PlayerNewStateSkill : PlayerBaseState
{
    private bool _skillFinished;

    public PlayerNewStateSkill(PlayerMainController controller) : base(controller) { }

    public override void OnEnter()
    {
        _skillFinished = false;

        if (_controller.IsOwner)
        {
            // 立即读取输入（同 Move 状态的修复逻辑）
            var input = _controller.Input;
            int skillIndex = -1;

            if (input.AttackDown) skillIndex = 0;
            else if (input.SkillQDown) skillIndex = 1;
            else if (input.SkillWDown) skillIndex = 2;
            else if (input.SkillEDown) skillIndex = 3;

            if (skillIndex != -1)
            {
                // 1. 获取并播放动画
                string animName = _controller.Combat.GetSkillAnimationName(skillIndex);
                _controller.Animator.Play(animName);

                // 2. 获取技能数据以计算持续时间
                var skillData = _controller.Combat.GetSkillDataByIndex(skillIndex);
                float duration = skillData != null ? 0.8f : 0.5f; // 建议从 SkillDataSO 中读取 AnimationDuration

                // 3. 计算瞄准位置
                Vector3 aimPos = input.HasMouseTarget ? input.MouseWorldPos : _controller.transform.position + _controller.transform.forward;

                // 4. 发送网络请求
                _controller.Combat.RequestCastSkill(skillIndex, aimPos);

                // 5. 启动结束协程
                _controller.StartCoroutine(EndSkillRoutine(duration));
            }
            else
            {
                // 如果没有检测到按键（异常情况），直接退出
                _skillFinished = true;
            }
        }
    }

    private System.Collections.IEnumerator EndSkillRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        _skillFinished = true;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (_controller.IsOwner)
        {
            StateLogic();
        }
    }

    protected override void StateLogic()
    {
        if (ChangeStateToIdle()) return;
    }

    private bool ChangeStateToIdle()
    {
        if (_skillFinished)
        {
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateIdle);
            return true;
        }
        return false;
    }
}