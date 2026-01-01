using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class ConeLaserEffect : SkillEffect
{
    [Header("激光预制体 (需挂 NetworkObject + LaserVisual)")]
    [SerializeField] private NetworkObject _laserPrefab;

    [Header("范围设置")]
    [Range(0, 180)] public float coneAngle = 60f; // 锥形角度
    public int rayCount = 5; // 射线数量

    [Header("生长设置")]
    public float startLength = 0.5f;
    public float maxLength = 20f;
    public float growthTime = 1.0f; // 达到最长所需时间
    public float totalDuration = 3.0f; // 技能总持续时间

    [Header("战斗参数")]
    public float rayWidth = 0.2f;
    public int damagePerTick = 10;
    public float damageInterval = 0.5f; // 每0.5秒造成一次伤害，防止秒杀

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 启动主协程，管理所有射线的生命周期
        caster.GetComponent<NetworkBehaviour>().StartCoroutine(
            LaserBarrageRoutine(caster)
        );
    }

    private IEnumerator LaserBarrageRoutine(GameObject caster)
    {
        List<NetworkObject> activeLasers = new List<NetworkObject>();
        List<Vector3> laserDirections = new List<Vector3>();

        // 1. 生成所有射线
        for (int i = 0; i < rayCount; i++)
        {
            // --- 数学部分：随机生成锥形内的方向 ---
            // 假设 Boss 面向 transform.forward
            // 我们在 Y 轴上随机偏转 (-angle/2 到 angle/2)
            float randomYAngle = Random.Range(-coneAngle / 2f, coneAngle / 2f);

            // 这是一个平面的扇形。如果你想要 3D 圆锥，还需要在 X 轴微调，这里假设是地面的扇形扫射
            Quaternion rotation = Quaternion.Euler(0, randomYAngle, 0);
            Vector3 finalDir = rotation * caster.transform.forward;

            // --- 生成物体 ---
            // 生成位置：稍微抬高一点，别插在地里
            Vector3 spawnPos = caster.transform.position + Vector3.up * 1.0f;

            // 旋转：让激光物体的 Z 轴朝向 finalDir
            Quaternion laserRot = Quaternion.LookRotation(finalDir);

            var laserInstance = GameObject.Instantiate(_laserPrefab, spawnPos, laserRot);
            laserInstance.Spawn();

            // 存下来用于后续逻辑
            activeLasers.Add(laserInstance);
            laserDirections.Add(finalDir);

            // 初始化客户端视觉
            if (laserInstance.TryGetComponent<LaserVisual>(out var visual))
            {
                visual.InitializeLaserClientRpc(startLength, maxLength, growthTime, rayWidth);
            }
        }

        // 2. 循环检测逻辑 (持续 totalDuration)
        float timer = 0f;

        // 用字典记录每个激光对每个玩家的“上一次伤害时间”
        // Key: Laser的NetworkObjectId + Player的NetworkObjectId (组合Key)
        // Value: 上次受伤时间
        Dictionary<string, float> hitRecords = new Dictionary<string, float>();

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;

            // 计算当前的物理射线长度
            float progress = Mathf.Clamp01(timer / growthTime);
            float currentLogicLen = Mathf.Lerp(startLength, maxLength, progress);

            // 遍历每一条激光进行检测
            for (int i = 0; i < activeLasers.Count; i++)
            {
                var laserObj = activeLasers[i];
                if (laserObj == null || !laserObj.IsSpawned) continue;

                Vector3 origin = laserObj.transform.position;
                Vector3 dir = laserObj.transform.forward; // 因为我们生成时旋转了物体，所以直接用 forward

                // 使用 SphereCast 代替 Raycast，模拟“粗”激光
                // 半径设为 rayWidth的一半
                RaycastHit[] hits = Physics.SphereCastAll(origin, rayWidth / 2f, dir, currentLogicLen);

                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject == caster) continue;

                    if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                    {
                        // --- 伤害频率控制 (Tick Logic) ---
                        ulong targetId = hit.collider.GetComponent<NetworkObject>().NetworkObjectId;
                        ulong laserId = laserObj.NetworkObjectId;
                        string key = $"{laserId}_{targetId}";

                        if (!hitRecords.ContainsKey(key) || (Time.time - hitRecords[key] > damageInterval))
                        {
                            // 造成伤害
                            damageable.TakeDamage(damagePerTick, caster.GetComponent<NetworkObject>().NetworkObjectId);

                            // 记录时间
                            hitRecords[key] = Time.time;

                            // 还可以加个击退？
                            // if (hit.collider.TryGetComponent<IKnockBackable>(out var kb)) ...
                        }
                    }
                }
            }

            yield return null; // 等待下一帧
        }

        // 3. 清理
        foreach (var laser in activeLasers)
        {
            if (laser != null && laser.IsSpawned)
            {
                laser.Despawn();
            }
        }
    }
}