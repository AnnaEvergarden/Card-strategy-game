# 用户向总结 (2026-04-03)

> 本轮对话在做什么、你可以直接沿用的结论、还没做完的事。

## 本轮你主要在推进什么

- **卡牌效果怎么做**：配置 + 策略/工厂；目标放在 **`CardEffectContext`** 里，而不是散在策略构造参数里。
- **静态效果 vs 场上 Buff**：`CardData` 里配的是模板；真正挂在单位上、回合触发的是 **`CardUnit` 上的附加列表**。
- **换牌界面**：从选将面板拆出 **`CardSwitchPanel`**；列表点击用 **`view.BindBench(unit, OnClickBenchCard)`**，不要写成 `OnClickBenchCard(unit)`。
- **UI 栈调试**：换牌后进栈又立刻 `Back` 会看不到面板名；`Back` 要会**关掉当前面板**，否则界面不关。
- **技能第一次没反应**：多半是 **`caster` 还没绑定**就点了技能（代码里 `caster == null` 直接 return，没有提示）。
- **投降**：用**单独结束原因**（如投降），不要靠把自己卡全打死来判负；以后接动画、结算都在**统一结束入口**里接。
- **结算界面**：战斗结束里调 `ShowPanel(结算)` 并给结算面板传胜负/原因（示例里用 `SettlementPanel`）。

## 仓库里已做过的实事（若你已同步）

- 修了 **`CardActionPanel`、`CardEffectContext`、`HealEffect`、`TestBattleScene`** 等中文乱码或注释问题。
- **`HealEffect`** 治疗对象应对 **`target`**。
- 新增 Cursor 规则：**改代码/给示例前先读现有代码**（`.cursor/rules/read-before-code-example-or-edit.mdc`）。

## 建议你接下来自己做（若还没做）

1. 实装 **投降按钮** → `BattleManager.Surrender...` → **打开结算界面**。
2. 检查 **`ExcuteOneConfig`** 里单体目标是否用错了槽位变量。
3. 统一 **`UIPanelManager` 的 Push/Back/ShowPanel** 使用方式，避免栈与界面不同步。
4. 技能按钮在 **`caster == null`** 时给出明确提示，避免误以为“按钮坏了”。

## 和上一份总结（2026-03-17）的关系

- 2026-03-17 里提到的选卡、换将、注入等**仍然有效**。
- 本轮在之上叠加了 **效果系统抽象、换牌面板拆分、UI 栈行为、投降/结算设计** 与 **编码/规则** 约束。

---

*生成时间：2026-04-03。*
