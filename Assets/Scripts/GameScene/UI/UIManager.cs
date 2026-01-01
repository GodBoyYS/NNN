using UnityEngine;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameHUDView hudView; // 拖拽引用场景里的 HUD

    private void Awake()
    {
        Instance = this;
    }

    // 当本地玩家生成时调用这个方法
    public void OnLocalPlayerSpawned(PlayerNetworkHealth health, PlayerNetworkCombat combat)
    {
    }

    public void OnLocalPlayerDespawned()
    {
    }
}