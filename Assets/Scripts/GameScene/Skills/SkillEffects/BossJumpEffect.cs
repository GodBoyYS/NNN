using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;


[Serializable]
public class BossJumpEffect : SkillEffect
{
    [Header("跳跃参数")]
    public float height = 5.0f;
    public float duration = 1.0f; // 上升/下落的时间
    public bool isLanding = false; // true=下落砸地，false=起跳

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        if (!caster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent)) return;
        // 启动一个独立的移动协程来处理平滑位移
        caster.GetComponent<NetworkBehaviour>().StartCoroutine(
            JumpRoutine(caster.transform, agent, isLanding)
        );
    }

    private IEnumerator JumpRoutine(Transform bossInfo, UnityEngine.AI.NavMeshAgent agent, bool landing)
    {
        float timer = 0f;
        Vector3 startPos = bossInfo.position;
        // 如果是起跳：目标是当前位置上方；如果是下落：目标是地面（简单处理为 y=0 或 raycast 地面）
        Vector3 endPos = landing ? new Vector3(startPos.x, 0, startPos.z) : startPos + Vector3.up * height;

        if (!landing)
        {
            // 起跳前禁用 NavMeshAgent，否则 Transform 甚至动不了
            agent.enabled = false;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            // 使用简单的 Lerp，如果要平滑可以用 AnimationCurve
            bossInfo.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        bossInfo.position = endPos;

        if (landing)
        {
            // 落地后恢复 NavMeshAgent
            agent.enabled = true;
            // 可以在这里防止穿模，重新 Warp 到 NavMesh 上
            if (UnityEngine.AI.NavMesh.SamplePosition(bossInfo.position, out var hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }
}