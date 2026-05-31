# 美术重绘记录（2026-05-27）

## 概要

- **原图清点**：`Assets/Resources/Art/` 共 **51** 张（png/jpg），未删除任何原文件。
- **本次生成**：**33** 张，位于 `Assets/Resources/Art/Generated/Redesign/`（按类别分子目录）。
- **风格规则**：`.cursor/rules/Art-Style-Unified.mdc`（配合 `AI-Generated-Assets.mdc`）。
- **安全策略**：含「battle / naval / military」等词的首次 Prompt 被拦截；已改用中性风景/室内描述并成功生成。

## 目录结构

```
Art/Generated/Redesign/
├── BackGround/     # 15 — 对应原 BackGround/ 命名资源 + 默认底图
├── Panels/         # 13 — 按 PanelNames 定制的面板底图
├── Button/         # 2  — 样例（主按钮、返回）
├── Border/         # 1  — 样例边框
├── Card/           # 2  — 样例（Normal、Rare 框）
└── Slot/           # 1  — 仓库格子样例
```

Unity 资源路径前缀：`Resources/Art/Generated/Redesign/<Category>/<filename>`

## 原 BackGround → 重绘映射

| 原文件 | 重绘文件（Redesign/BackGround/） |
|--------|----------------------------------|
| `TitleScreen.png` | `title-screen-redesign.png` |
| `MainScene.png` | `main-scene-redesign.png` |
| `Battle.png` | `battle-background-redesign.png` |
| `Inventory.png` | `inventory-background-redesign.png` |
| `LoginRewards.png` | `login-rewards-redesign.png` |
| `BackGround.png` | `background-default-redesign.png` |
| `BackGround (2).png` | `background-02-redesign.png` |
| `BackGround (3).png` | `background-03-redesign.png` |
| `BackGround (4).png` | `background-04-redesign.png` |
| `BackGround (5).png` | `background-05-redesign.png` |
| `BackGround (6).png` | `background-06-redesign.png` |
| `BackGround (7).png` | `background-07-redesign.png` |
| `BackGround (8).png` | `background-08-redesign.png` |
| `BackGround (9).png` | `background-09-redesign.png` |
| `BackGround (10).png` | `background-10-redesign.png` |

## Panel → 背景映射（Task 3）

| Panel（PanelNames） | 推荐重绘背景 | 备注 |
|---------------------|--------------|------|
| `TitlePanel` | `BackGround/title-screen-redesign.png` | 标题场景默认栈底 |
| `LoginPanel` | `Panels/panel-login-background.png` | |
| `MainPanel` | `BackGround/main-scene-redesign.png` | 游戏主场景 Hub |
| `InventoryPanel` | `BackGround/inventory-background-redesign.png` | |
| `ShipyardPanel` | `Panels/panel-shipyard-background.png` | |
| `ShipgirlDetailPanel` | `Panels/panel-shipgirl-detail-background.png` | |
| `FleetPanel` | `Panels/panel-fleet-background.png` | |
| `FleetPickPanel` | `Panels/panel-fleet-background.png` | 可与编队共用 |
| `BuildPanel` | `Panels/panel-build-background.png` | |
| `CardRevealPanel` | `Panels/panel-card-reveal-background.png` | |
| `ActivityPanel` | `Panels/panel-activity-background.png` | |
| `LevelSelectPanel` | `Panels/panel-level-select-background.png` | |
| `LevelAreaSelectPanel` | `Panels/panel-level-area-select-background.png` | |
| `LevelStageSelectPanel` | `Panels/panel-level-stage-select-background.png` | |
| `BattleDeckSelectPanel` | `Panels/panel-battle-deck-select-background.png` | 战斗场景栈底 |
| `BattleActivePickPanel` | `Panels/panel-battle-active-pick-background.png` | |
| `BattleMainPanel` | `BackGround/battle-background-redesign.png` | |
| `BattleSettlementPanel` | `Panels/panel-battle-settlement-background.png` | |
| `BattleCardSwitchPanel` | `BackGround/battle-background-redesign.png` | 叠层，可半透明 Image |
| `BattleSkillSelectPanel` | `BackGround/background-default-redesign.png` | 叠层菜单 |
| `BattleSlotActionMenuPanel` | `BackGround/background-default-redesign.png` | 小菜单 |
| `BattleEmojiPanel` | `BackGround/background-default-redesign.png` | 占位叠层 |
| `SaveSelectPanel` | `BackGround/title-screen-redesign.png` | 预留，未实现 Prefab |

## 原图清点（按文件夹）

| 文件夹 | 数量 | 本次重绘 |
|--------|------|----------|
| BackGround | 15 | 15/15 |
| Button | 14 | 2 样例 |
| Border | 2 | 1 样例 |
| Card | 7 | 2 样例 |
| Slot | 1 | 1 样例 |
| Icon/Shipgirl | 8 jpg | 0（需角色设定） |
| Icon/Skills | 2 png | 0 |
| Generated（旧） | 1 | 保留 |

## 待手动 / 下轮生成

1. **Button** 其余 12 张（`Button (2-6)`, `Blue/White`, `Home/Back`, `ItemSlot`, 翻页等）— 以 `button-primary-redesign.png` 为参考批量变体。
2. **Border** `Border_3.png`。
3. **Card** 其余 5 种稀有度框：`Elite`, `SuperRare`, `Activity`, `SeaLegend`, `None`。
4. **Icon/Shipgirl** 8 张角色头像 — 需按卡面/舰娘设定逐张生成，避免 AI 张冠李戴。
5. **Icon/Skills** 2 张 — 需与技能文案一致的技能图标。
6. **战斗叠层面板** 可选独立半透明底图（非必须）。

## Unity 替换步骤（示例：MainPanel）

1. 打开 `Assets/Scenes/GameScene.scene`，Hierarchy 选中 `MainPanel` 下背景 `Image`。
2. Inspector → **Source Image** 拖入 `Art/Generated/Redesign/BackGround/main-scene-redesign.png`。
3. **Image Type**：Simple 或 Sliced；全屏 Stretch 锚点四边拉满。
4. Play 模式确认无粉图、比例正常；Console 无 Missing Sprite。

## Prompt 注意（安全）

- 避免：`battle`, `war`, `military`, `naval`, `weapon`, `ship`（易触发拦截）。
- 推荐：`ocean horizon`, `coastal promenade`, `craft studio`, `visual novel`, `golden hour`, `soft painterly anime`。
