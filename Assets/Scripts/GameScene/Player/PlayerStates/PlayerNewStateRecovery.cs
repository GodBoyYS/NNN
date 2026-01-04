using UnityEngine;

public class PlayerNewStateRecovery : PlayerBaseState
{
    private bool _stateFinished = false;
    private SkillDataSO _currentSkill;
    private Coroutine _timerCoroutine;
    public PlayerNewStateRecovery(PlayerMainController controller) : base(controller)
    {
    }
    public override void OnEnter()
    {
        _stateFinished = false;
        // 【修改】直接读取 Index
        int skillIndex = _controller.StateMachine.PendingSkillIndex;
        if (skillIndex != -1)
            {
                // 1. 获取并播放动画
                string animName = _controller.Combat.GetSkillRecoveryAnimation(skillIndex);
                //string animName = _controller.Combat.GetSkillAnimationName(skillIndex);
                _controller.Animator.CrossFade(animName, 0.05f);

                // 2. 获取技能数据以计算持续时间
                _currentSkill = _controller.Combat.GetSkillDataByIndex(skillIndex);
                float duration = _currentSkill.activeDuration;

                // 3. 计算瞄准位置
                //Vector3 aimPos = input.HasMouseTarget ? input.MouseWorldPos : _controller.transform.position + _controller.transform.forward;

                //// 4. 发送网络请求
                //_controller.Combat.RequestCastSkill(skillIndex, aimPos);

                // 5. 启动结束协程
                _controller.StartCoroutine(EndStateRoutine(duration));
            }
            else
            {
                // 如果没有检测到按键（异常情况），直接退出
                _stateFinished = true;
            }
        }
  

    // 下面的方法需要更新名称为：EndStateRoutine
    private System.Collections.IEnumerator EndStateRoutine(float time)
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
        if (ChangeStateToIdle()) return;
        if (ChangeStateToMove()) return;
    }

    private bool ChangeStateToIdle()
    {
        if (_stateFinished)
        {
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateIdle);
            return true;
        }
        return false;
    }
    private bool ChangeStateToMove()
    {
        if (_currentInput.InteractDown && _currentInput.HasMouseTarget && _currentSkill.ifRecoveryInteruptable)
        {
            Debug.Log("在Recovery状态检测到移动输入，且有点击的目标，准备切换到 移动状态");
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateMove);
            return true;
        }
        return false;
    }
}
