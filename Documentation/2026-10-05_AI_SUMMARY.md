# 2026-10-05 AI_SUMMARY

## 项目概述

碧蓝航线题材回合制卡牌游戏（团结引擎 2022.3.62t13 / Tuanjie 1.10.1），六层架构迁移后的代码树（Assets/Scripts/{Presentation,Application,Domain,Infrastructure,ConfigTypes,Editor}，全局命名空间，中文 XML 注释强制）。本轮会话完成了**战斗系统在新六层架构下的完整重建**（全新重写，不复制旧 `Scripts/Game/Battle` 代码）。

## 本轮工作目的

- 修复文档断层：重写严重过时的《脚本结构图.md》（原描述的是最早的 Core/Services 结构）；生成 CODELY.md。
- 按既定决策完成战斗系统重建：完整重建（UnitId+Slot 模型 7 阶段）｜全新重写｜全量表现（事件驱动动画+两段式选目标）｜同步单测。依据：ARCHITECTURE-PRINCIPLES.md（强制）、2026-05-31_BattleSystem_Redesign.md、技能架构.txt（Skill = Effect Pipeline + Context）。

## 技术决策

| 决策 | 说明 |
|------|------|
| Domain 自带事件总线 | 现有 IEventBus 在 Application 层，Domain 不可引用（禁止跨层）→ Domain/Battle/Events 定义 IBattleEventBus（依赖倒置），Infrastructure/BattleEventBus 实现，构造注入。与全局事件流隔离。 |
| 防御=计算属性 | DefenseBuff 不写状态、由 Defense 属性动态求和 → 免除 BuffHandlerRegistry（原方案含 IBuffHandler，评估后 YAGNI 省略；扩展点=BuffKind+效果）。 |
| 回滚记录栈 | 每个效果成功后 RecordRollback；任一效果失败 → 逆序 Rollback（静默不发事件）→ 管线成功后才消耗次数/设置冷却，杜绝"扣次数但效果不完整"。已知边角：管线致死又被回滚时 UnitDiedEvent 已发（罕见，接受）。 |
| AI 与玩家同管道 | BattleAIService.ExecuteSingleAction 走同一 SkillSystem.TryCast；评分器按效果种类注册（IScoreCalculator），Utility 评分与执行解耦。 |
| 阵亡自动补位 | BattleSession 订阅 UnitDiedEvent → 解绑补位 + 补位槽位标记 acted（本回合不可行动）→ 终局判定。数据所有权：HP 属 BattleUnit，伤害/死亡事件由其发布。 |
| 两段式选目标 | BattleService.TryBeginCast → SkillTargetSelectingEvent（UI 高亮）→ ConfirmCastTarget/CancelPendingCast；RequiresManualTarget 由 TargetSelector 提供。 |
| AI 阶段驱动 | BattleService 订阅 TurnPhaseChangedEvent → RunAiPhaseAsync（350ms 节奏、防重入 guard、失败候选降级尝试、行动枯竭自动 Pass+EndTurn）。 |
| 表现层零预制体兜底 | 战斗面板全部 [SerializeField] 优先 + 代码动态构建（NetworkErrorDialog 先例）；BattleShipFieldSlotView.Create 全动态。 |
| 测试程序集方案 | Assets/Tests/Editor 无 asmdef → 编入 Assembly-CSharp-Editor（NUnit 默认被编辑器程序集引用，EditMode 测试可直接被 Test Runner 发现）；若团结版 Test Runner 不识别，回退方案=建测试 asmdef（references: UnityEngine.TestRunner/UnityEditor.TestRunner/Assembly-CSharp + precompiledReferences: nunit.framework.dll）。 |

## 项目改动清单（增/删/改）

**增（39 个代码文件）**：
- Domain/Battle/Core：BattleSide、UnitId、Buff、BuffKind、BattleUnit、BattleField、BattleOutcome
- Domain/Battle/Turn：BattleTurnPhase、BattleTurnSystem
- Domain/Battle/Skills：ISkillEffect（含 ISkillEffectRollback）、SkillCastRequest、SkillCastOutcome、SkillExecutionContext、SkillValidator、SkillPipeline、SkillEffectRegistry、SkillSystem、Targeting/（ITargetSelectionStrategy+7 策略+TargetSelector）、Effects/（Damage/Heal/DefenseBuff/SelfHpDrain/RefreshCooldownChance 五效果）
- Domain/Battle/AI：AIAction、IScoreCalculator、5 评分器、ScoreCalculatorRegistry、BattleAIService
- Domain/Battle/Events：IBattleEvent、IBattleEventBus、BattleEvents（17 事件 DTO）
- Domain/Battle/BattleSession
- Application/Battle：IBattleService、BattleService、BattleViewData、SkillMenuEntry、FleetCardPreview
- Presentation/BattleScene：BattleShipFieldSlotView
- Infrastructure/Event：BattleEventBus
- Tests/Editor/Battle：9 个测试文件（90 用例）

**删**：无（旧 Game/Battle 此前已删）。

**改（关键）**：
- BattleUnit 增 SkillIds 构造参数、DrainHp、回滚支持（RestoreHpForRollback 双向/RemoveBuffForRollback）
- SkillConfig/SkillConfigSO：+Effects 列表/SkillEffectConfig（Kind/Value/Duration/Chance）+UseLimit
- LevelStageConfig/LevelStageConfigSO：+EnemyCardIds（敌方卡组 Schema）
- GameBootstrap：注册 IBattleService
- BattleSceneController：接通开局（默认面板）/Dispose 清理会话
- BattleMainPanel/BattleDeckSelectPanel/BattleActivePickPanel/BattleSettlementPanel：全量重写（事件驱动表现+动态构建+两段式选目标+真实结算）
- BattleScene.scene（YAML）：4 个战斗面板补 _panelId（BattleDeckSelect/BattleActivePick/BattleMain/BattleSettlement）、全部改 m_IsActive: 0（服务注入前 OnEnable 会空引用+层级错乱）
- SceneConfigTable.json：BattleScene.DefaultPanelId="BattleDeckSelect"
- 运行时数据：SkillConfigTable.json（6 技能含效果）、CardConfigTable.json（8 舰娘）、LevelStageConfigTable.json（3 关卡敌方卡组）
- 文档：CODELY.md（新建）、脚本结构图.md（全量重写+战斗章节）

## 已做修改（重点代码逐项）

1. **BattleUnit（伤害公式）**：改动前占位无实现 → TakeDamage 先扣 Defense（下限 0、上限当前 HP）、发 UnitDamagedEvent（0 伤也发=格挡表现）、致死发 UnitDiedEvent；DrainHp 无视防御（技能代价）。目的：数据所有权+表现层可播放格挡。
2. **SkillPipeline（回滚）**：旧版"失败继续执行" → 顺序执行、任一失败逆序回滚、不消耗次数。目的：技能可靠性（2026-05-28 建议 P2 的架构级修复）。
3. **BattleTurnSystem（行动权按槽位）**：→ 槽位 acted 数组；换牌不清除（TrySwitchActive 不触碰回合系统）；PassRemainingSideActions 支撑"结束回合"一键交棒；EndRound Tick 全员 Buff+冷却。目的：Redesign 阶段 4 规则定稿落地。
4. **BattleService（两段式+AI 驱动）**：→ 单一入口服务化；UI 不触碰领域对象；AI 阶段事件驱动异步循环。目的：层边界+可测试性。
5. **BattleMainPanel（占位→全量）**：改动前"自动战斗=2 秒后直接结算" → 完整对局交互+事件表现。目的：闭环可玩。
6. **场景 YAML 直改**：面板此前 _panelId 空+激活态 → 补 ID+去激活。目的：UIService 场景实例路径（FindObjectsOfType 匹配 PanelId）可正确打开面板且服务注入后才 OnEnable。

## 已知问题

- 代码未经团结编辑器编译验证（CLI 无编译环境）——需用户在编辑器中编译并跑 Test Runner。
- BattleScene 中 4 个旧面板对象（SlotActionMenu/Emoji/CardSwitch/SkillSelect）与 5 个战斗 Widget 预制体引用已删除的旧脚本 → Missing Script 提示，待编辑器清理。
- 运行时 JSON 数据是我编的示例数值（8 舰娘/6 技能/3 关卡），SO 授权通道需用 DatabasePopulator/ConfigExporter 重新同步。
- 1 回合 Buff 的"后手方释放当回合即过期"不对称（回合末统一 Tick 的固有语义，与旧设计一致，暂按受）。

## 补充（同日第二轮：修复编译错误 / 单测 5 红 / 运行时空引用 / 按历史逻辑恢复 Main 下四面板）

### 修复类
1. **编译错误（19 处）**：UI 误用 `UnityEngine.TextAlignment`/`FontStyle` → 改为 TMPro 的 `TextAlignmentOptions`/`FontStyles`（BattleMainPanel/SlotView/ActivePick/DeckSelect/Settlement）。
2. **编译错误（6 处 CS1657）**：测试中 `using var _ = ...` 声明了名为 `_` 的变量与后续 `out _` 弃元冲突 → 改名 `sub`。
3. **单测 5 红**：① 真实缺陷 `BattleUnit.TickSkillCooldowns` 在 `foreach` 中写字典索引器 → 该运行时枚举器失效（InvalidOperationException），改为先取键快照再 for 循环；② 3 处测试自身预期算错（换牌替补顺序应为 P1_2；两处 Heal 用例忘记 SetUp 单位 10 点基础防御）。
4. **运行时 MissingReferenceException（船坞首次打开）**：火忘式异步取图在 await 后未校验引用存活。修复 7 处（ShipyardCardSlotView/CardItemView/InventorySlotView/BuildPanel×2/ShipgirlDetailPanel×2），并对"回收重绑"与"卡池切换"加状态校验；BattleMainPanel 结算延迟加 `this == null` 守卫。

### 功能类：BattleScene 的 `---------- Main ----------` 容器下四面板按历史（HEAD）逻辑恢复
- **结论**：旧脚本依赖已删除的 `BasePanel`/`BattleUiFlow`/`BattleUiSession`/`BattleStartContext(旧)`/`FleetStore`/`GameResourceLoader`/`PanelNames`，**不能原样拷回**；但四面板的**场景 UI 控件全部健在**，故按历史逻辑重新接入现成控件。
- **新增服务能力**：`IFleetService.LoadAllGroupsAsync()`（编队列表，空存档给 2 个占位）；`BattleEntryService.RetryCurrentStageAsync()`（同关重开、重置编队与首发）与 `ReturnToStageSelectAsync()`（回大厅 + 打开 LevelStageSelect，区域由 StageId 反查），构造新增 IConfigService（GameBootstrap 注册同步）。
- **面板行为**：DeckSelect→DeckRoot 生成编队条目（名+成员），点击即进入首发选择，Back 返回大厅，Tip 提示；ActivePick→SlotsRoot 生成槽位并显示出击序号①②，Confirm 选满 2 名才可用，Back 返回，Tip 提示；Main→槽位挂 EnemyRowRoot/PlayerRowRoot（引用已写入场景 YAML），Header_Text 显示"关卡名 · 阶段文案"（P2 追加"— 敌方行动中…"），GiveUp_Button=离开战斗（放弃战斗并进入结算）、EndTurn_Button=结束回合（仅我方行动阶段可点），点击我方可操作槽位**放大该槽位**后打开操作菜单面板，敌方施法播放强调动画，**已按要求移除自加的自动战斗按钮与状态文本**；Settlement→Title_Text（胜利/失败/放弃战斗）+ Reward_Text（奖励占位+回合/原因），Back=返回关卡选择、Retry=再次挑战。
- **新增文件**：`Presentation/Common/PanelWidgetUtil.cs`（按名解析场景控件/动态建文本与按钮的共享工具）。

### 遗留（更新）
- 5 个战斗 Widget 预制体（BattleShipFieldSlotView/BattleActivePickSlotView/BattleCardSwitchReserveSlotView/BattleDeckEntryView/BattleSkillBarView.prefab）仍引用已删除旧视图脚本（Missing Script）；当前战斗 UI 为代码构建、不依赖它们。
- 本轮改动仍未编译验证、未 git 提交。

## 待办

1. 用户验证：编辑器编译 0 错 → Test Runner 全绿 → 完整对局（验收 8 条，见 USER_SUMMARY）。
2. 编辑器侧：清理场景 Missing Script 残留；制作正式战斗预制体美化（[SerializeField] 已留接口）；SO 配置同步重导。
3. 设置面板、AudioService 接入、其余 Domain 单测、命名空间迁移（详见脚本结构图.md 待办清单）。
