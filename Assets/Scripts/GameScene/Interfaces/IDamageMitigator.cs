// [FILE START: Assets\Scripts\GameScene\Interfaces\IDamageMitigator.cs]
public interface IDamageMitigator
{
    /// <summary>
    /// 开启伤害降低效果
    /// </summary>
    /// <param name="percentage">减伤百分比 (0.0 ~ 1.0)</param>
    /// <param name="duration">持续时间 (秒)</param>
    void ApplyDamageReduction(float percentage, float duration);
}
// [FILE END]
