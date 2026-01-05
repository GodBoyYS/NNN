using System;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
public class PlayerStateMove : IPlayerState
{
    private PlayerPresentation _view;
    public PlayerStateMove(PlayerPresentation view) => _view = view;
    public void Enter()
    {
        //Debug.Log("进入 Move（Presentation）");
        //_view.Animator.SetBool("Moving", true);
        _view.Animator.Play("MoveFWD_Normal_InPlace_SwordAndShield");
        // Animator.SetBool("Moving", true)
    }
    public void Exit() 
    {
        //_view.Animator.SetBool("Moving", false);
    }
    public void Update()
    {
        // 纯表现：脚步声、移动动画
    }
}

//using System;
//using System.Globalization;
//using Unity.Netcode;
//using UnityEngine;
//public class PlayerStateMove : IPlayerState
//{
//    private PlayerController _player;
//    public PlayerStateMove(PlayerController player)
//    {
//        _player = player;
//    }
//    public void Enter()
//    {
//        Debug.Log("进入move状态");
//    }
//    public void Exit(){}
//    public void Update()
//    {
//        if (!_player.IsOwner) return;
//        if (Input.GetKeyDown(KeyCode.S))
//        {
//            _player.RequestStop(); // -> ServerStopMove -> BossMotionState=Idle
//            return;
//        }
//        if (Input.GetMouseButtonDown(1))
//        {
//            Ray ray = _player.MainCamera.ScreenPointToRay(Input.mousePosition);
//            if(Physics.Raycast(ray, out RaycastHit interactHit, _player.InteractLayer))
//            {
//                _player.CheckInteract(interactHit);
//                return;
//            }
//            if (Physics.Raycast(ray, out RaycastHit groundHit, 1000f, _player.GroundLayer))
//            {
//                _player.RequestMove(groundHit.point);
//                return;
//            }
//        }
//    }
//}

/*
 * using System;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerStateMove : IPlayerState
{
    private Vector3 _initialTarget;
    private PlayerController _player;

    private int cnt = 0;
    public PlayerStateMove(PlayerController player)
    {
        _player = player;
    }
    public void Enter()
    {
        Debug.Log("进入move状态");
        _player.RequestMove(_initialTarget);
    }

    public void Exit()
    {
    }

    public void Update()
    {
        if (!_player.IsOwner) return;
        if (Input.GetKeyDown(KeyCode.S))
        {
            _player.RequestStop(); // -> ServerStopMove -> BossMotionState=Idle
            return;
        }
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = _player.MainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit groundHit, 1000f, _player.GroundLayer))
            {
                _player.RequestMove(groundHit.point);
            }
        }



        //// 1. 客户端逻辑：检测输入并发送请求
        //if (_player.IsOwner)
        //{
        //    HandleInput();
        //}
        //// 2. 服务器逻辑：执行实际移动
        //// 注意：因为有 NetworkTransform，服务器移动后会自动同步给客户端
        //if (_player.IsServer) // --> 我的修改
        //{
        //    if (!_player.IsMoving)
        //    {
        //        Debug.Log("停止移动了！！！");
        //        // 注意：这里利用了 NetworkTransform 同步过来的 position
        //        // 客户端发现自己到了，切 Idle。
        //        // 服务器发现玩家到了，也切 Idle。
        //        // 大家达成"逻辑上的默契"，而不需要发 RPC 互相通知。
        //        _player.ChangeState(new PlayerStateIdle(_player));  // 第一步，服务器的复制体先切换状态

        //        _player.ChangeStateToIdleClientRpc();
        //        //_player.SwitchToIdleClientRpc();//  第二步，客户端同步执行 --> 不需要这里
        //    }
        //}
    }
    private void HandleInput()
    {
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            _player.RequestStop();
            //_player.RequestMove(_player.transform.position);
            //_player.ChangeState(new PlayerStateIdle(_player));
            return;
        }
        // 鼠标右键点击 (0是左键, 1是右键, 2是中键)
        if (Input.GetMouseButtonDown(1))
        {
            if (_player.IsServer)
            {
                Debug.Log("服务器实例输入");
            }
            else if (_player.IsClient)
            {
                Debug.Log("客户端实例输入");
            }
            Ray ray = _player.MainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // 射线检测，只检测 Ground 层
            if (Physics.Raycast(ray, out hit, 1000f, _player.GroundLayer))
            {
                // 发送 RPC 给服务器，请求移动
                // 注意：只发送点击的坐标点，不直接移动
                _player.RequestMove(hit.point);
            }
        }
    }
}

 */
