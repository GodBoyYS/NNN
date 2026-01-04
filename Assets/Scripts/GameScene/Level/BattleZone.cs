using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI; // 引入 NavMesh 命名空间

[RequireComponent(typeof(BoxCollider))]
public class BattleZone : NetworkBehaviour
{
    [Header("配置")]
    [SerializeField] private GateController _exitGate;
    [SerializeField] private GateController _entryGate;

    [System.Serializable]
    public struct EnemyWave
    {
        public GameObject EnemyPrefab;
        public Transform SpawnPoint;
        [Tooltip("在该半径内随机生成")]
        public float SpawnRadius; // 新增：每个波次可以配置生成半径
        [Tooltip("一次生成数量")]
        public int Count; // 新增：支持配置这个点一次生几只怪
    }

    [SerializeField] private List<EnemyWave> _enemiesToSpawn;

    [Header("状态")]
    private bool _isZoneActive = false;
    private bool _isZoneCleared = false;
    private int _aliveEnemyCount = 0;
    private int _aliveBossCount = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (_exitGate != null) _exitGate.SetLocked(true);
            if (_entryGate != null) _entryGate.SetLocked(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (_isZoneActive || _isZoneCleared) return;

        if (other.CompareTag("Player"))
        {
            StartBattle();
        }
    }

    private void StartBattle()
    {
        _isZoneActive = true;
        Debug.Log($"[BattleZone] {gameObject.name} 战斗开始！");

        if (_entryGate != null) _entryGate.SetLocked(true);

        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        _aliveEnemyCount = 0;
        _aliveBossCount = 0;

        foreach (var waveData in _enemiesToSpawn)
        {
            // 默认数量为1，如果未配置Count或者是0，则视为1
            int spawnCount = waveData.Count > 0 ? waveData.Count : 1;
            // 默认半径，如果未配置则给一个小范围防止完全重叠
            float radius = waveData.SpawnRadius > 0 ? waveData.SpawnRadius : 2.0f;

            for (int i = 0; i < spawnCount; i++)
            {
                // --- 计算随机位置 ---
                Vector3 spawnPos = waveData.SpawnPoint.position;

                // 在圆内随机取点 (2D)
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                // 转换为 3D 坐标 (假设地面是 XZ 平面)
                Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                Vector3 potentialPos = spawnPos + randomOffset;

                // 【关键】确保点在 NavMesh 上 (防止生成到墙里或地板下)
                if (NavMesh.SamplePosition(potentialPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                }
                // ------------------

                NetworkObject enemyNetObj = NetworkObjectPool.Instance.GetNetworkObject(
                    waveData.EnemyPrefab,
                    spawnPos, // 使用计算后的随机位置
                    waveData.SpawnPoint.rotation
                );

                if (enemyNetObj != null)
                {
                    if (enemyNetObj.TryGetComponent<EnemyController>(out var enemyScript))
                    {
                        // 这里记得要根据你的 EnemyController 定义修改
                        // 确保你已经添加了 Action<NetworkObject> OnDied 事件
                        enemyScript.OnDied += HandleEnemyDeath;

                        // 【修复问题1的补充】：如果预制体禁用了Agent，这里可以开启
                        // 如果 EnemyController 自己会在 OnNetworkSpawn 里开启，这里就不需要写
                        var agent = enemyNetObj.GetComponent<NavMeshAgent>();
                        if (agent != null) agent.enabled = true;

                        _aliveEnemyCount++;
                    }
                    else if (enemyNetObj.TryGetComponent<BossController>(out var bossScript))
                    {
                        bossScript.OnBossDied += HandleEnemyDeathNoArg;
                        _aliveBossCount++;
                    }
                }
            }
        }

        Debug.Log($"[BattleZone] 生成了 {_aliveEnemyCount} 个敌人");
    }

    private void HandleEnemyDeath(NetworkObject deadEnemy)
    {
        if (deadEnemy.TryGetComponent<EnemyController>(out var script))
            script.OnDied -= HandleEnemyDeath;

        DecreaseEnemyCount();
    }

    private void HandleEnemyDeathNoArg()
    {
        DecreaseEnemyCount();
    }

    private void DecreaseEnemyCount()
    {
        _aliveEnemyCount--;
        if (_aliveEnemyCount <= 0)
        {
            FinishBattle();
        }
    }

    private void FinishBattle()
    {
        _isZoneCleared = true;
        _isZoneActive = false;

        Debug.Log($"[BattleZone] {gameObject.name} 清理完毕！");

        if (_exitGate != null) _exitGate.SetLocked(false);
        if (_entryGate != null) _entryGate.SetLocked(false);

        ShowZoneClearClientRpc();
    }

    [ClientRpc]
    private void ShowZoneClearClientRpc()
    {
        // UI 表现
    }
}