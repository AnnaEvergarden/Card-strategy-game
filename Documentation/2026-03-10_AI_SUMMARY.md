# AI 上下文总结 (2026-03-10)

> 供新对话恢复上下文：架构、已做修改、技术决策、待办。  
> **项目改动**、**工作目的**、**代码逐一说明**已拆分为独立文档，便于按需查看：`2026-03-10_项目改动清单.md`、`2026-03-10_工作目的.md`、`2026-03-10_代码改动说明.md`。

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
| **BaseUIPanel** | UI/BaseUIPanel.cs | 所有 UI 面板抽象基类：GetPanelName、RegisterPanel、ShowPanel、HidePanel、OnShow/OnHide；Awake 中注册并隐藏 |
| **UIPanelManager** | Services/UIPanelManager.cs | 单例面板容器：RegisterPanel、ShowPanel(name)、GetPanel；挂载在 UIManager 物体上 |
| **TitlePanel** | UI/TitlePanel.cs | 标题界面：登录→Login 面板、登出、设置（预留）、退出游戏 |
| **LoginPanel** | UI/LoginPanel.cs | 登录界面：账号/密码 TMP_InputField、AccountManager.Login；成功→SaveSelect，取消→Title |
| **SaveSelectPanel** | UI/SaveSelectPanel.cs | 存档选择：container 下槽位 Button 绑定；HasSaveInSlot 后 SelectSlot，LoadScene("Game") |
| **CardInstance / CardStatsService** | 引用，定义未在仓库 | 运行时实例；静态 GetAttack/GetHealth(instance, data, useEnhancement) |

## 技术决策

1. **PVP 忽略强化**：isPVP=true 时 useEnhancement=false，数值经 CardStatsService 统一处理。  
2. **战斗极简版**：暂无单卡出牌、回合制，仅卡组总战力对比。  
3. **存档**：按账号+槽位 JSON；多套卡组 decks + currentDeckIndex；槽位 currentSlotIndex 与卡组下标分离。  
4. **调试日志**：所有 Debug 前加 `[类名]`；中文统一 UTF-8 简体，遵守 code-chinese-format 规则。  
5. **UI 面板**：BaseUIPanel 子类以 gameObject.name 作为面板 ID 向 UIPanelManager 注册；切换时只显示指定名称面板、其余隐藏。

## 项目改动清单（增/删/改）摘要

- **2026-03-10**：增 UI/BaseUIPanel、Services/UIPanelManager、UI/TitlePanel、UI/LoginPanel、UI/SaveSelectPanel；改 Documentation/脚本结构图.md（增加 UI 层与 UI 目录）；增本批总结文档。  

完整列表见 **`2026-03-10_项目改动清单.md`**。

## 本轮工作目的摘要

- 将账号与存档流程串联到 UI，形成标题→登录→选档→进入游戏场景的完整流程。  
- 抽象 BaseUIPanel + UIPanelManager，统一面板注册与显隐，便于后续扩展。  
- 选档逻辑与 SaveManager 一致：先 HasSaveInSlot 再 SelectSlot，仅允许有存档槽位进入游戏。  

详细说明见 **`2026-03-10_工作目的.md`**。

## 已做修改（技术要点）

- **BaseUIPanel**：抽象基类，Awake 注册并隐藏；ShowPanel/HidePanel 用 SetActive；GetPanelName 用 gameObject.name。  
- **UIPanelManager**：单例；RegisterPanel 存字典并 HidePanel；ShowPanel(name) 先全隐藏再显示目标。  
- **TitlePanel**：登录切 Login、登出调 AccountManager.Logout、设置预留、退出 Quit/停播。  
- **LoginPanel**：取输入框调用 Login，成功切 SaveSelect、失败 return，取消切 Title。  
- **SaveSelectPanel**：container 子物体按索引绑定 OnClickSlot(slotIndex)；HasSaveInSlot 为 true 才 SelectSlot 并 LoadScene("Game")。  
- **脚本结构图**：增加 UI 层描述与 UI 目录各脚本说明。  

每处改动的**作用、改动前、改动后、目的**见 **`2026-03-10_代码改动说明.md`**。

## 已知小问题（可选修复）

- GameBootstrap 中 RunTestBattleIfNeeded() 已注释；需验证战斗时可取消注释。  
- CardInstance、CardStatsService 定义仍未在仓库内，若未实现需补全。  
- LoginPanel 等部分日志/注释仍有乱码，需按 code-chinese-format 统一为 UTF-8 简体。  
- SaveSelectPanel 加载场景名为 "Game"，需与 Build Settings 中实际场景名一致（若为 GameScene 需改代码）。

## 待办 / 未完成点

- 确认 **CardInstance**、**CardStatsService** 定义位置并纳入版本管理（若未实现则需补全）。  
- 可选：GameBootstrap 中增加 PVP 测试战斗。  
- 可选：设置面板、登录失败/无存档时的提示 UI。  
- 后续可扩展：单卡出牌、回合流程、技能与 UI；注册账号 UI、选存档槽位 UI 的完善。

---

*生成时间：2026-03-10；总结与存储流程（含多文档区分）。新对话请先读本文件，再按需查阅 项目改动清单 / 工作目的 / 代码改动说明。*
