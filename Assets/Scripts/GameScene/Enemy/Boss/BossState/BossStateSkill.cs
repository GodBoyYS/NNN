using UnityEngine;

public class BossStateSkill : BossBaseState
{
    private float _timer;
    private float _recoveryTime;
    private bool _hasCast;

    public BossStateSkill(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void Enter()
    {
        var skillData = _controller.CurrentSkillData;
        //string animName = skillData != null ? skillData.animationName : "Attack";
        string animName = skillData != null ? skillData.skillActiveAnimationName : "Attack";

        // [表现]
        _view.PlayAnimation(animName);

        // [逻辑 - Server]
        if (_controller.IsServer)
        {
            _timer = 0f;
            _hasCast = false;
            // 硬编码的后摇时间，也可以配置在SkillData里
            _recoveryTime = (_controller.CurrentSkillIdx == 0) ? 1.0f : 2.0f;
            _controller.RotateTowardsTarget();
        }
    }

    public override void Update()
    {
        if (!_controller.IsServer) return;

        _timer += Time.deltaTime;

        // 这里的逻辑模拟了原本Coroutine里的 Cast 逻辑
        if (!_hasCast)
        {
            _hasCast = true;
            var skillData = _controller.CurrentSkillData;
            if (skillData != null)
            {
                // 具体的伤害/生成判定
                Vector3 castPos = _controller.Target != null ? _controller.Target.transform.position : _controller.transform.position;
                if (skillData.isSelfCentered) castPos = _controller.transform.position;

                skillData.Cast(_controller.gameObject,
                    _controller.Target != null ? _controller.Target.gameObject : null,
                    castPos);
            }
        }

        if (_timer >= _recoveryTime)
        {
            _controller.SetState(BossController.BossMotionState.Idle);
        }
    }
}

//using UnityEngine;

//public class BossStateSkill : IEnemyState
//{
//    private BossPresentation _view;
//    public BossStateSkill(BossPresentation view)
//    {
//        _view = view;
//    }

//    public void Enter()
//    {
//        _view.Animator.Play(_view.SkillAnimationName);
//    }

//    public void Exit()
//    {
//    }

//    // Update is called once per frame
//    public void Update()
//    {
//    }
//}
