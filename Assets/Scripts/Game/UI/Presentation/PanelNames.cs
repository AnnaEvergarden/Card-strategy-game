/// <summary>
/// 面板名称常量：统一管理所有面板的字符串 key。
/// </summary>
public static class PanelNames
{
    #region Panel Keys

    /// <summary>
    /// 标题面板 key。
    /// </summary>
    public const string TitlePanel = "TitlePanel";

    /// <summary>
    /// 登录面板 key。
    /// </summary>
    public const string LoginPanel = "LoginPanel";

    /// <summary>
    /// 存档选择面板 key（预留）。
    /// </summary>
    public const string SaveSelectPanel = "SaveSelectPanel";

    /// <summary>
    /// 游戏主界面面板 key。
    /// </summary>
    public const string MainPanel = "MainPanel";

    /// <summary>
    /// 仓库面板 key。
    /// </summary>
    public const string InventoryPanel = "InventoryPanel";

    /// <summary>
    /// 船坞（卡牌仓库）面板 key。
    /// </summary>
    public const string ShipyardPanel = "ShipyardPanel";

    /// <summary>
    /// 舰娘详情面板 key（船坞点击舰娘打开，展示技能等）。
    /// </summary>
    public const string ShipgirlDetailPanel = "ShipgirlDetailPanel";

    /// <summary>
    /// 编队面板 key。
    /// </summary>
    public const string FleetPanel = "FleetPanel";

    /// <summary>
    /// 编队选舰面板 key（从编队界面进入，保存后返回）。
    /// </summary>
    public const string FleetPickPanel = "FleetPickPanel";

    /// <summary>
    /// 建造面板 key。
    /// </summary>
    public const string BuildPanel = "BuildPanel";

    /// <summary>
    /// 领卡展示面板 key。
    /// </summary>
    public const string CardRevealPanel = "CardRevealPanel";

    /// <summary>
    /// 活动面板 key。
    /// </summary>
    public const string ActivityPanel = "ActivityPanel";

    /// <summary>
    /// 选择关卡面板 key（常驻 / 活动关卡入口）。
    /// </summary>
    public const string LevelSelectPanel = "LevelSelectPanel";

    /// <summary>
    /// 区域选择面板 key（常驻/活动共用，按模式读取不同配置）。
    /// </summary>
    public const string LevelAreaSelectPanel = "LevelAreaSelectPanel";

    /// <summary>
    /// 区域内关卡选择面板 key（按区域配置生成关卡按钮）。
    /// </summary>
    public const string LevelStageSelectPanel = "LevelStageSelectPanel";

    /// <summary>
    /// 战斗场景：卡组选择面板 key（进入战斗后默认栈底）。
    /// </summary>
    public const string BattleDeckSelectPanel = "BattleDeckSelectPanel";

    /// <summary>
    /// 战斗场景：首发两名舰娘选择面板 key。
    /// </summary>
    public const string BattleActivePickPanel = "BattleActivePickPanel";

    /// <summary>
    /// 战斗场景：正式对局布局面板 key（敌上我下占位）。
    /// </summary>
    public const string BattleMainPanel = "BattleMainPanel";

    /// <summary>
    /// 战斗场景：舰娘槽位操作小面板 key。
    /// </summary>
    public const string BattleSlotActionMenuPanel = "BattleSlotActionMenuPanel";

    /// <summary>
    /// 战斗场景：技能选择面板 key。
    /// </summary>
    public const string BattleSkillSelectPanel = "BattleSkillSelectPanel";

    /// <summary>
    /// 战斗场景：卡牌切换面板 key。
    /// </summary>
    public const string BattleCardSwitchPanel = "BattleCardSwitchPanel";

    /// <summary>
    /// 战斗场景：表情面板 key（占位）。
    /// </summary>
    public const string BattleEmojiPanel = "BattleEmojiPanel";

    /// <summary>
    /// 战斗场景：结算面板 key。
    /// </summary>
    public const string BattleSettlementPanel = "BattleSettlementPanel";

    #endregion
}

/// <summary>
/// 场景名称常量：统一管理可切换场景名。
/// </summary>
public static class SceneNames
{
    #region Scene Keys

    /// <summary>
    /// 标题场景名。
    /// </summary>
    public const string TitleScene = "TitleScene";

    /// <summary>
    /// 游戏主场景名。
    /// </summary>
    public const string GameScene = "GameScene";

    /// <summary>
    /// 战斗场景名。
    /// </summary>
    public const string BattleScene = "BattleScene";

    #endregion
}

