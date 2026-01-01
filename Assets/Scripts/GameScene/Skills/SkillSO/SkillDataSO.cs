// [FILE START: Assets\Scripts\GameScene\Skills\SkillSO\SkillDataSO.cs]
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skill Data")]
public class SkillDataSO : ScriptableObject
{
    
    [Header("技能基础")]
    public string SkillName;
    public float coolDown = 1.0f;
    public string animationName = "Skill1"; // 攻击动作

    [Header("阶段一，前摇")]
    [Tooltip("蓄力时间（秒），对应预警圈的时间")]
    public float chargeDuration = 1.5f;
    [Tooltip("蓄力动画")]
    public string chargeAnimationName = "Idle";
    [Tooltip("预警圈 Prefab")]
    public GameObject warningPrefab;
    [Tooltip("视觉效果 prefab list")]
    public List<GameObject> chargeVisualPrefabs; // [新增] 视觉预制体列表

    [Header("阶段二，释放")]
    public string skillActiveAnimationName;
    [SerializeReference, SubclassSelector]
    public List<SkillEffect> effects = new List<SkillEffect>();
    public bool isSelfCentered;

    [Header("阶段三，后摇")]
    public string skillRecoveryAnimationName;
    
    public void Cast(GameObject caster, GameObject target, Vector3 position)
    {
        if (caster.TryGetComponent<NetworkBehaviour>(out var networkBehaviour))
        {
            networkBehaviour.StartCoroutine(ExecutionRoutine(caster, target, position));
        }
    }

    private IEnumerator ExecutionRoutine(GameObject caster, GameObject target, Vector3 position)
    {
        foreach (var effect in effects)
        {
            if (effect is DelayEffect delayEffect)
            {
                yield return new WaitForSeconds(delayEffect.duration);
            }
            effect.Execute(caster, target, position);
        }
    }
}
// [FILE END]