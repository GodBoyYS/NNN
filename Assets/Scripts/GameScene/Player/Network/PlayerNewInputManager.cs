using System.Globalization;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
// 1. 定义输入数据包
public struct FrameInput
{
    public Vector2 MoveInput; // WASD 输入
    public bool AttackDown; // 左键
    public bool SkillQDown; // Q键
    public bool SkillWDown; // W键
    public bool SkillEDown; // E键
    public bool StopDown; // S键 (强制停止)
    public bool InteractDown; // 右键 (交互)
    public Vector3 MouseWorldPos; // 鼠标点击的世界坐标(用于移动/技能瞄准)
    public bool HasMouseTarget; // 鼠标是否点到了有效地面/目标
    public RaycastHit MouseHit;
}


// 2. 改造 InputManager
public class PlayerNewInputManager : NetworkBehaviour
{
    [Header("Raycast Layers")]
    [SerializeField] private LayerMask groundLayer;
    private Camera _mainCamera;
    public FrameInput CurrentInput { get; private set; }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _mainCamera = Camera.main;
        }
        else
        {
            enabled = false; // 非本地玩家不需要处理输入
        }
    }
    private void Update()
    {
        if (!IsOwner) return;
        CurrentInput = GatherInput();
    }
    private FrameInput GatherInput()
    {
        var input = new FrameInput
        {
            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            StopDown = Input.GetKeyDown(KeyCode.S),
            AttackDown = Input.GetKeyDown(KeyCode.A),
            SkillQDown = Input.GetKeyDown(KeyCode.Q),
            SkillWDown = Input.GetKeyDown(KeyCode.W),
            SkillEDown = Input.GetKeyDown(KeyCode.E),
            InteractDown = Input.GetMouseButtonDown(1),
            HasMouseTarget = false,
        };

        // 获取鼠标射线结果
        if (_mainCamera != null)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                input.MouseWorldPos = hit.point;
                input.HasMouseTarget = true;
                input.MouseHit = hit;
            }
        }

        return input;
    }
}
