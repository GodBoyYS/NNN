using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount, ulong attackerId);
}
