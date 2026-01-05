using Unity.Netcode;
using UnityEngine;

public class InteractableTest : NetworkBehaviour
{
    private NetworkVariable<float> _health = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        _health.OnValueChanged += OnHealthChanged;
    }
    public override void OnNetworkDespawn()
    {
        _health.OnValueChanged -= OnHealthChanged;
    }
    private void OnHealthChanged(float prev, float cur)
    {
        if (!IsServer) return;
        if(cur <= 0f)
        {
            NetworkObject.Despawn();
        }
    }
    [ServerRpc]
    public void ServerTakeDamageServerRpc(float damage)
    {
        if(!IsServer) return;
        _health.Value -= damage;
        Debug.Log("木桩受到伤害");
    }
}
