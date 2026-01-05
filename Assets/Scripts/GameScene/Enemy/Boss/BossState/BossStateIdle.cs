using UnityEngine;

public class BossStateIdle : BossBaseState
{
    public BossStateIdle(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void OnEnter()
    {
        PlayAnimation("Idle");

        if (_controller.IsServer)
        {
            if (_controller.Agent.isOnNavMesh)
                _controller.Agent.ResetPath();
        }
    }

    public override void OnUpdate()
    {
        if (!_controller.IsServer) return;

        if (_controller.Target != null)
        {
            float dist = Vector3.Distance(_controller.transform.position, _controller.Target.transform.position);

            // 检查是否进入攻击范围
            if (dist <= _controller.BasicAttackRange)
            {
                // 尝试攻击，如果 CD 好了就会切换到 Charge 状态
                if (!_controller.TrySelectAndStartAttack())
                {
                    // CD 没好，或者没技能可用，保持 Idle 或者做点别的（比如侧向移动）
                }
            }
            else
            {
                // 距离太远，进入追逐
                _controller.SetState(BossController.BossMotionState.Chase);
            }
        }
        else
        {
            DetectPlayer();
        }
    }

    private void DetectPlayer()
    {
        var hits = Physics.OverlapSphere(_controller.transform.position, _controller.ChaseRange, _controller.ChaseLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PlayerDataContainer>(out var playerData) && !playerData.IsDead)
            {
                _controller.SetTarget(hit.GetComponent<Unity.Netcode.NetworkObject>());
                return;
            }
        }
    }
}
