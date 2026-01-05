
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerNetworkMovement : NetworkBehaviour, IKnockBackable, ITeleportable
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotateSpeed = 15f;
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Physics")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float drag = 2.0f;
    [SerializeField] private LayerMask groundLayer; // 【新增】务必在 Inspector 中赋值！
    [SerializeField] private float groundCheckOffset = 2.0f; // 【新增】射线检测高度偏移

    private Vector3 _serverTargetPosition;
    private Vector3 _smoothDampVelocity;
    private Vector3 _velocity;
    private bool _isKnockedBack = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _serverTargetPosition = transform.position;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (_isKnockedBack)
        {
            HandleKnockbackPhysics();
            return;
        }

        ProcessMovementServer();
    }

    #region Public API
    public void RequestMove(Vector3 worldPos)
    {
        if (IsOwner)
        {
            RequestMoveServerRpc(worldPos);
        }
    }

    public void RequestStop()
    {
        if (IsOwner)
        {
            RequestStopServerRpc();
        }
    }
    #endregion

    #region Server Logic
    [ServerRpc]
    private void RequestMoveServerRpc(Vector3 pos)
    {
        if (_isKnockedBack) return;
        // 【修改】接受点击点的真实位置，虽然我们主要还是靠射线检测地形
        _serverTargetPosition = pos;
    }

    [ServerRpc]
    private void RequestStopServerRpc()
    {
        ServerStopMove();
    }

    public void ServerStopMove()
    {
        if (!IsServer) return;
        _serverTargetPosition = transform.position;
        _smoothDampVelocity = Vector3.zero;
        _velocity = Vector3.zero;
    }

    public void ServerLookAt(Vector3 targetPos)
    {
        if (!IsServer) return;
        Vector3 direction = targetPos - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    private void ProcessMovementServer()
    {
        // 1. 计算平面（X/Z）的下一个位置
        Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPosFlat = new Vector3(_serverTargetPosition.x, 0, _serverTargetPosition.z);

        if (Vector3.SqrMagnitude(targetPosFlat - currentPosFlat) < 0.01f) return;

        Vector3 directionToTarget = targetPosFlat - currentPosFlat;

        // 旋转
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(directionToTarget.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // 移动 X 和 Z
        Vector3 newPosFlat = Vector3.SmoothDamp(
            currentPosFlat,
            targetPosFlat,
            ref _smoothDampVelocity,
            smoothTime,
            moveSpeed
        );

        // 2. 计算高度（Y）：贴地逻辑
        float newY = transform.position.y;

        // 从新位置的上方发射射线向下检测地面
        // 注意：groundCheckOffset 要足够高，以防坡度太陡
        Vector3 rayOrigin = new Vector3(newPosFlat.x, transform.position.y + groundCheckOffset, newPosFlat.z);

        // 建议射线长度设长一点，以防掉坑里检测不到
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20.0f, groundLayer))
        {
            newY = hit.point.y;
        }
        else
        {
            // 如果你也想在非 Knockback 状态下应用重力（例如走下悬崖），可以在这里写
            // newY += gravity * Time.deltaTime;
        }

        // 3. 应用最终位置
        transform.position = new Vector3(newPosFlat.x, newY, newPosFlat.z);
    }

    public void ApplyKnockbackServer(Vector3 forceDir, float forceStrength)
    {
        if (!IsServer) return;
        _isKnockedBack = true;
        _velocity = forceDir.normalized * forceStrength;
        _serverTargetPosition = transform.position;
    }

    public void TeleportServer(Vector3 position)
    {
        if (!IsServer) return;
        transform.position = position;
        _serverTargetPosition = position;
        _smoothDampVelocity = Vector3.zero;
        _velocity = Vector3.zero;
        _isKnockedBack = false;
        Debug.Log($"[Movement] Teleported to {position}");
    }

    private void HandleKnockbackPhysics()
    {
        float dt = Time.deltaTime;
        _velocity.y += gravity * 2.0f * dt;

        Vector3 horizontalVel = new Vector3(_velocity.x, 0, _velocity.z);
        horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, drag * dt);

        _velocity = new Vector3(horizontalVel.x, _velocity.y, horizontalVel.z);

        Vector3 nextPos = transform.position + _velocity * dt;

        // 简单的地面碰撞检测
        if (Physics.Raycast(nextPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.0f, groundLayer))
        {
            if (_velocity.y < 0 && nextPos.y <= hit.point.y)
            {
                _isKnockedBack = false;
                _velocity = Vector3.zero;
                transform.position = new Vector3(nextPos.x, hit.point.y, nextPos.z);
                _serverTargetPosition = transform.position;
                return;
            }
        }

        transform.position = nextPos;
    }

    public void ServerReset()
    {
        _serverTargetPosition = transform.position;
        _smoothDampVelocity = Vector3.zero;
        _velocity = Vector3.zero;
        _isKnockedBack = false;
    }
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(gameObject);
            }
        }
    }
}
