using UnityEngine;

public class BossStateMove : BossBaseState
{
    private float _repathTimer = 0f; private float _repathInterval = 0.2f;

    public BossStateMove(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void OnEnter()
    {
        PlayAnimation("Walk");

        if (_controller.IsServer)
        {
            if (_controller.Agent.isOnNavMesh)
                _controller.Agent.isStopped = false;
        }
    }

    public override void OnUpdate()
    {
        if (!_controller.IsServer) return;

        if (_controller.Target == null)
        {
            _controller.SetState(BossController.BossMotionState.Idle);
            return;
        }

        float dist = Vector3.Distance(_controller.transform.position, _controller.Target.transform.position);

        // 如果距离过远，放弃追逐
        if (dist > _controller.ChaseRange * 1.5f)
        {
            _controller.SetTarget(null);
            _controller.SetState(BossController.BossMotionState.Idle);
            return;
        }

        // 如果距离足够近，尝试进入攻击
        if (dist <= _controller.BasicAttackRange)
        {
            if (_controller.TrySelectAndStartAttack())
                return; // 成功切换到 Charge
        }

        // 执行寻路
        _repathTimer += Time.deltaTime;
        if (_repathTimer > _repathInterval)
        {
            _repathTimer = 0f;
            if (_controller.Agent.isOnNavMesh)
                _controller.Agent.SetDestination(_controller.Target.transform.position);
        }
    }
}
