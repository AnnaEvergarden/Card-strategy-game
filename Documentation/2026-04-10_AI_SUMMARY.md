# AI 上下文总结 (2026-04-10)

> 供新对话恢复上下文：架构、已讨论/已修改内容、技术决策、待办。  
> **自上次总结（2026-04-03）至本日**的汇总。

## 项目概述

- **类型**：Unity 卡牌策略游戏（C#，团结/Unity 2022.x）
- **解决方案**：`卡牌策略游戏.sln`

## 本轮主题（相对 2026-04-03）

本轮为**架构设计与分层通信**的答疑，**未修改仓库代码**。

1. **分层角色（用户既定）**  
   - **Application**：流程编排（战斗流程、出牌流程等）。  
   - **Domain**：数据与规则（数值、扣费、伤害等），尽量纯逻辑。  
   - **Presentation（UI）**：表现；**不直接改 Domain 数据**；操作经 Application 进入 Domain。

2. **核心约束**  
   - Application **不得直接调用 UI**；状态变更后通过 **事件/消息** 或 **只读模型**，由 UI 订阅刷新。  
   - Manager 偏初始化；System/服务偏逻辑处理（与现有工程习惯一致即可）。

3. **推荐结论（卡牌 + 背包规模）**  
   - 保持 **分层 + 结构化事件**，不必强行全项目 MVVM；可对背包/表单类界面**局部** ViewModel。  
   - **事件防乱**：按领域切片命名（如 `Battle.*` / `Inventory.*`）；区分 **事实事件**（已发生、驱动 UI）与 **命令意图**（应由 UI→Application 入口，勿用事件当隐式 API）；payload 小而稳定；单一写入路径；场景级订阅与取消订阅；开发期可打事件日志。

4. **通信边界**  
   - UI → Application：集中用例入口。  
   - Application ↔ Domain：同步调用，Domain 不依赖 Unity UI。  
   - Application → UI：事件或只读快照，不反向依赖 Panel。  
   - 选中/拖拽等**纯表现状态**留 Presentation；**能否打出/费用**以 Domain/Application 为准。

5. **与「Clean / MVVM」的关系**  
   - 当前三层即接近 Clean 的依赖方向；MVVM 适合多绑定、强测试需求的界面，战斗 HUD 可用事件 + 轻量 Presenter。

## 已修改文件（本轮）

- **无**（仅讨论，未提交代码变更）。

## 延续自 2026-04-03 的已知问题 / 技术债

以下仍以仓库实际文件为准；若已修复请在后续总结中删除。

- **`ExcuteOneConfig` 单体分支**：`finalSlot` 与 `GetActiveUnit` 槽位不一致时的目标错误风险。  
- **`ConfirimPLayer1Actives` 拼写**、确认条件括号（历史待办）。  
- **`UIPanelManager.useStack`**：`false` 时 `PushPanel` 不入栈，易误解。  
- **战斗结束与输入**：`FinishBattle` 后需禁止技能/回合等（`IsBattleFinished` 类守卫）。

## 待办建议

**架构落地（可选后续迭代）**

- 为「打出卡牌 → 扣能量 → HUD」等路径定义 **事件契约**（命名空间/程序集或静态类分组）与 **订阅生命周期**（按场景注册/注销）。

**自 2026-04-03 延续**

- 实装投降 + 结算面板跳转（统一 `FinishBattle`）。  
- 修正 `ExcuteOneConfig` 单体目标与 `finalSlot` 一致。  
- 梳理 `CardSwitchPanel` 与 `CardSelectPanel` 职责及 `ShowPanel` 与栈混用。  
- `OnClickSkillBtn` 在 `caster == null` 时明确警告或提示。

---

*生成时间：2026-04-10。新对话请先读本文件，再按需打开具体脚本。*
