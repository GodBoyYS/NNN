using UnityEngine;

public class PlayerNewStateCharge : PlayerBaseState
{
    private bool stateFinished = false;
    SkillDataSO _currentSkill;
    // 关键：保存协程的引用
    private Coroutine _timerCoroutine;
    public PlayerNewStateCharge(PlayerMainController controller) : base(controller)
    {
    }
    public override void OnEnter()
    {
        stateFinished = false;

        if (_controller.IsOwner)
        {
            // 立即读取输入（同 Move 状态的修复逻辑）
            int skillIndex = _controller.StateMachine.PendingSkillIndex;
            Vector3 aimPos = _controller.StateMachine.PendingAimPosition; // 获取存储的位置

            if (skillIndex != -1)
            {
                // 【新增】把这个 index 存入状态机，供后续状态使用
                _controller.StateMachine.PendingSkillIndex = skillIndex;
                // 1. 获取并播放动画
                //string animName = _controller.Combat.GetSkillAnimationName(skillIndex);
                string animName = _controller.Combat.GetSkillChargeAnimation(skillIndex);
                _controller.Animator.CrossFade(animName, 0.05f);

                

                // 3. 获取技能数据以计算持续时间
                _currentSkill = _controller.Combat.GetSkillDataByIndex(skillIndex);
                float duration = _currentSkill.chargeDuration;
                // 5. 启动结束协程
                _timerCoroutine = _controller.StartCoroutine(EndStateRoutine(duration));
            }
            else
            {
                // 如果没有检测到按键（异常情况），直接退出
                stateFinished = true;
            }
        }
    }

    // 下面的方法需要更新名称为：EndStateRoutine
    private System.Collections.IEnumerator EndStateRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        stateFinished = true;
        _controller.StateMachine.ChangeState(_controller.StateMachine.StateSkill);
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
        if (_timerCoroutine != null)
        {
            _controller.StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }

    protected override void StateLogic()
    {
        //if (ChangeStateToSkill()) return;
        if (ChangeStateToMove()) return;
    }

    private bool ChangeStateToSkill()
    {
        if (stateFinished)
        {
            
            return true;
        }
        return false;
    }
    private bool ChangeStateToMove()
    {
        if (_currentInput.InteractDown && _currentInput.HasMouseTarget && _currentSkill.ifChargeInteruptable)
        {
            Debug.Log("在Charge状态检测到移动输入，且有点击的目标，准备切换到 移动状态");
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateMove);
            return true;
        }
        return false;
    }
}
