using UnityEngine;

public class BossStateSkill : IEnemyState
{
    private BossPresentation _view;
    public BossStateSkill(BossPresentation view)
    {
        _view = view;
    }

    public void Enter()
    {
        _view.Animator.Play(_view.SkillAnimationName);
    }

    public void Exit()
    {
    }

    // Update is called once per frame
    public void Update()
    {
    }
}
