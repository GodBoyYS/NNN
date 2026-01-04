using System;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerNetworkMovement : NetworkBehaviour, 
    IKnockBackable, 
    ITeleportable
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 6f; 
    [SerializeField] private float rotateSpeed = 15f; 
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Physics")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float drag = 2.0f;

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
        _serverTargetPosition = new Vector3(pos.x, transform.position.y, pos.z);
    }

    [ServerRpc]
    private void RequestStopServerRpc()
    {
        ServerStopMove();
    }

    /// <summary>
    /// 服务器端强制停止移动
    /// </summary>
    public void ServerStopMove()
    {
        if (!IsServer) return;
        _serverTargetPosition = transform.position;
        _smoothDampVelocity = Vector3.zero;
    }

    /// <summary>
    /// 服务器端强制看向目标点
    /// </summary>
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
        float currentY = transform.position.y;
        Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPosFlat = new Vector3(_serverTargetPosition.x, 0, _serverTargetPosition.z);

        // 如果非常接近目标点，则停止计算，避免抖动，且允许其他逻辑（如Skill）控制旋转
        if (Vector3.SqrMagnitude(targetPosFlat - currentPosFlat) < 0.01f) return;

        // 旋转
        Vector3 directionToTarget = targetPosFlat - currentPosFlat;
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(directionToTarget.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // 移动
        Vector3 newPosFlat = Vector3.SmoothDamp(
            currentPosFlat,
            targetPosFlat,
            ref _smoothDampVelocity,
            smoothTime,
            moveSpeed
        );

        transform.position = new Vector3(newPosFlat.x, currentY, newPosFlat.z);
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
        // 瞬移逻辑：直接设置位置，并重置目标点和速度，防止回弹
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