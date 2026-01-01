// [FILE START: Assets\Scripts\GameScene\Enemy\Boss\BossState\BossStateCharge.cs]
using UnityEngine;
using static BossController;

public class BossStateCharge : BossBaseState
{
    private float timer;
    public BossStateCharge(BossController bossController, BossPresentation view) : base(bossController, view)
    {
    }
    public override void Enter()
    {
        // 客户端逻辑：播放蓄力动画（不管是Server还是Client，只要本地有View都需要播）
        if (base._view != null)
        {
            //base._view.Animator.CrossFade(base._bossController.CurrentChargeAnimName, 0.1f);
            // 生成蓄力特效
            //base._bossController.SpawnChargeVFX();
        }

        // 服务端逻辑：重置计时器
        if (base._bossController.IsServer)
        {
            timer = 0f;
        }
    }

    public override void Update()
    {
        // --- 逻辑分流 ---

        // 服务端职责：处理倒计时、状态切换、数据计算
        if (base._bossController.IsServer)
        {
            timer += Time.deltaTime;
            // 始终面向玩家
            //base._bossController.RotateToTarget();

            //if (timer >= base._bossController.CurrentSkillData.chargeDuration)
            //{
            //    // 时间到，切入 Active 状态
            //    base._bossController.StateMachine.ChangeState(BossMotionState.SkillActive);
            //}
        }

        // 客户端职责：如果有需要每帧更新的视觉效果（比如红圈跟随变色、聚气粒子增强）
        if (base._bossController.IsClient)
        {
            // 这里处理纯视觉的每帧更新
        }
    }
}
// [FILE END]