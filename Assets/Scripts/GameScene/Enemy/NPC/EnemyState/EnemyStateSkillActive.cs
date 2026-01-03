using UnityEngine;

public class EnemyStateSkillActive : IEnemyState
{
    private EnemyPresentation _view;
    public EnemyStateSkillActive(EnemyPresentation view)
    {
        _view = view;
    }

    public void Enter()
    {
        // 从 Controller 获取配置的技能动画名
        _view.Animator.CrossFade(_view.SkillAnimationName, 0.1f);
    }

    public void Exit()
    {
        // 可以重置动画状态等
    }

    public void Update()
    {
        // 表现层可能不需要做太多 Update 逻辑，主要由 Controller 的 NetVar 驱动
    }
}