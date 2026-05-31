# 2026-05-21 USER_SUMMARY（续 2）

## 已完成

1. **Buff**：`IBattleBuffHandler` + `BattleBuffHandlerRegistry`，新增种类只注册处理器，不在 `TickTurnEnd` 堆 if。
2. **回合系统**：`BattleTurnSystem` — 玩家先手、每卡每回合行动一次，换牌/放技能消耗行动；行动后变暗禁点；双方结束后下一回合（Buff Tick + 技能冷却 -1）。
3. **DOTween**：打开操作菜单时槽位 `scaleRoot` 放大（`BattleShipFieldSlotView`）。
4. **敌方回合**：占位自动跳过（`EnemyPhaseRoutine`，待 AI）。

## 编辑器 / 预制体

- `shipFieldSlotPrefab` 建议绑定 **scaleRoot**（一般为槽位根 RectTransform）与 **CanvasGroup**（无则运行时自动加）。
- 可调 `selectedScale`、`actedAlpha`。

## Canvas Root / Layer（说明）

- **Root**：锚点拉伸铺满 Canvas（Stretch 四边 0），不要固定 100×100。
- **Layer**：同样 Stretch 铺满 Root，或按功能区设高度；用于分子树，不是 100×100 小方块。

## 待办

- PvP 先手 50% 抛硬币（`BattleTurnSystem.BeginBattle` TODO）。
- 敌方真实 AI（替换 `AutoPassRemainingEnemyActions`）。
- 回合结束已接 `BattleBuffState.TickTurnEnd` + `TickAllSkillCooldownsEndOfRound`。

## 验证

Play 进战斗 → 点我方舰娘放大并开菜单 → 放技能或换牌 → 该卡变暗 → 另一张行动 → 敌方阶段标题变化 → 进入第 2 回合。
