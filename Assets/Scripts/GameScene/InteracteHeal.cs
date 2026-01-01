using Unity.Netcode;
using UnityEngine;

public class InteracteHeal : NetworkBehaviour, IInteractable
{
    public string InteractionPrompt => "Eat";

    public void Interact(GameObject source)
    {
        ulong sourceId = source.GetComponent<NetworkObject>().NetworkObjectId;
        RequestInteractServerRpc(sourceId);
    }
    [ServerRpc(RequireOwnership = false)] // 允许任何人调用，不仅仅是物体的Owner
    private void RequestInteractServerRpc(ulong sourceId)
    {
        if(NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(sourceId, out var sourceObject))
        {
            // 验证距离（防止作弊，千里之外吃药）
            if (Vector3.Distance(transform.position, sourceObject.transform.position) > 3.0f) return;
            if(sourceObject.TryGetComponent<PlayerNetworkHealth>(out var health))
            {
                health.ServerHeal(10);
                GetComponent<NetworkObject>().Despawn(); 
            }
        }
    }
}
