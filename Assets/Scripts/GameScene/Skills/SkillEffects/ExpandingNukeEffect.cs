using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class ExpandingNukeEffect : SkillEffect
{
    [Header("核爆数值配置")]
    public float damageRadius = 10f; // 伤害半径
    public int killDamage = 9999;    // 伤害数值

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 视觉效果已经由 BossController 在蓄力阶段播放完毕
        // 这里只负责“结算”那一瞬间的伤害

        // 1. 获取施法者ID (用于防止误伤)
        ulong attackerId = 0;
        if (caster.TryGetComponent<NetworkObject>(out var netObj))
        {
            attackerId = netObj.NetworkObjectId;
        }

        // 2. 范围检测与伤害判定
        Collider[] hits = Physics.OverlapSphere(position, damageRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == caster) continue; // 不炸自己

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(killDamage, attackerId);
                Debug.Log($"[Effect] 核爆命中: {hit.name}，造成 {killDamage} 伤害");
            }
        }

        // 可选：在这里生成一个“爆炸瞬间”的特效（Explosion VFX），那是属于 Execution 阶段的视觉
    }
}
