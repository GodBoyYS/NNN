using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skill Data")]
public class SkillDataSO : ScriptableObject
{
    [Header("技能基础配置")]
    public float coolDown = 1.0f;

    [Tooltip("施法距离/攻击半径：玩家进入此范围内时，敌人将停止移动并开始攻击")]
    public float castRadius = 2.0f; // 新增属性：决定了是近战(如2.0)还是远程(如15.0)

    [Header("前摇阶段")]
    [Tooltip("前摇时间（秒），对应预警圈时间")]
    public float chargeDuration = 1.5f;
    [Tooltip("前摇动作名称")]
    public string chargeAnimationName = "Idle";
    [Tooltip("预警圈 Prefab")]
    public GameObject warningPrefab;
    [Tooltip("蓄力视觉特效 prefab list")]
    public List<GameObject> chargeVisualPrefabs;

    [Header("释放阶段")]
    public string skillActiveAnimationName;
    [SerializeReference, SubclassSelector]
    public List<SkillEffect> effects = new List<SkillEffect>();
    public bool isSelfCentered;

    [Header("后摇阶段")]
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