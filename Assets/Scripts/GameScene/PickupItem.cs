using Unity.Netcode;
using UnityEngine;

public class PickupItem : NetworkBehaviour, IInteractable
{
    private int points = 10;
    public string InteractionPrompt => "Pickup";

    public void Interact(GameObject source)
    {
        RequestPickupServerRpc(source.GetComponent<NetworkObject>().NetworkObjectId);
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(ulong sourceId)
    {
        // 2. 服务器负责：验证、加分、销毁
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(sourceId, out var sourceNetObj))
        {
            // 验证距离
            if (Vector3.Distance(transform.position, sourceNetObj.transform.position) > 3.0f) return;
            // 获取玩家核心组件
            // 增加分数 + 物品入背包
            //if (sourceNetObj.TryGetComponent<PlayerNetworkCore>(out var playerdata))
            if (sourceNetObj.TryGetComponent<PlayerDataContainer>(out var playerdata))
            {
            // 直接调用 Server 端方法加分
            playerdata.AddPointsServer(points);
            // 调用 server 端方法入背包
            playerdata.AddItemServer(gameObject.name);

            // 销毁药水
            GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
