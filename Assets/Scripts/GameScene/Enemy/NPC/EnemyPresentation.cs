using UnityEditor.VersionControl;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyPresentation : MonoBehaviour
{
    private EnemyController _controller;
    private IEnemyState _currentState;

    private Animator _animator;

    public Animator Animator => _animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _controller = GetComponent<EnemyController>();
        _controller.Motion.OnValueChanged += OnMtionStateChanged;
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnMtionStateChanged(EnemyController.NPCMotionState oldState, EnemyController.NPCMotionState newState)
    {
        RefreshState();
    }
    private void RefreshState()
    {
        if(_controller == null) return;
        // life state
        switch (_controller.MotionStateVar)
        {
            case EnemyController.NPCMotionState.Idle:
                if (_currentState is EnemyStateIdle) return;
                ChangeState(new EnemyStateIdle(this));
                break;
            case EnemyController.NPCMotionState.Chase:
                if(_currentState is EnemyStateMove) return;
                ChangeState(new EnemyStateMove(this));
                break;
            case EnemyController.NPCMotionState.Attack:
                if(_currentState is EnemyStateAttack) return;
                ChangeState(new EnemyStateAttack(this));
                break;
            default:
                break;
        }
    }
    public void ChangeState(IEnemyState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }
}
