using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerNetworkMovement : NetworkBehaviour, IKnockBackable
{
    [Header("Server Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotateSpeed = 15f; // 稍微调高旋转速度，配合“无惯性”转向更跟手
    [SerializeField] private float smoothTime = 0.15f; // 惯性平滑时间

    [Header("Physics Settings")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float drag = 2.0f;

    // --- 击飞相关变量 ---
    private bool _isKnockedBack = false;
    private Vector3 _velocity; // 击飞物理速度
    private Vector3 _smoothDampVelocity; // 移动惯性速度 (SmoothDamp 专用)

    private Vector3 _serverTargetPosition;

    private PlayerNetworkCore _core;

    public event Action ServerReachedDestination;

    private void Awake()
    {
        _core = GetComponent<PlayerNetworkCore>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _serverTargetPosition = transform.position;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
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

    // ========= Client Requests =========
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

    // ========= Server Direct Control =========
    public void ServerMoveTo(Vector3 worldPos)
    {
        if (!IsServer) return;
        if (_core != null && _core.IsDead) return;

        _serverTargetPosition = new Vector3(worldPos.x, transform.position.y, worldPos.z);
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Moving);
    }

    public void ServerForceStop()
    {
        if (!IsServer) return;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
        _serverTargetPosition = transform.position;
        _smoothDampVelocity = Vector3.zero; // 强制停止时清除惯性
    }

    public void ServerForceDash(Vector3 direction, float distance, float duration)
    {
        if (!IsServer) return;
        Vector3 targetPos = transform.position + direction.normalized * distance;
        _serverTargetPosition = targetPos;
        transform.position = targetPos;
        _smoothDampVelocity = Vector3.zero;
    }

    public void ServerReset()
    {
        if (!IsServer) return;
        _serverTargetPosition = transform.position;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
        _smoothDampVelocity = Vector3.zero;
    }

    // ========= RPCs =========
    [ServerRpc]
    private void RequestMoveServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        if (_core != null && _core.IsDead) return;
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
        if (_isKnockedBack) return;

        _serverTargetPosition = new Vector3(pos.x, transform.position.y, pos.z);
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Moving);
    }

    [ServerRpc]
    private void RequestStopServerRpc(ServerRpcParams rpcParams = default)
    {
        if (_core != null && _core.IsDead) return;
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        ServerForceStop();
    }

    // ================= 核心修改区域 =================
    private void ProcessMovementServer()
    {
        // 1. 处理击飞 (物理模拟，优先级最高)
        if (_isKnockedBack)
        {
            float dt = Time.deltaTime;
            _velocity.y += gravity * 2.0f * dt;

            Vector3 horizontalVel = new Vector3(_velocity.x, 0, _velocity.z);
            horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, drag * dt);

            _velocity = new Vector3(horizontalVel.x, _velocity.y, horizontalVel.z);
            transform.position += _velocity * dt;

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 0.6f))
            {
                if (_velocity.y < 0)
                {
                    _isKnockedBack = false;
                    _velocity = Vector3.zero;
                    transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                    _serverTargetPosition = transform.position;
                }
            }
            return;
        }

        // 2. 检查移动状态
        if (_core.Motion != PlayerNetworkStates.MotionState.Moving) return;

        // 准备数据
        float currentY = transform.position.y;
        Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPosFlat = new Vector3(_serverTargetPosition.x, 0, _serverTargetPosition.z);

        // ---------------------------------------------------------------------
        // A. 旋转逻辑：【无视惯性】
        // 直接计算“当前位置 -> 目标点”的向量。
        // 只要目标点没变，这个向量就是稳定的，不会受到 SmoothDamp 减速的影响。
        // ---------------------------------------------------------------------
        Vector3 directionToTarget = targetPosFlat - currentPosFlat;

        // 只有当距离足够远，且方向向量有效时才旋转，避免重叠时胡乱旋转
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(directionToTarget.normalized);
            // 这里依然保留 Slerp 是为了视觉不那么生硬（瞬间跳变），但源头数据已经是纯净的目标方向了
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // ---------------------------------------------------------------------
        // B. 位移逻辑：【保留惯性】
        // 使用 SmoothDamp 制造平滑的加减速
        // ---------------------------------------------------------------------
        Vector3 newPosFlat = Vector3.SmoothDamp(
            currentPosFlat,
            targetPosFlat,
            ref _smoothDampVelocity, // 惯性速度记录在这里
            smoothTime,
            moveSpeed
        );

        transform.position = new Vector3(newPosFlat.x, currentY, newPosFlat.z);

        // ---------------------------------------------------------------------
        // C. 到达判定
        // ---------------------------------------------------------------------
        float dist = Vector3.Distance(currentPosFlat, targetPosFlat);
        // 距离很近 且 速度很慢 时才算停下
        if (dist < 0.2f && _smoothDampVelocity.sqrMagnitude < 0.1f)
        {
            _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
            ServerReachedDestination?.Invoke();
            _smoothDampVelocity = Vector3.zero;
        }
    }
    // ===============================================

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
        _isKnockedBack = true;
        _velocity = forceDir.normalized * forceStrength;
        _serverTargetPosition = transform.position;
        _smoothDampVelocity = Vector3.zero;
    }
}