# 本轮工作小结 (2026-03-13) — 给你自己看

自上次总结（2026-03-12）到本日，主要做了**回合制对战框架**、**战斗单位 CardUnit 与卡牌 UI 绑定**、**面板栈（弹窗式）** 以及 **卡牌配置服务 CardDataBase**，并新增战斗相关 UI 面板与场景入口。

| 文档 | 内容 |
|------|------|
| **2026-03-13_项目改动清单.md** | 本轮的**增/删/改**列表 |
| **2026-03-13_工作目的.md** | 本轮的**工作目的** |
| **2026-03-13_代码改动说明.md** | **代码改动逐一说明**（作用、改动前、改动后、目的） |
| **2026-03-13_AI_SUMMARY.md** | 给 AI 的完整上下文 |

---

## 本轮做了什么（概括）

- **新增**
  - **CardUnit**（Core/Battle）：战斗用单位，由 CardInstance + BattleContext 生成，持当前血攻、本回合是否已行动、TakeDamage/Heal。
  - **CardDataBase**（Services）：单例，卡牌配置表 cardId→CardData，GetCardData/RegisterCardData。
  - **BattleFieldView**（Core/Battle）：单例，根据双方 CardUnit 列表生成卡牌 Prefab 并 Bind 到 CardUI。
  - **CardUI**（Core/Card）：挂在卡牌 Prefab 上，Bind(CardUnit)、刷新显示、点击时 PushPanel 打开卡牌操作面板。
  - **CardEffect**（Core/Card）：技能/效果抽象基类，Execute()。
  - **CardActionPanel**（UI）：卡牌操作面板（释放技能、切换卡牌），当前为空壳。
  - **BattlePanel**（UI）：战斗界面，结束回合、卡 1/卡 2 按钮；结束回合调 BattleManager.EndCurrentSideTurn(false)。
  - **TitleSceneManager**（Services/SceneManagers）：标题场景入口，ShowPanel(Title)。
- **修改**
  - **UIPanelManager**：增加面板栈（panelStack、useStack）；PushPanel 只显示新面板不关其他、Back 弹栈显示上一面板、ReplacePanel、ClearStackAndShow。
  - **BattleManager**：扩展为回合制；BattleSide、turnCount；双方 CardUnit 列表与上场 2 张；StartBattle(ctx)、EndCurrentSideTurn(auto)、IsAllActiveUnitsActedThisTurn、GetActiveUnit、CheckBattleEnd；卡牌数据改为用 CardDataBase。
  - **BattleContext**：PlayerDeck/EnemyDeck 改为 Player1Deck/Player2Deck。
- **删除**：无。

---

## 本轮工作目的

1. **回合制**：双方各 2 张上场卡，玩家方回合可结束回合或等两张卡都释放技能后切换行动方，双方都行动完进入下一回合。
2. **卡牌与 UI 绑定**：战斗开始时用 CardUnit 列表生成卡牌 Prefab，CardUI.Bind(unit)；点击卡牌弹出操作面板（栈式）。
3. **面板栈**：弹窗式小面板叠在当前面板上，返回只关栈顶，不关底层面板。
4. **卡牌配置统一查表**：CardDataBase 独立，BattleManager/CardUnit 等处统一从这里取 CardData。

---

## 还有哪些待办

1. **BattleFieldView**：SetUpCards/InstantiatePrefabs 中循环应为遍历 _playerUnits，且 unit 为 null 时 continue，确保生成数量与绑定正确。
2. **CardActionPanel**：实现 ShowFor(CardUnit)，以及「释放技能」「切换卡牌」等按钮与 BattleManager/CardUnit 的逻辑。
3. **BattlePanel**：卡 1/卡 2 按钮点击时打开 CardActionPanel 并传入当前方对应 CardUnit。
4. **胜负结算**：在回合结束或单位死亡后调用 CheckBattleEnd，并接入 UI 或流程。
5. **脚本结构图**：在 Documentation/脚本结构图.md 中补充 CardUnit、CardUI、BattleFieldView、CardDataBase、CardActionPanel、BattlePanel、TitleSceneManager、CardEffect 及 UIPanelManager 栈的说明。

---

## 下一步建议

- 先修 BattleFieldView 的生成与绑定逻辑，再补 CardActionPanel 与 BattlePanel 的联动。
- 接好 CheckBattleEnd 与胜负提示后，再扩展技能效果（CardEffect 子类）与更多 UI 反馈。

---

*需要继续开发时，可开启新对话，并让 AI 先读 `Documentation/2026-03-13_AI_SUMMARY.md`（或最新日期的 `*_AI_SUMMARY.md`）恢复上下文。*
