using UnityEngine;

public class PlayerNewStateSkill : PlayerBaseState
{
    private bool _stateFinished;
    private Coroutine _timerCoroutine;
    private SkillDataSO _currentSkillData; // 缓存当前技能数据
    public PlayerNewStateSkill(PlayerMainController controller) : base(controller) { }

    public override void OnEnter()
    {
        _stateFinished = false;

        if (_controller.IsOwner)
        {
            // 【修改】不再检测 Input，而是直接读取状态机缓存的 Index
            int skillIndex = _controller.StateMachine.PendingSkillIndex;
            Vector3 aimPos = _controller.StateMachine.PendingAimPosition; // 获取存储的位置

            if (skillIndex != -1)
            {
                // 获取技能数据
                _currentSkillData = _controller.Combat.GetSkillDataByIndex(skillIndex);
                // 1. 获取并播放动画
                string animName = _controller.Combat.GetSkillAnimationName(skillIndex);
                _controller.Animator.CrossFade(animName, 0.05f);
                // 2. [关键修复] 发送RPC请求给服务器执行技能逻辑
                // 这里调用 Combat 组件的方法，Combat 组件会负责调用 ServerRpc
                _controller.Combat.RequestCastSkill(skillIndex, aimPos);
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
            // 【新增】处理移动逻辑
            HandleMovementLogic();
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
        _currentSkillData = null; // 清理引用
    }

    protected override void StateLogic()
    {
        if (ChangeStateToRecovery()) return;
    }

    private bool ChangeStateToRecovery()
    {
        if (_stateFinished)
        {
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateRecovery);
            return true;
        }
        return false;
    }
    private void HandleMovementLogic()
    {
        // 如果技能配置为空，或者不允许移动，直接返回
        if (_currentSkillData == null || !_currentSkillData.canMoveDuringActive) return;

        // 复用通用的移动输入检测逻辑
        var input = _controller.Input;

        // 如果有移动输入
        if (input.MoveInput != Vector2.zero)
        {
            // 这里我们需要一种方式把 Input 转换为世界坐标
            // 简单起见，我们假设是基于摄像机的方向（类似于 Move State 的逻辑）
            // 注意：这里为了简化代码，直接调用了 Movement 的请求，
            // 实际项目中你可能需要把 CalculateMovementVector 逻辑从 PlayerNewStateMove 中提取到公共工具类

            // 下面是一段简化的移动计算逻辑，最好是从 PlayerNewStateMove 中提取复用
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * input.MoveInput.y + camRight * input.MoveInput.x).normalized;
            Vector3 targetPos = _controller.transform.position + moveDir * 2.0f; // 往那个方向走

            _controller.Movement.RequestMove(targetPos);
        }
        else if (input.StopDown)
        {
            _controller.Movement.RequestStop();
        }

        // 这里不需要处理“点击地板移动”，因为通常移动施法都是配合 WASD 操作
        // 如果是点击移动（Click to Move），逻辑也是类似的，检测 input.HasMouseTarget 并 RequestMove
        if (input.InteractDown && input.HasMouseTarget)
        {
            _controller.Movement.RequestMove(input.MouseWorldPos);
        }
    }
}
