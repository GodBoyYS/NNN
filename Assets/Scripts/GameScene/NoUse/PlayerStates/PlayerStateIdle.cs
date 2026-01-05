using UnityEngine;
public class PlayerStateIdle : IPlayerState
{
    private RaycastHit groundHit;
    private PlayerPresentation _view;
    public PlayerStateIdle(PlayerPresentation view) => _view = view;
    public void Enter()
    {
        //Debug.Log("进入 Idle（Presentation）");
        _view.Animator.Play("Idle_Battle_SwordAndShield");
        // 这里放：Animator.SetBool("Moving", false) 等
    }
    public void Exit() { }
    public void Update()
    {
        // 纯表现：脚步声停、待机呼吸等
    }
}

//using UnityEngine;
//public class PlayerStateIdle : IPlayerState
//{
//    private RaycastHit groundHit;
//    private PlayerController _player;
//    public PlayerStateIdle(PlayerController player)
//    {
//        _player = player;
//    }
//    public void Enter()
//    {
//        if(_player.IsServer)
//        {
//            Debug.Log("服务器实例进入idle");
//        }
//        else if(_player.IsClient)
//        {
//            Debug.Log("客户端实例进入idle");
//        }
//        Debug.Log("进入idle状态");
//    }
//    public void Exit(){}
//    public void Update()
//    {
//        if (!_player.IsOwner) return;
//        if(Input.GetMouseButtonDown(01))
//        {
//            Ray ray = _player.MainCamera.ScreenPointToRay(Input.mousePosition);
//            // 提取最大距离常量，方便统一修改和绘制
//            float maxDistance = 1000f;
//            if (Physics.Raycast(ray, out RaycastHit interactHit, maxDistance, _player.InteractLayer))
//            {
//                return;
//            }
//            else if (Physics.Raycast(ray, out groundHit, maxDistance, _player.GroundLayer))
//            {
//                // -> ServerRpc -> Server 写 BossMotionState = Moving
//                _player.RequestMove(groundHit.point);
//                return;
//            }
//        }
//    }
//}
/*
 * using UnityEngine;

public class PlayerStateIdle : IPlayerState
{
    private RaycastHit groundHit;
    private PlayerController _player;
    public PlayerStateIdle(PlayerController player)
    {
        _player = player;
    }
    public void Enter()
    {
        if(_player.IsServer)
        {
            Debug.Log("服务器实例进入idle");
        }
        else if(_player.IsClient)
        {
            Debug.Log("客户端实例进入idle");
        }
        Debug.Log("进入idle状态");
    }

    public void Exit()
    {
    }

    public void Update()
    {
        if (!_player.IsOwner) return;
        if(Input.GetMouseButton(01))
        {
            Ray ray = _player.MainCamera.ScreenPointToRay(Input.mousePosition);
            // 提取最大距离常量，方便统一修改和绘制
            float maxDistance = 1000f;
            if (Physics.Raycast(ray, out RaycastHit interactHit, maxDistance, _player.InteractLayer))
            {
                Debug.Log($">>> 点击到交互物体: {interactHit.collider.name} <<<");
                return;
            }
            else if (Physics.Raycast(ray, out groundHit, maxDistance, _player.GroundLayer))
            {
                // -> ServerRpc -> Server 写 BossMotionState = Moving
                _player.RequestMove(groundHit.point);
                return;
            }
        }
        //if (ChangeStateToMove())
        //{
        //    if (_player.IsServer)
        //    {
        //        _player.ChangeState(new PlayerStateMove(_player, groundHit.point));
        //        _player.SwithToMoveClientRpc(groundHit.point);
        //        return;
        //    }
        //}
    }

    //private bool ChangeStateToMove()
    //{
    //    if (Input.GetMouseButtonDown(1))
    //    {
    //        Ray ray = _player.MainCamera.ScreenPointToRay(Input.mousePosition);

    //        // 1. 正常的交互检测
    //        if (Physics.Raycast(ray, out RaycastHit interactHit, 1000f, _player.InteractLayer))
    //        {
    //            Debug.Log(">>> 点击到交互物体 <<<");
    //            return false;
    //        }
    //        // 2. 地面检测
    //        if (Physics.Raycast(ray, out RaycastHit groundHit, 1000f, _player.GroundLayer))
    //        {
    //            _player.ChangeState(new PlayerStateMove(_player, groundHit.point));
    //            return true;
    //        }
    //    }
    //    return false;
    //}
    private bool ChangeStateToMove()
    {
        // 鼠标右键点击 (0是左键, 1是右键, 2是中键)
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = _player.MainCamera.ScreenPointToRay(Input.mousePosition);
            // 提取最大距离常量，方便统一修改和绘制
            float maxDistance = 1000f;
            if (Physics.Raycast(ray, out RaycastHit interactHit, maxDistance, _player.InteractLayer))
            {
                Debug.Log($">>> 点击到交互物体: {interactHit.collider.name} <<<");
                return false;
            }
            else if (Physics.Raycast(ray, out groundHit, maxDistance, _player.GroundLayer))
            {
                //_player.ChangeState(new PlayerStateMove(_player, groundHit.point));
                return true;
            }
        }
        return false;
    }
}

 */
