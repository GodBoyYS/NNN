using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerNetworkCore))]
public class PlayerInputManager : NetworkBehaviour
{
    [Header("Raycast Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask interactLayer;

    [Header("Raycast")]
    [SerializeField] private float rayMaxDistance = 1000f;

    private Camera _mainCamera;
    //private PlayerAuthority _authority;
    private PlayerNetworkCore _core;
    private PlayerNetworkMovement _movement;
    private PlayerNetworkCombat _combat;
    private void Awake()
    {
        //_authority = GetComponent<PlayerAuthority>();
        _core = GetComponent<PlayerNetworkCore>();
        _movement = GetComponent<PlayerNetworkMovement>();
        _combat = GetComponent<PlayerNetworkCombat>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false; // 非owner不跑 update
            return;
        }
        _mainCamera = Camera.main;
        // 可选，死亡后禁用输入（authority也会拒绝命令，双保险）
        if( _core != null )
        {
            _core.LifeVar.OnValueChanged += OnLifeChanged;
        }
        //if (_authority != null)
        //    _authority.LifeVar.OnValueChanged += OnLifeChanged;
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner && _core != null)
            _core.LifeVar.OnValueChanged -= OnLifeChanged;
            //_authority.LifeVar.OnValueChanged -= OnLifeChanged;
    }
    private void OnLifeChanged(PlayerNetworkStates.LifeState oldState, PlayerNetworkStates.LifeState newState)
    {
        if (newState == PlayerNetworkStates.LifeState.Dead)
            enabled = false;
    }

    private void Update()
    {
        if (!IsOwner) return;
        //if (_authority == null) return;
        if (_movement == null) return;
        // Stop:演示S键停止
        if (Input.GetKeyDown(KeyCode.S))
        {
            //_authority.RequestStop();
            _movement.RequestStop();
            return;
        }
        if (Input.GetKeyDown(KeyCode.A)) // 假设A键测试普攻，或者用鼠标左键
        {
            // 请求索引 0 (普攻)
            // 这里的 aimPos 可以是鼠标指向的位置
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 aimPos = transform.position + transform.forward; // 默认前方
            _combat.RequestCastSkill(0, aimPos);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 请求索引 1 (Q技能)
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 aimPos = transform.position;
            if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, groundLayer))
            {
                aimPos = hit.point;
            }

            _combat.RequestCastSkill(1, aimPos);
            return;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            // 请求索引 1 (Q技能)
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 aimPos = transform.position;
            if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, groundLayer))
            {
                aimPos = hit.point;
            }
            _combat.RequestCastSkill(2, aimPos);
            return;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 请求索引 1 (Q技能)
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 aimPos = transform.position;
            if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, groundLayer))
            {
                aimPos = hit.point;
            }
            _combat.RequestCastSkill(3, aimPos);
            return;
        }

        // move:右键点击地面 or 可交互物体
        if (Input.GetMouseButtonDown(1))
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            // 点击到交互层，先不处理
            if (Physics.Raycast(ray, out RaycastHit interactHit, rayMaxDistance, interactLayer))
            {
                if(interactHit.collider.TryGetComponent<IInteractable>(out var interact))
                {
                    // 如果存在 交互prompt，那么接口代码是否应该是
                    // interact.Interact(gameObject, "Open"/ "PickUp");
                    Debug.Log("点击到交互层，获取交互接口");
                    interact.Interact(gameObject);
                }
                //Debug.Log("点击到交互层，攻击 组件暂时没有处理攻击");
                //var netObj = interactHit.collider.GetComponentInParent<NetworkObject>();
                //_authority.RequestAttack(netObj);
                //_movement.RequestAttack(netObj);
                return;
            }
            if(Physics.Raycast(ray, out RaycastHit groundHit, rayMaxDistance, groundLayer))
            {
                //_authority.RequestMove(groundHit.point);
                _movement.RequestMove(groundHit.point);
            }
        }
    }
}
