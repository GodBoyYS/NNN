// [FILE START: Assets\Scripts\GameScene\Skills\SkillEffects\FollowAreaDamageEffect.cs]
using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class FollowAreaDamageEffect : SkillEffect
{
    [Header("Prefab Settings")]
    [Tooltip("必须挂载 DamageAuraController 和 NetworkObject 的预制体")]
    public DamageAuraController auraPrefab;

    [Header("Aura Stats")]
    public float radius = 3.0f;
    public int damagePerTick = 5;
    public float tickInterval = 0.5f;
    public float duration = 3.0f;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 只有服务器有权限生成网络对象并造成伤害
        if (!NetworkManager.Singleton.IsServer) return;

        if (auraPrefab == null)
        {
            Debug.LogError("[FollowAreaDamageEffect] 未配置 Aura Prefab！");
            return;
        }

        // 1. 生成预制体
        var auraInstance = UnityEngine.Object.Instantiate(auraPrefab, caster.transform.position, Quaternion.identity);

        // 2. 获取组件并初始化
        auraInstance.Initialize(
            caster.GetComponent<NetworkObject>(),
            damagePerTick,
            radius,
            tickInterval,
            duration
        );

        // 3. 在网络上生成
        auraInstance.GetComponent<NetworkObject>().Spawn();
    }
}
// [FILE END]
