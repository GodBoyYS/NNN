using UnityEngine;

public class BossStateDie : BossBaseState
{
    public BossStateDie(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void OnEnter()
    {
        PlayAnimation("Die");

        if (_controller.IsServer)
        {
            if (_controller.Agent.isOnNavMesh)
                _controller.Agent.isStopped = true;

            _controller.GetComponent<Collider>().enabled = false;
        }
    }
}
