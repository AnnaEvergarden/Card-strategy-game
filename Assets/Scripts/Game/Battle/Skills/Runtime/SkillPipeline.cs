using System.Collections.Generic;

/// <summary>
/// 技能流水线：按配置顺序执行 Effect 列表；遇失败则中止并按相反顺序回滚已生效 Effect。
/// </summary>
public static class SkillPipeline
{
    #region Fields

    /// <summary>
    /// 单次执行期间已登记的回滚（复用缓冲，避免每次分配）。
    /// </summary>
    private static readonly List<ISkillEffectRollback> RollbackBuffer = new(8);

    #endregion

    #region Public API

    /// <summary>
    /// 顺序执行技能绑定的全部 Effect。
    /// </summary>
    /// <returns>是否全部 Effect 执行成功。</returns>
    public static bool TryExecute(SkillExecutionContext context, IReadOnlyList<ISkillEffect> effects)
    {
        RollbackBuffer.Clear();
        if (context == null || effects == null || effects.Count == 0)
        {
            return context != null;
        }

        var battle = context.Battle;
        try
        {
            if (battle != null)
            {
                battle.ActiveSkillExecution = context;
            }

            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                if (!effect.TryExecute(context, out var rollback))
                {
                    RollbackAll();
                    context.DiscardPendingEvents();
                    return false;
                }

                if (rollback != null)
                {
                    RollbackBuffer.Add(rollback);
                }
            }

            context.FlushPendingEvents();
            RollbackBuffer.Clear();
            return true;
        }
        finally
        {
            if (battle != null)
            {
                battle.ActiveSkillExecution = null;
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 按相反顺序回滚已生效 Effect。
    /// </summary>
    private static void RollbackAll()
    {
        for (var i = RollbackBuffer.Count - 1; i >= 0; i--)
        {
            RollbackBuffer[i]?.Rollback();
        }

        RollbackBuffer.Clear();
    }

    #endregion
}
