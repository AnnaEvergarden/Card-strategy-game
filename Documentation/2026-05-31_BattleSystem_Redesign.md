# Battle System 重构设计方案

## 概述

对战斗系统进行架构级重构，核心引入 **UnitId + 固定 Slot 槽位** 模型替代现有的 `cardId` + `Side` 双参数传递模式，统一 P1/P2 命名规范，将 AI 系统通用化。

## 变更列表（14 项）

1. `_reserveBuffer` 改为 P1/P2 各一个
2. `ActiveCardIds` 别名删除，统一使用 `P1ActiveCardIds`
3. `_runtimeByCardId` 重命名为 `_p1RuntimeByCardId`
4. 全面审计命名规范
5. **UnitId 系统**：固定 2 槽位绑定 BattleUnit
6. ApplyBuff 使用 UnitId
7. SkillCastRequest 使用 UnitId
8. TryConsumeSkillUse 顺序修正
9. EvaluateBestActions 接受 BattleSide
10. Calculator 评分模式
11. 通用工具提取（ShipName 等）
12. BattleTurnSystem 接受 BattleSide
13. AI 系统通用化
14. 全局重设计

## 第一阶段：命名规范化（安全重构）

### 改动文件

| 文件 | 改动 |
|------|------|
| `BattleFieldState.cs` | `_runtimeByCardId` → `_p1RuntimeByCardId`；删除 `ActiveCardIds` 别名，只保留 `P1ActiveCardIds`；`_reserveBuffer` → `_p1ReserveBuffer` + 新增 `_p2ReserveBuffer` |
| `BattleTurnSystem.cs` | `IsPlayerActionPhase` → `IsSideActionPhase(BattleSide)` 重载；`_p2CardIdsBuffer` → `_p1CardIdsBuffer` + `_p2CardIdsBuffer` |
| 全工程搜索 | `ActiveCardIds` 引用替换为 `P1ActiveCardIds`；`IsPlayerActive()` 调用替换 |

> **可验证**：此阶段结束后 Unity 编译通过，战斗流程不受影响。

### `_reserveBuffer` 拆分细节

```csharp
// 旧
private readonly List<string> _reserveBuffer = new(FleetStore.MaxCardsPerFleet);
// 新
private readonly List<string> _p1ReserveBuffer = new(FleetStore.MaxCardsPerFleet);
private readonly List<string> _p2ReserveBuffer = new(FleetStore.MaxCardsPerFleet);
```

`CopyReserveCardIds(List<string> destination)` 改为接受 `BattleSide` 参数：
```csharp
public void CopyReserveCardIds(List<string> destination, BattleSide side)
```

## 第二阶段：UnitId + Slot 核心模型（架构变更）

### BattleUnit 持久化

```csharp
public sealed class BattleUnit
{
    public string UnitId { get; }           // "P1_0" ~ "P1_5", "P2_0" ~ "P2_5"
    public string CardId { get; }           // 配置表 cardId
    public BattleSide Side { get; }         // 构造时确定，只读

    // 运行时数据（直接挂载，不再通过 BattleFieldState 字典访问）
    public int Hp { get; private set; }
    public int Defense { get; private set; }
    public bool IsDead => Hp <= 0;

    // 技能状态
    public Dictionary<string, int> SkillCooldowns { get; }        // skillId → 剩余冷却
    public Dictionary<string, int> SkillRemainingUses { get; }    // skillId → 剩余次数，-1=无限

    // Buff 列表
    public List<Buff> Buffs { get; }

    // 核心方法
    public void TakeDamage(int damage, out int applied);
    public void Heal(int amount);
    public void AddBuff(BuffConfigSO buffConfig);
    public void TickCooldowns();    // 回合结束时减冷却
    public bool CanUseSkill(SkillConfigSO skill);   // 检查冷却+次数
    public bool ConsumeSkillUse(string skillId);
    public void SetCooldown(string skillId, int turns);
}
```

### BattleFieldState Slot 结构

```csharp
public sealed class BattleFieldState
{
    // 所有卡牌单位（固定 6 个）
    public IReadOnlyList<BattleUnit> P1Units { get; }   // [0..5]
    public IReadOnlyList<BattleUnit> P2Units { get; }   // [0..N]

    // 上场槽位（引用 P1Units / P2Units 中的元素）
    public BattleUnit?[] P1Slots { get; }                // [0..1]
    public BattleUnit?[] P2Slots { get; }                // [0..1]

    // 替补队列（UnitId 列表，按序上场）
    private Queue<string> _p1ReserveQueue;
    private Queue<string> _p2ReserveQueue;

    // 核心操作
    public void BindSlot(BattleSide side, int slotIndex, BattleUnit unit);
    public void UnbindSlot(BattleSide side, int slotIndex);  // 阵亡/surrender
    public bool TryFillSlotFromReserve(BattleSide side, int slotIndex);

    // 清理阵亡（遍历 slot，HP<=0 → Unbind → TryFillSlotFromReserve）
    public void TryRemoveDeadCards(BattleSide side);
}
```

### 初始化流程（`EnsureInitialized`）

```
1. 从 BattleStartContext 读取 P1 卡组 6 张 cardId
2. 创建 6 个 BattleUnit（UnitId = P1_0 ~ P1_5），HP/防御从 CardConfigSO 读取
3. Slots[0] = P1Units[0], Slots[1] = P1Units[1]
4. 剩余 4 个加入 _p1ReserveQueue
5. 从关卡配置读取 P2 卡组（不定数量）
6. 创建对应数量 BattleUnit（UnitId = P2_0 ~ P2_N）
7. 前 2 个绑定 P2Slots，其余加入 _p2ReserveQueue
```

### 换牌逻辑

```csharp
public bool TrySwitchActive(string focusUnitId, string incomingUnitId, out string failureReason)
{
    // 1. 确定目标槽位（根据 focusUnitId 找到 SlotIndex + Side）
    // 2. 校验 incomingUnitId 未被上场且存活
    // 3. 出场的 unit 放回替补队尾
    // 4. 入场的 unit 绑定该槽位
    // 5. 标记该槽位本回合已行动（不重置 acted flag）
}
```

### 淘汰的旧结构

| 旧结构 | 替代 |
|--------|------|
| `_runtimeByCardId` / `_p2RuntimeByCardId` | 消亡，数据在 `BattleUnit.Hp` / `.Defense` |
| `_activeCardIds` / `_p2ActiveCardIds` | 消亡，由 `P1Slots` / `P2Slots` 代替 |
| `_deckCardIds` | 消亡，由 `P1Units` / `P2Units` + Slots 代替 |
| `"side\|cardId\|skillId"` key 拼串 | 消亡，直接 `BattleUnit.SkillCooldowns[skillId]` |
| `TryGetRuntime(cardId, side, out rt)` | 消亡，直接 `slots[i].Hp` |
| `TryGetRuntime(cardId)` 无 side | 消亡 |
| `IsAlive(cardId)` / `IsAlive(cardId, side)` | 消亡，改用 `unit.IsDead` |
| `GetSide(cardId)` | 消亡，BattleUnit 自带 Side |
| `IsOnSide(cardId, side)` | 消亡 |
| `CopyAllRuntimeCardIds` | 消亡 |

## 第三阶段：技能系统改造

### SkillCastRequest

```csharp
public struct SkillCastRequest
{
    public string CasterUnitId;       // 原 CasterCardId
    public string TargetUnitId;       // 原 TargetCardId
    public SkillConfigSO SkillConfig;
}
```

### SkillSystem.TryCast

```csharp
public static bool TryCast(
    string casterUnitId,
    SkillConfigSO skillConfig,
    string targetUnitId,
    out SkillCastOutcome outcome)
// 不再需要 casterSide 参数，从 BattleUnit.Side 获取
```

`TryCast` 内部包含完整校验（冷却/次数由 `BattleUnit.CanUseSkill` 完成），执行成功后 `BattleUnit.ConsumeSkillUse()` + `SetCooldown()` 在 `TryCast` 内统一处理。

当前 `TryFinalizeCast` 中 `SkillSystem.TryCast` 之后的 `TryConsumeSkillUse` + `SetSkillCooldown` 代码合并进 `TryCast`，消除"技能执行完才发现次数不够"的问题。

### 目标选取策略

```csharp
public sealed class SingleEnemyTargetStrategy : ITargetSelectionStrategy
{
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId, out string failureReason)
    {
        // 直接 context.Battle.GetUnit(manualTargetUnitId) 获取 BattleUnit
        // target.Side != context.Caster.Side 即可
    }
}
```

不再需要手动传入 `targetSide` 参数。

### ApplyBuff

```csharp
// 旧
battle.Field.AddBuff(cardId, buffConfig, side);

// 新
targetUnit.AddBuff(buffConfig);   // BattleUnit.Buffs.Add(...)
```

## 第四阶段：BattleTurnSystem 改造

```csharp
public sealed class BattleTurnSystem
{
    private int _roundNumber = 1;
    private BattleTurnPhase _phase = BattleTurnPhase.P1Action;
    private bool _p1AiEnabled;      // 自动战斗
    private bool _p2AiEnabled;      // NPC 默认 true

    // 按槽位追踪行动（2 个槽位，不按 UnitId）
    private readonly bool[] _p1SlotActed = new bool[2];
    private readonly bool[] _p2SlotActed = new bool[2];

    // 缓冲（双阵营各一个）
    private readonly List<string> _p1CardIdsBuffer = new(2);
    private readonly List<string> _p2CardIdsBuffer = new(2);

    // 核心 API
    public bool IsSideActionPhase(BattleSide side);                          // 取代 IsPlayerActionPhase
    public bool IsSideRoundComplete(BattleSide side);
    public bool CanOpenActionMenu(string unitId);                            // 取代 CanOpenActionMenu(cardId)
    public bool TrySelectCardForAction(string unitId, out string reason);
    public bool HasSlotActedThisRound(BattleSide side, int slotIndex);       // 按槽位检查
    public bool TryCompleteSlotAction(BattleSide side, int slotIndex);       // 标记槽位已行动
    public bool TryEndSideTurn(BattleSide side);                             // 取代 TryEndP1Turn
    public bool TryForceEndPhase(BattleSide side);                           // 取代 TryForceEndP2Phase
}
```

**行动权规则**：行动状态绑定到**槽位**而非 BattleUnit。
- `TrySwitchActive`（换牌）后，该槽位的 `acted` 标记不清除
- `OnTurnStateChanged` 中判断当前阶段是否 AI 启用，启用则 `StartCoroutine(AiPhaseRoutine(side))`

## 第五阶段：AI 系统通用化

### BattleAIService

```csharp
public static class BattleAIService
{
    public static void EvaluateBestActions(BattleSide side, List<AIAction> results);
    public static bool ExecuteSingleAction(AIAction action);
    public static void PassRemainingUnits(BattleSide side);
}
```

核心变化：
- 所有方法接受 `BattleSide side` 参数，不再硬编码 P2
- 目标枚举：`side == P1` 时敌人 = P2Slots，`side == P2` 时敌人 = P1Slots
- `P2PhaseRoutine` → 通用 `AiPhaseRoutine(BattleSide side)`

### Calculator 评分体系

```csharp
public interface IScoreCalculator
{
    float Calculate(BattleUnit caster, BattleUnit target, SkillConfigSO skill, AIProfileSO profile);
}

public class DamageScoreCalculator : IScoreCalculator { ... }
public class HealScoreCalculator : IScoreCalculator { ... }
public class DefenseBuffScoreCalculator : IScoreCalculator { ... }
public class SelfDrainScoreCalculator : IScoreCalculator { ... }
public class CooldownRefreshScoreCalculator : IScoreCalculator { ... }
```

`BattleAIService.ScoreSkillTarget` 遍历效果列表，通过 `CalculatorRegistry.Get(category)` 分发到对应 Calculator。

### 通用工具提取

```csharp
public static class BattleUtility
{
    public static string GetShipDisplayName(string cardId);      // 原 ShipName
    public static ShipFaction GetCardFaction(string cardId);     // 原 ResolveCardFaction
    public static int GetCardMaxHp(string cardId);
    public static int GetCardDefense(string cardId);
}
```

UI 层（面板标题、Debug 日志）、AI 层统一调用。

## 第六阶段：UI 层改造

### BattleShipFieldSlotView

```csharp
public sealed class BattleShipFieldSlotView : MonoBehaviour
{
    private string _boundUnitId;            // 改为 UnitId
    [SerializeField] private GameObject _blockingOverlay;  // 半透明遮罩

    public void Bind(BattleUnit unit);                       // 直接传 BattleUnit 对象
    public void SetActionMenuAvailable(bool visible);        // 保留但背后查 CanOpenMenu 逻辑不变

    // 交互控制：用遮罩替代 interactable 切换
    public void SetSlotInteractive(bool active)
    {
        _blockingOverlay?.SetActive(!active);
        // canvasGroup 保持 true，不阻断 DOTween 动画
    }

    public void ApplyTurnInteractionState()
    {
        // 使用 _boundUnitId 查 TurnSystem.CanOpenActionMenu
        // 使用 IsAwaitingSkillTarget 判断是否可选为目标
        // 用 SetSlotInteractive 统一管理
    }
}
```

### BattleMainPanel

```csharp
// 词典从 cardId→view 改为 UnitId→view
private Dictionary<string, BattleShipFieldSlotView> _p1SlotViews = new(2);
private Dictionary<string, BattleShipFieldSlotView> _p2SlotViews = new(2);

// SpawnFieldSlot 不再传入 cardId + side
private void SpawnFieldSlot(RectTransform row, BattleUnit unit, BattleSide side)
{
    var view = Instantiate(...).GetComponent<BattleShipFieldSlotView>();
    view.Bind(unit);
    // view 自身从 unit.UnitId, unit.Side, unit.Hp 读取数据
    _p1SlotViews[unit.UnitId] = view;  // 改以 UnitId 为 key
}

// 通用 AI 协程（替代 P2PhaseRoutine）
private IEnumerator AiPhaseRoutine(BattleSide side)
{
    yield return new WaitForSeconds(0.35f);
    if (BattleFacade.TryOpenBattleSettlement())
        yield break;

    var actions = new List<AIAction>(4);
    BattleAIService.EvaluateBestActions(side, actions);
    // ...执行逻辑与 P2PhaseRoutine 相同，但 side 由参数决定
}
```

## 第七阶段：BattleFacade 简化

```csharp
public static class BattleFacade
{
    public static void TryCastSkill(string casterUnitId, int skillIndex, string skillId);
    public static void TryCompleteSkillOnTarget(string targetUnitId);     // 不再要 targetSide
    public static void CancelPendingSkillCast();
    public static bool TryOpenBattleSettlement();
}
```

`TryCompleteSkillOnTarget(string targetUnitId)` 删除 `targetSide` 参数——从 `BattleUnit` 的 `Side` 字段即可获取。

## 依赖顺序

```
阶段1: 命名规范化（安全重构，可单独编译验证）
    │
    ▼
阶段2: UnitId + Slot 核心模型（BattleFieldState + BattleUnit 重写）
    │
    ├────→ 阶段3: 技能系统改造（SkillCastRequest, Effects, Targeting）
    ├────→ 阶段4: BattleTurnSystem 改造（Slot 行动追踪 + 双阵营 API）
    │
    ▼
阶段5: AI 系统通用化（BattleAIService + Calculator + BattleUtility）
    │
    ▼
阶段6: UI 层改造（SlotView + MainPanel）
    │
    ▼
阶段7: BattleFacade 简化
```

## 不受影响的文件

| 文件 | 原因 |
|------|------|
| `BattleContext.cs` | 纯组合根，只引用 Field/Turns/Events 接口 |
| `BattleUiSession.cs` | 只存 pending cast / focus card，改为 UnitId 即可 |
| `BattleUiFlow.cs` | UI 导航编排，无阵营逻辑 |
| `BattleSettlementPanel.cs` | 纯 UI 展示，无战斗逻辑 |
| `BattleEventBus.cs` | 事件系统，事件体加 UnitId 字段 |
| `AIAction.cs` | DTO，字段改为 casterUnitId / targetUnitId |

## 验证方式

每阶段结束 Unity 编译通过 + 完整战斗流程正常：
1. 进入战斗 → 双方卡牌上场显示正确
2. P1 回合：可打开操作菜单、放技能、换牌、结束回合
3. 技能目标选取：点击敌方/P1 友军可正确识别
4. 相同 cardId 双方在场时：扣血/技能互不影响
5. P2 回合：AI 执行行动、强调动画播放
6. 阵亡 → 替补上场 → 槽位行动标记正确
7. 全灭 → 结算面板弹出
8. 复盘流程无误
