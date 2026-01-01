using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerNetworkMovement : NetworkBehaviour,
    IKnockBackable
{
    [Header("Server Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 10f;

    [Header("Physics Settings")]
    [SerializeField] private float gravity = -9.81f; // 重力加速度
    [SerializeField] private float drag = 2.0f;      // 水平阻力
    // --- 击飞相关变量 ---
    private bool _isKnockedBack = false;
    private Vector3 _velocity; // 统一管理当前速度（包含击飞和重力）

    private Vector3 _serverTargetPosition;

    private PlayerNetworkCore _core;

    // server-only events（Combat 用来监听到点）
    public event Action ServerReachedDestination;

    // server-only events（Owner 输入导致的 move/stop，用于 Combat 取消追逐）
    //public event Action ServerOwnerIssuedMoveCommand;
    //public event Action ServerOwnerIssuedStopCommand;

    private void Awake()
    {
        _core = GetComponent<PlayerNetworkCore>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _serverTargetPosition = transform.position;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
        //_motionState.Value = PlayerNetworkStates.BossMotionState.Idle;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (_core != null && _core.IsDead) return;

        ProcessMovementServer();
    }
    private IEnumerator RecoverFromKnockback(float duration)
    {
        yield return new WaitForSeconds(duration);
        _isKnockedBack = false;
    }

    // ========= client -> server : public request =========
    public void RequestMove(Vector3 worldPos)
    {
        if (!IsOwner) return;
        RequestMoveServerRpc(worldPos);
    }

    public void RequestStop()
    {
        if (!IsOwner) return;
        RequestStopServerRpc();
    }

    // ========= server-side direct control (Combat can call) =========
    public void ServerMoveTo(Vector3 worldPos)
    {
        if (!IsServer) return;
        if (_core != null && _core.IsDead) return;

        _serverTargetPosition = new Vector3(worldPos.x, transform.position.y, worldPos.z);
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Moving);
        //_motionState.Value = PlayerNetworkStates.BossMotionState.Moving;
    }

    public void ServerForceStop()
    {
        if (!IsServer) return;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
        //_motionState.Value = PlayerNetworkStates.BossMotionState.Idle;
        _serverTargetPosition = transform.position;
    }
    // 在 PlayerNetworkMovement 中添加
    public void ServerForceDash(Vector3 direction, float distance, float duration)
    {
        if (!IsServer) return;
        // 简单的瞬移实现，如果是冲刺过程需要配合协程或修改 TargetPosition
        // 这里为了演示方便，做一个带物理检测的“瞬间突进”

        // 实际项目中建议使用协程平滑插值，或者给 CharacterController 施加瞬时速度
        // 这里我们简单修改 _serverTargetPosition 并强制瞬移
        Vector3 targetPos = transform.position + direction.normalized * distance;

        // 防止穿墙 (使用 Raycast)
        //if (Physics.Raycast(transform.position + Vector3.up, direction.normalized, out RaycastHit hit, distance, groundLayer))
        //{
        //    targetPos = hit.point;
        //}

        _serverTargetPosition = targetPos;
        transform.position = targetPos; // 瞬移
                                        // 如果要平滑冲刺，这里应该设置一个特殊状态 IsDashing，在 Update 里快速移动
    }

    public void ServerReset()
    {
        if (!IsServer) return;
        _serverTargetPosition = transform.position;
        //_motionState.Value = PlayerNetworkStates.BossMotionState.Idle;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
    }

    // ========= ServerRpc =========
    [ServerRpc]
    private void RequestMoveServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        if (_core != null && _core.IsDead) return;

        // 额外保险：只接收 owner 的输入
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
        // 如果正在被击飞，禁止玩家输入移动
        if (_isKnockedBack) return;
        _serverTargetPosition = new Vector3(pos.x, transform.position.y, pos.z);
        //_motionState.Value = PlayerNetworkStates.BossMotionState.Moving;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Moving);

        // 通知 Combat：这是“手动移动”，可以取消追逐
        //ServerOwnerIssuedMoveCommand?.Invoke();
    }

    [ServerRpc]
    private void RequestStopServerRpc(ServerRpcParams rpcParams = default)
    {
        if (_core != null && _core.IsDead) return;
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        ServerForceStop();

        // 通知 Combat：这是“手动停止”
        //ServerOwnerIssuedStopCommand?.Invoke();
    }

    // ========= server simulation =========
    private void ProcessMovementServer()
    {
        // --- 🅰️ 击飞/物理状态处理 ---
        if (_isKnockedBack)
        {
            float dt = Time.deltaTime;
            // 1. 应用重力 (只影响 Y 轴)
            // 乘以 2.0f 是为了让游戏里的下落感更重，不飘
            _velocity.y += gravity * 2.0f * dt;
            // 2. 应用水平阻力 (只影响 X, Z)
            Vector3 horizontalVel = new Vector3(_velocity.x, 0, _velocity.z);
            // 简单的线性阻力，或者使用 Lerp
            horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, drag * dt);
            // 重新组合速度
            _velocity = new Vector3(horizontalVel.x, _velocity.y, horizontalVel.z);
            // 3. 应用位移
            transform.position += _velocity * dt;
            // 4. 落地检测 (Ground Check)
            // 简单的射线检测脚底
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 0.6f))
            {
                // 如果垂直速度向下（正在下落）且离地面很近
                if (_velocity.y < 0)
                {
                    Debug.Log("落地！结束击飞状态");
                    _isKnockedBack = false;
                    _velocity = Vector3.zero;

                    // 修正位置贴地，防止穿模
                    transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                    _serverTargetPosition = transform.position; // 重置导航点为当前位置
                }
            }
            // 强制退出，不执行下面的 NavMesh/MoveTowards 逻辑
            return;
        }
        // --- 🅱️ 正常移动处理 (你的原有逻辑) ---
        if (_core.Motion != PlayerNetworkStates.MotionState.Moving) return;

        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _serverTargetPosition, step);

        Vector3 direction = _serverTargetPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, _serverTargetPosition) < 0.01f)
        {
            _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
            ServerReachedDestination?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (_core != null && _core.IsDead) return;
        if (collision == null) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            ServerForceStop();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (_core != null && _core.IsDead) return;
        if (other == null) return;
        if (other.gameObject.TryGetComponent<IInteractable>(out var interact))
        {
            interact.Interact(gameObject);
        }
    }

    public void ApplyKnockbackServer(Vector3 forceDir, float forceStrength)
    {
        if (!IsServer) return;
        // 重置状态
        _isKnockedBack = true;
        // 核心：给一个瞬间的初速度
        _velocity = forceDir.normalized * forceStrength;
        // 稍微打断当前的移动状态，防止逻辑冲突
        _serverTargetPosition = transform.position;
    }
}
