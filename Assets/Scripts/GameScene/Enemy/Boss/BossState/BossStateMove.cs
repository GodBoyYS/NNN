using UnityEngine;

public class BossStateMove : BossBaseState
{
    private float _repathTimer = 0f;
    private float _repathInterval = 0.2f;

    public BossStateMove(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void Enter()
    {
        // [表现]
        _view.PlayAnimation("Walk");

        // [逻辑 - Server]
        if (_controller.IsServer)
        {
            if (_controller.Agent.isOnNavMesh) _controller.Agent.isStopped = false;
        }
    }

    public override void Update()
    {
        if (!_controller.IsServer) return;

        // [逻辑 - Server]
        if (_controller.Target == null)
        {
            _controller.SetState(BossController.BossMotionState.Idle);
            return;
        }

        float dist = Vector3.Distance(_controller.transform.position, _controller.Target.transform.position);

        // 超出追击范围
        if (dist > _controller.ChaseRange * 1.5f)
        {
            _controller.SetState(BossController.BossMotionState.Idle);
            return;
        }

        // 进入攻击范围
        if (dist <= _controller.BasicAttackRange)
        {
            if (_controller.TrySelectAndStartAttack()) return;
        }

        // 寻路逻辑
        _repathTimer += Time.deltaTime;
        if (_repathTimer > _repathInterval)
        {
            _repathTimer = 0f;
            if (_controller.Agent.isOnNavMesh)
                _controller.Agent.SetDestination(_controller.Target.transform.position);
        }
    }
}

//using UnityEngine;
//using UnityEngine.AI;

//public class BossStateMove : IEnemyState
//{
//    private BossPresentation _view;
//    private NavMeshAgent _agent;

//    public BossStateMove(BossPresentation view)
//    {
//        _view = view;
//        _agent = view.GetComponent<NavMeshAgent>();
//    }

//    public void Enter()
//    {
//        _view.Animator.Play("Walk");
//    }

//    public void Exit() { }

//    public void Update()
//    {
//        // 简单的防滑步检测
//        if (_agent != null)
//        {
//            // 如果速度很慢，可能是被挡住或者停止了，临时播 Idle
//            if (_agent.velocity.sqrMagnitude < 0.1f)
//            {
//                // _view.Animator.Play("Idle"); 
//            }
//            else
//            {
//                // 确保动画状态机在 Walk
//                // _view.Animator.Play("Walk");
//            }
//        }
//    }
//}