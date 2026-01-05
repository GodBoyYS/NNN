using Unity.Netcode;
using UnityEngine;

public class PotionSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject potionPrefab; // 拖入药水预制体
    [SerializeField] private Transform[] spawnPoints;    // 拖入场景中的空物体作为生成点

    public override void OnNetworkSpawn()
    {
        // 只在服务器运行生成逻辑
        if (IsServer)
        {
            foreach (var point in spawnPoints)
            {
                SpawnPotion(point.position);
            }
        }
    }

    private void SpawnPotion(Vector3 position)
    {
        if (potionPrefab == null) return;

        // 动态实例化
        var instance = Instantiate(potionPrefab, position, Quaternion.identity);

        // 网络生成 (这一步非常关键，它会告诉所有(包括后来的)客户端这里有个东西)
        instance.Spawn();
    }
}
