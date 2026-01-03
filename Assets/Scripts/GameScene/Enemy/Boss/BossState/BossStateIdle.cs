using UnityEngine;

public class BossStateIdle : BossBaseState
{
    public BossStateIdle(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void Enter()
    {
        // [±íÏÖ]
        _view.PlayAnimation("Idle");

        // [Âß¼­ - Server]
        if (_controller.IsServer)
        {
            if (_controller.Agent.isOnNavMesh) _controller.Agent.ResetPath();
        }
    }

    public override void Update()
    {
        if (!_controller.IsServer) return;

        // [Âß¼­ - Server] ¼ì²âÍæ¼Ò
        if (_controller.Target != null)
        {
            float dist = Vector3.Distance(_controller.transform.position, _controller.Target.transform.position);
            if (dist <= _controller.BasicAttackRange)
            {
                // ³¢ÊÔ¹¥»÷
                if (!_controller.TrySelectAndStartAttack())
                {
                    // CDÃ»ºÃ£¬¼ÌÐø·¢´ô»òµ÷ÕûÎ»ÖÃ
                }
            }
            else
            {
                // ¾àÀëÔ¶£¬×·»÷
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

//using UnityEngine;

//public class BossStateIdle : IEnemyState
//{
//    private BossPresentation _view;
//    public BossStateIdle(BossPresentation view)
//    {
//        _view = view;
//    }
//    public void Enter()
//    {
//        _view.Animator.Play("Idle");
//    }

//    public void Exit()
//    {

//    }

//    public void Update()
//    {

//    }
//}
