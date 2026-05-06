# AI 上下文总结 (2026-03-17)

> 供新对话恢复上下文：架构、已做修改、技术决策、待办。  
> 自上次总结（**2026-03-13**）至本日的改动汇总。  
> 拆分文档：`2026-03-17_项目改动清单.md`、`2026-03-17_工作目的.md`、`2026-03-17_代码改动说明.md`。

## 项目概述

- **类型**：Unity 卡牌策略游戏（C#，团结/Unity 2022.x）
- **解决方案**：`卡牌策略游戏.sln`

## 当前架构要点（相对 2026-03-13 的新增/强化）

| 类/模块 | 路径 | 功能（本轮相关） |
|---------|------|------------------|
| **BattleManager** | Core/Battle/BattleManager.cs | `StartBattlePrepare` → 打开选卡；`ConfirimPLayer1Actives` → 写上场槽 + 敌方自动上场 + `StartBattle`；`GetPlayerActiveArray`；`SwitchActiveWithBench` + `BuildListFromActive` 刷新战场 |
| **CardSelectPanel** | UI/BattleScene/CardSelectPanel.cs | 准备阶段选两张；`ShowSwitchMode` 只显示替补，`BindBench` 点选切换 |
| **CardSelectItemView** | UI/BattleScene/CardSelectItemView.cs | `Bind` / `BindBench`；列表项 UI |
| **BaseUIPanel** | UI/BaseUIPanel.cs | 可序列化 `UIPanelManager`，`protected UIPanels`（可回退 `Instance`） |
| **CardUI** | Core/Card/CardUI.cs | `SetUIPanelManager`，点击用注入引用 |
| **BattleFieldView** | Core/Battle/BattleFieldView.cs | 向 `CardUI` 注入 `UIPanelManager`；`ClearContainer` 清理 |
| **CardDataBase** | Services/CardDataBase.cs | 查表；日志前缀 `[CardDataBase]` |
| **CardData** | Core/Card/CardData.cs | `icon` 为 `Sprite` |
| **文档注释** | 多脚本 | 类/方法中文 `/// <summary>`；多文件 UTF-8 重写去乱码 |
| **Cursor Rules** | .cursor/rules/ | `answer-only-no-edit.mdc`、`design-structure-first.mdc` |

## 技术决策（本轮）

1. **准备阶段与正式开战分离**：先 `CardUnit` 全卡组，再由玩家索引确认 `player1Active`，最后 `SetUpCards(上场数组)`。
2. **替补切换**：`GetBenchUnits` 用引用相等判断「不在上场槽」；切换后 `SwitchActiveWithBench` 重建传给 `BattleFieldView` 的列表。
3. **UI 与单例**：优先 **SerializeField 注入** + **属性回退 `Instance`**，避免一次性大改 DI 框架。
4. **编码**：脚本与注释统一 **UTF-8 简体中文**，避免乱码影响协作与 AI 上下文。
5. **规则**：「仅回答」与「结构/设计优先」写入 `.cursor/rules/`，长期约束行为。

## 项目改动清单（摘要）

- **增**：两条 Cursor 规则；`Documentation/2026-03-17_*` 四份本轮文档。
- **改**：大量脚本的 XML 注释与 UTF-8；战斗准备/选卡/换将/注入相关脚本；`脚本结构图.md` 说明行。
- **删**：无。

完整列表见 **`2026-03-17_项目改动清单.md`**。

## 已知问题 / 待完善

- **`ConfirimPLayer1Actives` 拼写**：方法名仍为笔误，建议重命名为 `ConfirmPlayer1Actives` 并全局替换引用。
- **玩家确认条件**：`player1Active[0] == null && player1Active[1] == null || ...` 运算符优先级可能导致与预期不符，需加括号或拆条件明确「必须两张有效且不同」。
- **`BattleManager` 仍直连** `UIPanelManager.Instance`、`BattleFieldView.Instance`：战斗核心与 UI/视图仍可进一步注入或门面封装。
- **`CardUnit` 仍用** `CardDataBase.Instance`：可改为工厂/接口注入以便测试。
- **`脚本结构图.md` 表格**：建议补充 `CardSelectPanel`、`CardSelectItemView` 及 `UI/BattleScene` 目录说明（与 TitleScene 并列）。
- **部分文件若仍见乱码**：用编辑器将文件存为 **UTF-8（无 BOM 或带 BOM 与团队约定一致）** 再保存。

## 待办建议

- 修正 `ConfirimPLayer1Actives` 命名与确认逻辑括号。
- 更新 `脚本结构图.md` 中 UI 分层表，加入战斗选卡相关脚本与目录。
- `CardActionPanel` 的「切换卡牌」按钮调用 `CardSelectPanel.ShowSwitchMode` 并传入当前 `BattleSide` 与槽位索引。
- 逐步去掉 `UIPanels`/`Battle` 的 `Instance` 回退：场景统一拖引用。

---

*生成时间：2026-03-17。新对话请先读本文件，再按需查阅项目改动清单 / 工作目的 / 代码改动说明。*
