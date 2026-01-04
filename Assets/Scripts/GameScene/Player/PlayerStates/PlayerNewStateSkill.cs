using UnityEngine;

public class PlayerNewStateSkill : PlayerBaseState
{
    private bool _stateFinished;
    private Coroutine _timerCoroutine;
    public PlayerNewStateSkill(PlayerMainController controller) : base(controller) { }

    public override void OnEnter()
    {
        _stateFinished = false;

        if (_controller.IsOwner)
        {
            // 【修改】不再检测 Input，而是直接读取状态机缓存的 Index
            int skillIndex = _controller.StateMachine.PendingSkillIndex;
            if (skillIndex != -1)
            {
                // 1. 获取并播放动画
                string animName = _controller.Combat.GetSkillAnimationName(skillIndex);
                _controller.Animator.CrossFade(animName, 0.05f);

                // 2. 获取技能数据以计算持续时间
                var skillData = _controller.Combat.GetSkillDataByIndex(skillIndex);
                float duration = skillData.activeDuration;
                // 5. 启动结束协程
                _timerCoroutine = _controller.StartCoroutine(EndSkillRoutine(duration));
            }
            else
            {
                // 如果没有检测到按键（异常情况），直接退出
                _stateFinished = true;
            }
        }
    }

    // 下面的方法需要更新名称为：EndStateRoutine
    private System.Collections.IEnumerator EndSkillRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        _stateFinished = true;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (_controller.IsOwner)
        {
            StateLogic();
        }
    }
    public override void OnExit()
    {
        // 杀掉协程
        if (_timerCoroutine != null)
        {
            _controller.StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }

    protected override void StateLogic()
    {
        if (ChangeStateToCharge()) return;
    }

    private bool ChangeStateToCharge()
    {
        if (_stateFinished)
        {
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateRecovery);
            return true;
        }
        return false;
    }
}