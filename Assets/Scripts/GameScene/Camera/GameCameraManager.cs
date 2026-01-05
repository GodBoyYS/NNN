using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine; // 注意：命名空间变成了 Unity.Cinemachine

public class GameCameraManager : MonoBehaviour
{
    public static GameCameraManager Instance { get; private set; }

    [Header("Cinemachine Components")]
    // 这里的类型从 CinemachineVirtualCamera 变成了 CinemachineCamera
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        Instance = this;
    }

    public void SetFollowTarget(Transform target)
    {
        if (virtualCamera != null)
        {
            // 在 CM 3.0 中，Follow 和 LookAt 统一归为 "Target" 管理
            // 但如果使用的是 simple Follow 模式，直接赋值给 Follow 属性依然有效
            virtualCamera.Follow = target;

            // 如果你是上帝视角（Top-Down），通常不需要 LookAt，保持默认即可
            // virtualCamera.LookAt = target; 

            Debug.Log($"[Camera] Now following {target.name}");
        }
    }

    public void ShakeCamera(float force)
    {
        //Debug.Log("调用震屏方法");
        if (impulseSource != null)
        {
            //Debug.Log("振动源不空，震屏！");
            impulseSource.GenerateImpulse(force);
        }
    }
}
