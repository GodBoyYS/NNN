using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class DynamicLaserEffect : SkillEffect
{
    [Header("激光预制体 (需挂 NetworkObject + NetworkTransform + LaserVisual)")]
    [SerializeField] private NetworkObject _laserPrefab;

    [Header("范围设置")]
    [Range(0, 180)] public float baseConeAngle = 60f; // 基础扇形范围
    public int rayCount = 5;

    [Header("动态扫射设置 (Snake Motion)")]
    public float sweepSpeed = 2.0f; // 扫射速度
    public float sweepAmplitude = 30f; // 扫射幅度(角度)
    public bool syncRotationWithBoss = true; // 是否跟随Boss身体转向

    [Header("生长设置")]
    public float startLength = 0.5f;
    public float maxLength = 20f;
    public float growthTime = 1.0f;
    public float totalDuration = 5.0f;

    [Header("战斗参数")]
    public float rayWidth = 0.5f;
    public int damagePerTick = 10;
    public float damageInterval = 0.5f;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        caster.GetComponent<NetworkBehaviour>().StartCoroutine(
            LaserBarrageRoutine(caster)
        );
    }

    private IEnumerator LaserBarrageRoutine(GameObject caster)
    {
        List<NetworkObject> activeLasers = new List<NetworkObject>();
        // 存储每条激光的“基础偏移角”，比如第1条是-30度，第2条是-15度...
        List<float> initialYOffsets = new List<float>();
        // 存储每条激光的随机种子，让它们扫射的频率不一样，看起来更乱
        List<float> noiseSeeds = new List<float>();

        // 1. 生成 (Spawn Phase)
        for (int i = 0; i < rayCount; i++)
        {
            // 计算均匀分布的角度 (或者随机分布)
            // 这里用均匀分布看起来更有规律，然后通过动态扫射打乱
            float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
            float baseAngle = Mathf.Lerp(-baseConeAngle / 2f, baseConeAngle / 2f, t);

            // 初始位置
            Vector3 spawnPos = caster.transform.position + Vector3.up * 1.0f;
            Quaternion spawnRot = caster.transform.rotation * Quaternion.Euler(0, baseAngle, 0);

            var laserInstance = GameObject.Instantiate(_laserPrefab, spawnPos, spawnRot);
            laserInstance.Spawn();

            activeLasers.Add(laserInstance);
            initialYOffsets.Add(baseAngle);
            noiseSeeds.Add(Random.Range(0f, 100f)); // 随机种子

            // 初始化视觉
            if (laserInstance.TryGetComponent<LaserVisual>(out var visual))
            {
                visual.InitializeLaserClientRpc(startLength, maxLength, growthTime, rayWidth);
            }
        }

        float timer = 0f;
        Dictionary<string, float> hitRecords = new Dictionary<string, float>();

        // 2. 循环更新 (Update Phase)
        while (timer < totalDuration)
        {
            if (caster == null) break; // Boss死了就停止

            timer += Time.deltaTime;
            float growProgress = Mathf.Clamp01(timer / growthTime);
            float currentLogicLen = Mathf.Lerp(startLength, maxLength, growProgress);

            // 获取 Boss 当前的状态
            Vector3 bossCenter = caster.transform.position + Vector3.up * 1.0f;
            Quaternion bossForwardRot = syncRotationWithBoss ? caster.transform.rotation : Quaternion.identity;

            for (int i = 0; i < activeLasers.Count; i++)
            {
                var laserObj = activeLasers[i];
                if (laserObj == null || !laserObj.IsSpawned) continue;

                // === A. 核心修改：每帧更新位置跟随 Boss ===
                laserObj.transform.position = bossCenter;

                // === B. 核心修改：计算“蛇形”旋转 ===
                // 1. 基础角度
                float baseOffset = initialYOffsets[i];

                // 2. 动态扫射 (使用 PerlinNoise 产生平滑的随机摆动)
                // Time.time * sweepSpeed: 时间驱动
                // noiseSeeds[i]: 让每条线摆动的不一样
                float noiseVal = Mathf.PerlinNoise(Time.time * sweepSpeed, noiseSeeds[i]);
                // PerlinNoise 返回 0~1，我们需要映射到 -1~1
                float sweepOffset = (noiseVal - 0.5f) * 2f * sweepAmplitude;

                // 3. 合成最终角度
                // 最终旋转 = Boss朝向 * 基础偏移 * 扫射偏移
                Quaternion targetRot = bossForwardRot * Quaternion.Euler(0, baseOffset + sweepOffset, 0);

                laserObj.transform.rotation = targetRot;

                // === C. 伤害检测 (逻辑不变) ===
                Vector3 dir = laserObj.transform.forward;
                RaycastHit[] hits = Physics.SphereCastAll(bossCenter, rayWidth / 2f, dir, currentLogicLen);

                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject == caster) continue;

                    // 忽略其它激光
                    if (activeLasers.Contains(hit.collider.GetComponent<NetworkObject>())) continue;

                    if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                    {
                        ulong targetId = hit.collider.GetComponent<NetworkObject>().NetworkObjectId;
                        string key = $"{laserObj.NetworkObjectId}_{targetId}";

                        if (!hitRecords.ContainsKey(key) || (Time.time - hitRecords[key] > damageInterval))
                        {
                            damageable.TakeDamage(damagePerTick, caster.GetComponent<NetworkObject>().NetworkObjectId);
                            hitRecords[key] = Time.time;
                        }
                    }
                }
            }

            yield return null;
        }

        // 3. 清理
        foreach (var laser in activeLasers)
        {
            if (laser != null && laser.IsSpawned) laser.Despawn();
        }
    }
}