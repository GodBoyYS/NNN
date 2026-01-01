using Unity.Netcode;
using UnityEngine;

public class DestructableCrate : NetworkBehaviour, IDamageable
{
    [SerializeField] private NetworkObject _lootPrefab;
    private readonly NetworkVariable<int> _hp = new NetworkVariable<int>(
        50,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    public void TakeDamage(int amount, ulong attackerId)
    {
        _hp.Value -= amount;
        if( _hp.Value <= 0)
        {
            // 生成凋落物（就像单机游戏一样实例化）
            if(_lootPrefab != null)
            {
                var lootInstance = Instantiate(_lootPrefab, transform.position, Quaternion.identity);
                lootInstance.Spawn();
            }
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
