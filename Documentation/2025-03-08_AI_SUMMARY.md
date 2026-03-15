# AI 上下文总结 (2025-03-08)

> 供新对话恢复上下文：架构、已做修改、技术决策、待办。  
> **项目改动**、**工作目的**、**代码逐一说明**已拆分为独立文档，便于按需查看：`2025-03-08_项目改动清单.md`、`2025-03-08_工作目的.md`、`2025-03-08_代码改动说明.md`。

## 项目概述

- **类型**：Unity 卡牌策略游戏（C#，Unity 2022.3.61t7 / 团结编辑器）
- **解决方案**：`卡牌策略游戏.sln`，内含 Assembly-CSharp 项目

## 当前架构（类功能摘要）

| 类 | 路径 | 功能 |
|----|------|------|
| **CardData** | Core/Card/CardData.cs | 卡牌静态配置 ScriptableObject；基础属性，PVP/PVE 共用；GetBaseAttack/GetBaseHealth |
| **BattleContext** | Core/Battle/BattleContext.cs | 单局只读上下文：isPVP、PlayerDeck、EnemyDeck（List&lt;CardInstance&gt;） |
| **BattleManager** | Core/Battle/BattleManager.cs | 战斗驱动（极简）：CardStatsService 总战力对比；StartBattle、GetDeckTotalPower、RegisterCardData；日志前缀 `[BattleManager]` |
| **SaveData** | Services/SaveData.cs | 存档结构：playerName、gold、inventory、**decks**（List&lt;DeckSlot&gt;）、**currentDeckIndex**；无参构造时默认卡组 |
| **DeckSlot** | 同文件 | deckName、instanceIds |
| **SaveManager** | Services/SaveManager.cs | 按**账号+槽位**：`Save_{accountId}_{slot}.json`；currentSlotIndex / SelectSlot；GetCurrentDeck 从 current.decks[currentDeckIndex] 取；日志前缀 `[SaveManager]` |
| **AccountEntry / AccountListData** | Services/AccountEntry.cs | 账号条目（userName、accountId、password）；账号列表 |
| **AccountManager** | Services/AccountManager.cs | 本地账号：accounts.json、LoadAccounts、Register、Login、Logout、GetCurrentAccountId、IsLoggedIn；日志前缀 `[AccountManager]` |
| **GameBootstrap** | Services/GameBootstrap.cs | 启动：Awake 取 SaveManager/AccountManager/BattleManager、Load、RegisterCardData；Start 若已登录 SelectSlot(0)；日志前缀 `[GameBootstrap]` |
| **CardInstance / CardStatsService** | 引用，定义未在仓库 | 运行时实例；静态 GetAttack/GetHealth(instance, data, useEnhancement) |

## 技术决策

1. **PVP 忽略强化**：isPVP=true 时 useEnhancement=false，数值经 CardStatsService 统一处理。  
2. **战斗极简版**：暂无单卡出牌、回合制，仅卡组总战力对比。  
3. **存档**：按账号+槽位 JSON；多套卡组 decks + currentDeckIndex；槽位 currentSlotIndex 与卡组下标分离。  
4. **调试日志**：所有 Debug 前加 `[类名]`；中文统一 UTF-8 简体，遵守 code-chinese-format 规则。

## 项目改动清单（增/删/改）摘要

- **03-07**：增 Documentation 总结两份、summary-docs.mdc；无代码改动。  
- **03-08**：增 AccountEntry.cs、AccountManager.cs、code-chinese-format.mdc 及多份总结文档；改 SaveData、SaveManager、GameBootstrap、BattleManager、AccountManager、BattleContext、CardData、summary-docs.mdc。  

完整列表见 **`2025-03-08_项目改动清单.md`**。

## 本轮工作目的摘要

- 03-07：建立总结与上下文恢复规范；只读梳理架构。  
- 03-08：支持多账号+多槽位+多卡组；统一日志与中文；修复槽位与卡组下标混用；修正账号加载与注册逻辑；增强空引用与笔误防护。  

详细说明见 **`2025-03-08_工作目的.md`**。

## 已做修改（技术要点）

- 日志前缀：GameBootstrap、BattleManager、SaveManager、AccountManager 均加 `[类名]`。  
- 中文统一：全脚本注释/Header/Tooltip/字符串/Debug 改为 UTF-8 简体；新增 code-chinese-format.mdc。  
- SaveManager：路径用 currentSlotIndex；SelectSlot 设 currentSlotIndex 再 Load；槽位边界 >= SaveSlotCount；SetCurrentDeckIndex/SetDeckInstanceIds 边界与无效处理。  
- AccountManager：Awake 调 LoadAccounts；IsLoggedIn 改为 !string.IsNullOrEmpty(currentAccountId)；Register 的 Add 移出 foreach。  
- GameBootstrap：引用 AccountManager、登录后 SelectSlot(0)、Start 中 accountManager 空检查。  
- BattleManager：GetDeckTotalPower 方法体 `};` → `{`。  

每处改动的**作用、改动前、改动后、目的**见 **`2025-03-08_代码改动说明.md`**。

## 已知小问题（可选修复）

- GameBootstrap 中 RunTestBattleIfNeeded() 已注释；需验证战斗时可取消注释。  
- CardInstance、CardStatsService 定义仍未在仓库内，若未实现需补全。

## 待办 / 未完成点

- 确认 **CardInstance**、**CardStatsService** 定义位置并纳入版本管理（若未实现则需补全）。  
- 可选：GameBootstrap 中增加 PVP 测试战斗。  
- 后续可扩展：单卡出牌、回合流程、技能与 UI；登录/注册 UI、选存档槽位 UI。

---

*生成时间：2025-03-08；总结与存储流程（含多文档区分）。新对话请先读本文件，再按需查阅 项目改动清单 / 工作目的 / 代码改动说明。*
