// »ýÄ¾A£ºÉËº¦
using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class DamageEffect : SkillEffect
{
    public int damageAmount = 10;
    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        if (target == null) return;
        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(
                damageAmount,
                caster.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }
}