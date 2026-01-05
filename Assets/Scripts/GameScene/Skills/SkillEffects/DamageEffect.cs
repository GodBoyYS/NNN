// [FILE START: Assets\Scripts\GameScene\Skills\SkillEffects\DamageEffect.cs]
using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class DamageEffect : SkillEffect
{
    [Header("Damage Settings")]
    public int damageAmount = 10;

    [Header("Area Settings")]
    [Tooltip("攻击判定的中心点距离施法者前方的距离")]
    public float forwardDistance = 1.5f;
    [Tooltip("攻击判定的半径")]
    public float attackRadius = 1.5f;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 1. 计算攻击判定中心点：施法者位置 + 前方偏移
        // 注意：不使用 position 参数（那是点击位置），我们只关心施法者朝向
        Vector3 attackCenter = caster.transform.position + caster.transform.forward * forwardDistance;

        // 2. 获取施法者的 NetworkObjectID (用于避免误伤自己)
        ulong attackerId = 0;
        if (caster.TryGetComponent<NetworkObject>(out var casterNetObj))
        {
            attackerId = casterNetObj.NetworkObjectId;
        }

        // 3. 范围检测
        Collider[] hits = Physics.OverlapSphere(attackCenter, attackRadius);
        bool hasHit = false;

        foreach (var hit in hits)
        {
            // 排除自己
            if (hit.gameObject == caster) continue;

            // 尝试获取 IDamageable 接口
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damageAmount, attackerId);
                hasHit = true;
                // Debug.Log($"[DamageEffect] Hit {hit.name}");
            }
        }

        // 可选：在这里添加一些打击特效 (VFX) 或音效
        // if(hasHit) { ... }
    }
}
// [FILE END]
