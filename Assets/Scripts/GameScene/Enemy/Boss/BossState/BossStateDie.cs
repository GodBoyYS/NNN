using UnityEngine;

public class BossStateDie : BossBaseState
{
    public BossStateDie(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void Enter()
    {
        // [±íÏÖ]
        _view.PlayAnimation("Die");

        // [Âß¼­]
        if (_controller.IsServer)
        {
            if (_controller.Agent.isOnNavMesh) _controller.Agent.isStopped = true;
            _controller.GetComponent<Collider>().enabled = false;
        }
    }
}

//using UnityEngine;

//public class BossStateDie : IEnemyState
//{
//    private BossPresentation _view;
//    public BossStateDie(BossPresentation view)
//    {
//        _view = view;
//    }

//    public void Enter()
//    {

//    }

//    public void Exit()
//    {

//    }

//    public void Update()
//    {

//    }
//}
