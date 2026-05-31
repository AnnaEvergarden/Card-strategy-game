# 2026-05-21 AI_SUMMARY（续）

## 本轮代码变更

- 删除 `BattleSkillIds.cs`、`BattleSkillContext.cs`；新增 `SkillCastRequest`（`Battle/Core`）。
- `SkillTargetKind` 扩展：`SingleAlly`、`All`、`AllAllies`、`AllEnemies`；新增 Targeting 策略与 `BattleTargetCollector`。
- 新增 `BattleBuffState`：`DefenseBuffEffect` 写入 Buff 列表并同步 `CardRuntime.Defense`；`TickTurnEnd` 待回合系统调用。
- `BattleFieldState`：`CopyAllRuntimeCardIds`、`Reset` 时 `BattleBuffState.Reset()`。
- 船坞持久化：`CardCollectionStore` 去掉编辑器假 `card_001`；`Load(forceReload)`；`SaveCurrent` 防空缓存覆盖磁盘；`ShipyardPanel` 强制重载；`GameBootstrap.OnDestroy` + 编辑器 `ExitingPlayMode` 保存。

## 两 Context 分工

| 类型 | 用途 |
|------|------|
| `SkillCastRequest` | UI→`BattleFacade` 请求（cardId 字符串） |
| `SkillExecutionContext` | 流水线运行时（`BattleUnit`、Targets、Outcome） |

## Buff 存储

- 本局：`BattleBuffState` 按 cardId 存 `RuntimeBuff` 列表；非卡牌 SO 上的字段。
- 触发：释放技能 → `DefenseBuffEffect` → `ApplyBuff(..., SkillBuffKind.DefenseBuff, ...)`；回合结束由 `BattleTurnSystem.EndRound` 调用 `TickTurnEnd()`。

## 待办

- 战斗回合结束时调用 `BattleBuffState.TickTurnEnd()`。
- 两段式选目标 UI。
- `PopupCanvas` Sorting Order = 100（场景手调）。
- 每 Effect 独立 `TargetScope`（可选）。

## 船坞丢数据根因（已缓解）

1. 编辑器无文件时注入假 `card_001` 与真实 `001` 不一致。
2. 停止 Play 时 `OnApplicationQuit` 不一定执行；域重载后空 `_cached` 经 `SaveAll` 覆盖有效 `card_collection.dat`。
3. 修复：去掉假数据、退出 Play 保存、保存前跳过「空缓存覆盖非空文件」、船坞 `Load(forceReload: true)`。
