using System;
using Unity.Netcode;
using UnityEngine;

public class BossStateCharge : BossBaseState
{
    private float _timer;
    private float _duration;

    public BossStateCharge(BossController controller, BossStateMachine sm) : base(controller, sm) { }

    public override void Enter()
    {
        var skillData = _controller.CurrentSkillData;
        string animName = skillData != null && !string.IsNullOrEmpty(skillData.chargeAnimationName)
            ? skillData.chargeAnimationName
            : "Idle";

        // [表现] 播放蓄力动画
        _view.PlayAnimation(animName);

        // [逻辑 - Server]
        if (_controller.IsServer)
        {
            _timer = 0f;
            _duration = skillData != null ? skillData.chargeDuration : 0f;
            if (_controller.Agent.isOnNavMesh) _controller.Agent.ResetPath();

            // --- 核心修复：计算位置并调用 Controller 的方法 ---
            if (skillData != null)
            {
                // 1. 计算特效生成位置
                Vector3 spawnPos = _controller.transform.position; // 默认自身中心

                // 如果技能不是以自身为中心，且有目标，则在目标脚下生成
                if (!skillData.isSelfCentered && _controller.Target != null)
                {
                    spawnPos = _controller.Target.transform.position;
                }

                // 2. 触发 RPC
                // 注意：这里不需要判空 prefabs，因为 RPC 内部会去检查，
                // 这样服务端逻辑更简洁，只负责“发起指令”
                _controller.TriggerChargeVisuals(spawnPos, _duration);
            }
        }
    }

    public override void Update()
    {
        // [通用] 可以在这里处理蓄力特效的Update (比如光圈变大)

        // [逻辑 - Server]
        if (_controller.IsServer)
        {
            _timer += Time.deltaTime;
            _controller.RotateTowardsTarget();

            if (_timer >= _duration)
            {
                // 蓄力结束，进入释放状态
                _controller.SetState(BossController.BossMotionState.Skill);
            }
        }
    }

    // [新增] 专门用于生成纯视觉蓄力特效的 RPC
    [ClientRpc]
    private void SpawnChargeVisualsClientRpc()
    {   // 参数传入 蓄力视觉效果预制体列表 List<GameObject>
        // 生成物体
        // 生成的物体自动管理生命周期
        // 参数可能还需要生成位置，以及蓄力时间来控制自己的成长速度
    }
}

//// [FILE START: Assets\Scripts\GameScene\Enemy\Boss\BossState\BossStateCharge.cs]
//using UnityEngine;
//using static BossController;

//public class BossStateCharge : BossBaseState
//{
//    private float timer;
//    public BossStateCharge(BossController bossController, BossPresentation view) : base(bossController, view)
//    {
//    }
//    public override void Enter()
//    {
//        // 客户端逻辑：播放蓄力动画（不管是Server还是Client，只要本地有View都需要播）
//        if (base._view != null)
//        {
//            //base._view.Animator.CrossFade(base._bossController.CurrentChargeAnimName, 0.1f);
//            // 生成蓄力特效
//            //base._bossController.SpawnChargeVFX();
//        }

//        // 服务端逻辑：重置计时器
//        if (base._bossController.IsServer)
//        {
//            timer = 0f;
//        }
//    }

//    public override void Update()
//    {
//        // --- 逻辑分流 ---

//        // 服务端职责：处理倒计时、状态切换、数据计算
//        if (base._bossController.IsServer)
//        {
//            timer += Time.deltaTime;
//            // 始终面向玩家
//            //base._bossController.RotateToTarget();

//            //if (timer >= base._bossController.CurrentSkillData.chargeDuration)
//            //{
//            //    // 时间到，切入 Active 状态
//            //    base._bossController.StateMachine.ChangeState(BossMotionState.SkillActive);
//            //}
//        }

//        // 客户端职责：如果有需要每帧更新的视觉效果（比如红圈跟随变色、聚气粒子增强）
//        if (base._bossController.IsClient)
//        {
//            // 这里处理纯视觉的每帧更新
//        }
//    }
//}
//// [FILE END]