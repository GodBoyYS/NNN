using UnityEngine;

public class PlayerStateSkill : IPlayerState
{
    private PlayerPresentation _view;
    public PlayerStateSkill(PlayerPresentation view)
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

    public void Update()
    {
    }
}
