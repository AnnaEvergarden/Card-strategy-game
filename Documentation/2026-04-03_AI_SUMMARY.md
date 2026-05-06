# AI 上下文总结 (2026-04-03)

> 供新对话恢复上下文：架构、已讨论/已修改内容、技术决策、待办。  
> **自上次总结（2026-03-17）至本日**的汇总（含多轮对话中的设计与答疑）。

## 项目概述

- **类型**：Unity 卡牌策略游戏（C#，团结/Unity 2022.x）
- **解决方案**：`卡牌策略游戏.sln`

## 本轮主题（相对 2026-03-17）

1. **卡牌效果与策略模式**：用 `CardEffectConfig` + 工厂/策略执行；`Target` 放在 **`CardEffectContext`**（施法者、目标、阵营、槽位、战斗引用），策略只消费上下文。
2. **`EffectTargetSideRule` vs `EffectTargetMode`**：前者解析**目标阵营**（敌/友/己侧）；后者在现有工程里承担**单体/群体/自身**等模式；`Ally` 与 `Self` 在“阵营解析”上可能相同，差异在“最终指向谁”（自身应强制 `caster`）。
3. **`CardData.effects` vs `CardUnit.attachedEffects`**：前者为**静态配置**（ScriptableObject）；后者为**运行时持续效果**（Buff/回合触发等）。
4. **`ExcuteOneConfig`（拼写为项目约定）**：`BattleManager` 内按单条 `CardEffectConfig` 解析目标并调用 `ICardEffect.Excute`。
5. **换牌 UI 拆分**：将战斗内换牌从 `CardSelectPanel` 抽到独立 **`CardSwitchPanel`**；`BindBench(unit, OnClickBenchCard)` 传**方法/委托**，禁止写成 `OnClickBenchCard(unit)`（会在绑定时立即执行）。
6. **`UIPanelManager` 栈行为**：`PushPanel` 后若立刻 `Back()` 会**刚入栈即弹出**；`Back()` 需**隐藏当前面板**再显示上一面板，否则出现“栈变了但界面仍显示”。`ShowPanel` 会 `Clear` 栈并只保留当前名（与多层 Push 混用需注意）。
7. **技能按钮第一次无反应**：`CardActionPanel.OnClickSkillBtn` 在 `caster == null` 时**静默 return**，若未先 `BindContext` 会表现为第一次无日志；第二次绑定后才出现“释放失败”等日志。
8. **投降与结算**：推荐 **`FinishBattle(winner, reason)`** 统一出口，使用已有/扩展的 **`BattleEndReason`**，**不要**靠把己方全打死再走 `HasAllDead`；结算 UI 在结束事件中打开（示例中为 `UIPanelNames.Settlement` + `SettlementPanel.BindResult`）。
9. **Cursor 规则**：新增 **`read-before-code-example-or-edit.mdc`**（`alwaysApply: true`）：给代码示例或改代码前须先读相关现有代码。

## 已修改文件（仓库内可核对）

| 路径 | 说明 |
|------|------|
| `.cursor/rules/read-before-code-example-or-edit.mdc` | 新增：先读代码再给方案 |
| `Assets/Scripts/UI/BattleScene/CardActionPanel.cs` | 中文注释/乱码修复；技能/换牌逻辑（与对话迭代一致） |
| `Assets/Scripts/Core/Card/CardEffect/CardEffectContext.cs` | 乱码修复；`CardEffectContext` 非 Mono 的澄清与注释 |
| `Assets/Scripts/Core/Card/CardEffect/CardEffectFactory.cs` | 中文注释 |
| `Assets/Scripts/Core/Card/CardEffect/AttachedEffectRuntime.cs` | 中文注释 |
| `Assets/Scripts/Core/Card/CardEffect/HealEffect.cs` | 乱码修复；治疗目标改为 `target`（原误用 `caster`） |
| `Assets/Scripts/Test/TestBattleScene.cs` | 中文注释乱码修复 |
| `Assets/Scripts/Core/Battle/CardUnit.cs` | 附加效果相关方法中文注释 |
| `Assets/Scripts/Core/Battle/BattleManager.cs` | 技能区等方法中文注释（若工程内已有 `BattleEndReason` 等，以实际文件为准） |
| `Assets/Scripts/UI/BaseUIPanel.cs` | `UIPanelNames.CardSwitch` 等常量（若已合并） |

> 说明：对话中部分 **完整示例**（如独立 `SettlementPanel`、`BattleGameEvents`、大幅重构 `UIPanelManager`）可能仍以用户**手动对比粘贴**为主，**未必已全部合并进仓库**；以当前 Git/工作区文件为准。

## 已知问题 / 技术债（对话中点名）

- **`ExcuteOneConfig` 单体分支**：若存在 `finalSlot` 与 `GetActiveUnit(targetSide, _targetSlot)` 不一致，易导致目标错误（应用 `finalSlot` 取目标）。
- **`ConfirimPLayer1Actives` 拼写**、确认条件括号：仍为历史待办（见 2026-03-17 总结）。
- **`UIPanelManager.useStack`**：为 `false` 时 `PushPanel` 不入栈，调试堆栈时易误解。
- **战斗结束与输入**：`FinishBattle` 后应禁止技能/回合等（示例中用 `IsBattleFinished` 守卫）。

## 待办建议

- 合并并实装：**投降** + **结算面板跳转**（统一 `FinishBattle`，订阅事件或直接在结束处 `ShowPanel(Settlement)`）。
- 修正 `ExcuteOneConfig` 单体目标索引与 `finalSlot` 一致。
- 梳理 `CardSwitchPanel` 与 `CardSelectPanel` 职责，避免 `ShowPanel(Battle)` 与栈混用导致状态错乱。
- 为 `OnClickSkillBtn` 在 `caster == null` 时打明确警告，避免“第一次静默无响应”。

---

*生成时间：2026-04-03。新对话请先读本文件，再按需打开具体脚本。*
