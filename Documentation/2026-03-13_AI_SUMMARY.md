# AI 上下文总结 (2026-03-13)

> 供新对话恢复上下文：架构、已做修改、技术决策、待办。  
> 自上次总结（2026-03-12）至本日的改动汇总。  
> 拆分文档：`2026-03-13_项目改动清单.md`、`2026-03-13_工作目的.md`、`2026-03-13_代码改动说明.md`。

## 项目概述

- **类型**：Unity 卡牌策略游戏（C#，Unity 2022.3.61t7 / 团结编辑器）
- **解决方案**：`卡牌策略游戏.sln`，内含 Assembly-CSharp 项目

## 当前架构（类功能摘要，含本轮新增/变更）

| 类 | 路径 | 功能 |
|----|------|------|
| **CardData** | Core/Card/CardData.cs | 卡牌静态配置 ScriptableObject；基础攻/血/费、名称、描述等 |
| **CardInstance** | 引用 | 卡组/背包中的卡牌实例：cardId、instanceId、强化相关字段 |
| **CardStatsService** | Core/Card/CardStatsService.cs | 静态 GetAttack/GetHealth(instance, data, useEnhancement) |
| **CardEffect** | Core/Card/CardEffect.cs | 抽象基类，技能/效果执行接口 Execute()，本轮新增 |
| **CardUnit** | Core/Battle/CardUnit.cs | **本轮新增**。战斗用单位：由 CardInstance + BattleContext 构造，持 currentHP/currentAttack、hasActedThisTurn、effects；TakeDamage/Heal；通过 CardDataBase 取 CardData |
| **BattleContext** | Core/Battle/BattleContext.cs | 单局只读上下文：isPVP、**Player1Deck**、**Player2Deck**（命名由 PlayerDeck/EnemyDeck 调整为双方对称） |
| **BattleManager** | Core/Battle/BattleManager.cs | **大幅扩展**：继承 Singleton；BattleSide、turnCount；player1Units/player2Units、player1Active/player2Active（各 2 张）；StartBattle(ctx)、CreateUnitsFromDeck、SelectActiveUnitsForSide、EndCurrentSideTurn(auto)、IsAllActiveUnitsActedThisTurn、GetActiveUnit、CheckBattleEnd；卡牌数据改为通过 CardDataBase 查询 |
| **BattleFieldView** | Core/Battle/BattleFieldView.cs | **本轮新增**。Singleton；根据双方 CardUnit 列表 SetUpCards，Instantiate 卡牌 Prefab 并 Bind(unit) 到 CardUI；RefreshAllCards、ClearCards |
| **CardDataBase** | Services/CardDataBase.cs | **本轮新增**。Singleton；卡牌配置表 cardId→CardData，GetCardData、RegisterCardData；从 BattleManager 抽离的查表职责 |
| **UIPanelManager** | Services/UIPanelManager.cs | **扩展**：新增面板栈 panelStack、useStack；PushPanel（压栈显示，不 HideAllPanels）、Back（弹栈显示上一面板）、ReplacePanel、ClearStackAndShow；GetPanel、StackCount |
| **CardUI** | Core/Card/CardUI.cs | **本轮新增**。挂在卡牌 Prefab 上：Bind(CardUnit)、RefreshDisplay；IPointerClickHandler 点击时 PushPanel 打开 CardActionPanel |
| **CardActionPanel** | UI/CardActionPanel.cs | **本轮新增**。继承 BaseUIPanel；卡牌操作面板（释放技能、切换卡牌等），当前为空壳，待接逻辑 |
| **BattlePanel** | UI/BattlePanel.cs | **本轮新增**。继承 BaseUIPanel；结束回合、卡 1/卡 2 按钮；EndCurrentSideTurn(auto: false)；卡牌按钮弹出操作界面待接 |
| **TitleSceneManager** | Services/SceneManagers/TitleSceneManager.cs | **本轮新增**。标题场景入口，Start 时 ShowPanel(UIPanelNames.Title) |
| **SaveManager / AccountManager / SaveData / AccountEntry / GameBootstrap** | 同前 | 逻辑延续 2026-03-12；SaveManager 槽位与账号存档；AccountManager 注册/登录 |
| **Singleton / BaseUIPanel / UIPanelNames / TitlePanel / LoginPanel / SaveSelectPanel** | 同前 | 延续 2026-03-12 |

## 技术决策（本轮）

1. **卡牌配置查表独立**：CardDataBase 单例负责 cardId→CardData，BattleManager、CardUnit 等只调用 GetCardData，不再在 BattleManager 内维护卡表。
2. **战斗单位与表现分离**：CardUnit 纯数据/逻辑（当前血攻、本回合是否已行动）；CardUI 挂在 Prefab 上负责显示与点击，通过 Bind(unit) 与 CardUnit 一一对应。
3. **面板栈不隐藏底层**：PushPanel/Back 只对栈顶面板做 Show/Hide，不调用 HideAllPanels，实现“小面板叠在当前面板上”的弹窗效果。
4. **回合制流程**：双方各 2 张上场卡；EndCurrentSideTurn 切换行动方；双方都结束则 turnCount++ 并 ResetUnitsTurnFlags；IsAllActiveUnitsActedThisTurn 用于判断是否可自动结束或提示。

## 项目改动清单（增/删/改）摘要

- **增**：`Assets/Scripts/Core/Battle/CardUnit.cs`、`Assets/Scripts/Core/Battle/BattleFieldView.cs`、`Assets/Scripts/Core/Card/CardUI.cs`、`Assets/Scripts/Core/Card/CardEffect.cs`、`Assets/Scripts/Services/CardDataBase.cs`、`Assets/Scripts/UI/CardActionPanel.cs`、`Assets/Scripts/UI/BattlePanel.cs`、`Assets/Scripts/Services/SceneManagers/TitleSceneManager.cs`
- **改**：`Assets/Scripts/Services/UIPanelManager.cs`（栈：PushPanel、Back、ReplacePanel、ClearStackAndShow，不 HideAllPanels）、`Assets/Scripts/Core/Battle/BattleManager.cs`（回合制、CardUnit、CardDataBase、StartBattle、EndCurrentSideTurn 等）、`Assets/Scripts/Core/Battle/BattleContext.cs`（Player1Deck/Player2Deck）
- **删**：无

完整列表见 **`2026-03-13_项目改动清单.md`**。

## 本轮工作目的摘要

- 实现**回合制对战框架**：双方各上场 2 张卡，按行动方回合、结束回合切换，双方行动完后下一回合。
- 实现**战斗用单位 CardUnit** 与 **CardUI 绑定**：卡牌 Prefab 生成时 Bind(CardUnit)，点击卡牌弹出操作面板。
- **面板栈**：支持弹窗式小面板叠在当前面板上，返回时只关栈顶。
- **卡牌配置服务**：CardDataBase 独立，便于多处复用与维护。

详细说明见 **`2026-03-13_工作目的.md`**。

## 已做修改（技术要点）

- **CardUnit**：由 CardInstance + BattleContext 构造；从 CardDataBase 取 CardData，用 CardStatsService 算当前攻血；hasActedThisTurn、TakeDamage、Heal、effects 列表。
- **BattleManager**：StartBattle 根据 BattleContext 创建双方 CardUnit 列表，SelectActiveUnitsForSide 取前 2 张上场，调用 BattleFieldView.SetUpCards；EndCurrentSideTurn 切换 currentSide，双方都结束则回合数+1 并重置行动标记；IsAllActiveUnitsActedThisTurn、GetActiveUnit、CheckBattleEnd（待接结算/通知）。
- **UIPanelManager**：panelStack + useStack；PushPanel 仅 Show 新面板不隐藏其余；Back 弹栈后 Show 新栈顶；ReplacePanel、ClearStackAndShow。
- **CardUI**：Bind(CardUnit)、RefreshDisplay；OnPointerClick 时 GetPanel(CardActionPanel) 并 PushPanel。
- **BattleFieldView**：SetUpCards 遍历双方 CardUnit 列表，Instantiate cardPrefab，GetComponent<CardUI>().Bind(unit)；RefreshAllCards、ClearCards。
- **CardDataBase**：Awake 构建 cardId→CardData 字典，GetCardData、RegisterCardData。
- **BattleContext**：PlayerDeck/EnemyDeck 更名为 Player1Deck/Player2Deck。
- **CardActionPanel**：占位，待接释放技能、切换卡牌等逻辑。
- **BattlePanel**：结束回合按钮调 EndCurrentSideTurn(false)；卡 1/卡 2 按钮待接弹出操作面板并传入对应 CardUnit。

每处改动的**作用、改动前、改动后、目的**见 **`2026-03-13_代码改动说明.md`**。

## 已知小问题 / 待完善

- BattleFieldView.SetUpCards 内 InstantiatePrefabs 循环条件与判空：当前为 `i < _cardUIs.Count` 且 `if (unit != null) continue`，应为遍历 _playerUnits 且 unit 为 null 时 continue，需按实际代码核对修正。
- CardUI.RefreshDisplay 中若 CardData 暴露为 icon（Sprite），需用 icon.sprite = data.icon 或项目内实际字段名，避免编译或显示错误。
- CardActionPanel 未实现 ShowFor(CardUnit)、释放技能/切换卡牌按钮逻辑。
- BattlePanel 卡 1/卡 2 按钮未与 CardActionPanel 及对应 CardUnit 联动。
- CheckBattleEnd 未在回合结束或单位死亡时主动调用并通知 UI/流程。

## 待办 / 未完成点

- 修正 BattleFieldView 中 SetUpCards/InstantiatePrefabs 的循环与判空逻辑，确保按 _player1Units/_player2Units 正确生成并绑定 CardUI。
- 实现 CardActionPanel：ShowFor(CardUnit)、释放技能、切换卡牌等按钮与 BattleManager/CardUnit 联动。
- BattlePanel 卡 1/卡 2 点击时打开 CardActionPanel 并传入当前方对应 CardUnit。
- 在 EndCurrentSideTurn 或单位死亡后调用 CheckBattleEnd，并接入胜负结算与 UI 提示。
- 更新 Documentation/脚本结构图.md，加入 CardUnit、CardUI、BattleFieldView、CardDataBase、CardActionPanel、BattlePanel、TitleSceneManager、CardEffect 及 UIPanelManager 栈相关说明。

---

*生成时间：2026-03-13。新对话请先读本文件，再按需查阅项目改动清单 / 工作目的 / 代码改动说明。*
