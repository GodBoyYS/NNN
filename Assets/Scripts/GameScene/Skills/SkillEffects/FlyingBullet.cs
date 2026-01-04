using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

[Serializable]
public class FlyingBullet : SkillEffect
{
    [Header("投射物配置")]
    [SerializeField] private NetworkObject _bulletPrefab;
    public float speed = 10f;
    public float maxDistance = 20f;
    public int damage = 15;
    public float radius = 1f;

    [Header("模型修正")]
    [Tooltip("如果箭矢方向不对（例如朝上），尝试设置为 (90, 0, 0) 或 (-90, 0, 0) 以修正朝向")]
    public Vector3 modelRotationOffset = Vector3.zero;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 计算生成位置
        Vector3 spawnPos = caster.transform.position + Vector3.up * 1.5f;

        // 计算飞行方向 (水平)
        Vector3 direction = (position - caster.transform.position).normalized;
        direction.y = 0;

        // 计算旋转 (包含模型修正)
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Quaternion finalRotation = lookRotation * Quaternion.Euler(modelRotationOffset);

        var bulletNetObj = NetworkObjectPool.Instance.GetNetworkObject(_bulletPrefab.gameObject, spawnPos, finalRotation);
        if(bulletNetObj != null && bulletNetObj.TryGetComponent<ProjectileController>(out var projectileController))
        {
            projectileController.Initialize(
                direction, 
                speed, 
                50f,
                damage, 
                radius, 
                caster.GetComponent<NetworkObject>().NetworkObjectId, 
                caster);
        }
    }
}