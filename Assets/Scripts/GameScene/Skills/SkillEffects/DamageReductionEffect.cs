// [FILE START: Assets\Scripts\GameScene\Skills\SkillEffects\DamageReductionEffect.cs]
using System;
using UnityEngine;

[Serializable]
public class DamageReductionEffect : SkillEffect
{
    [Range(0f, 1f)]
    public float reductionPercentage = 0.5f; // 50% 减伤
    public float duration = 3.0f;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 尝试从施法者身上获取减伤接口
        if (caster.TryGetComponent<IDamageMitigator>(out var mitigator))
        {
            mitigator.ApplyDamageReduction(reductionPercentage, duration);
        }
        else
        {
            Debug.LogWarning($"[DamageReductionEffect] {caster.name} 没有实现 IDamageMitigator 接口，无法应用减伤。");
        }
    }
}
// [FILE END]
