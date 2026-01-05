using UnityEngine;

public interface IKnockBackable
{
    // forceDir: 方向, forceStrength: 力度
    void ApplyKnockbackServer(Vector3 forceDir, float forceStrength);
}
