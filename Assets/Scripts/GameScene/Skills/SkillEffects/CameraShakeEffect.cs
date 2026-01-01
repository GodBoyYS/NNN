using System;
using UnityEngine;

[Serializable]
public class CameraShakeEffect : SkillEffect
{
    [Header("屏幕震动配置")]
    [Tooltip("震动力度 (0.1 - 5.0)")]
    public float shakeForce = 1.0f;

    [Tooltip("只有释放者自己能感受到震动吗？")]
    public bool onlyLocalPlayer = true;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 只有在客户端才处理视觉表现
        if (GameCameraManager.Instance == null) return;

        bool shouldShake = false;

        if (onlyLocalPlayer)
        {
            // 只有当释放者是本机玩家时，才震动
            // 注意：我们需要检查 Caster 的 NetworkObject 是否是 IsOwner
            if (caster.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
            {
                if (netObj.IsOwner) shouldShake = true;
            }
        }
        else
        {
            // 全局震动 (例如 Boss 砸地，所有人都应该感觉到)
            // 这里可以简单处理为只要执行就震动，
            // 进阶做法是计算 Camera 和 position 的距离，太远就不震
            shouldShake = true;
        }

        if (shouldShake)
        {
            GameCameraManager.Instance.ShakeCamera(shakeForce);
        }
    }
}