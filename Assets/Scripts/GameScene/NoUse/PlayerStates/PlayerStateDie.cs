using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerStateDie : IPlayerState
{
    private float DieTime = 3f;
    private float timer = 0;
    private PlayerPresentation _view;
    public PlayerStateDie(PlayerPresentation view) => _view = view;

    public void Enter()
    {
        if (_view.CapsuleCollider != null)
        {
            _view.CapsuleCollider.enabled = false;
        }
        if (_view.Rigidbody != null)
        {
            _view.Rigidbody.isKinematic = true;
        }
        Debug.Log("进入 Die（Presentation）：碰撞器失效、刚体冻结（Despawn 由 Server Authority 负责）");
        _view.Animator.Play("Die01_SwordAndShield");
    }

    public void Exit()
    {

    }

    public void Update()
    {
    }
}

//using Unity.Netcode;
//using Unity.VisualScripting;
//using UnityEngine;

//public class PlayerStateDie : IPlayerState
//{
//    private float DieTime = 3f;
//    private float timer = 0;
//    private PlayerController _player;
//    public PlayerStateDie(PlayerController player)
//    {
//        _player = player;
//    }
//    public void Enter()
//    {
//        // 1.表现层逻辑（server+client都要执行）
//        if(_player.CapsuleCollider != null)
//        {
//            _player.CapsuleCollider.enabled = false;
//        }
//        // 停止物理计算
//        if(_player.Rigidbody != null)
//        {
//            _player.Rigidbody.isKinematic = true;
//        }
//        Debug.Log("角色死亡，碰撞器失效，3秒后角色消失");
//    }

//    public void Exit()
//    {

//    }

//    public void Update()
//    {
//        // 2.核心逻辑（只有server有权销毁物体）
//        if (_player.IsServer)
//        {
//            timer += Time.deltaTime;
//            if(timer > DieTime)
//            {
//                // 严禁使用 gameobject.destroy
//                // 必须使用 networkobject.despawn
//                _player.GetComponent<NetworkObject>().Despawn();
//                Debug.Log("server 已经销毁玩家");
//            }
//        }
//    }
//}
