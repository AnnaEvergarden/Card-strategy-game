# 夏日画风全量重绘记录（Summer2026）

**日期**：2026-05-28  
**风格**：`.cursor/rules/Art-Style-Unified.mdc`（夏日海滩 / 碧蓝航线系 Q 版手游风）  
**输出根目录**：`Assets/Resources/Art/Generated/Redesign/Summer2026/`  
**Unity 加载前缀**：`Resources/Art/Generated/Redesign/Summer2026/<Category>/<filename>`

## 概要

| 项目 | 数量 |
|------|------|
| 原图清点（`Art/`，未删除） | 51 |
| 本轮前已有 Summer2026 | 28（BackGround 15 + Panels 13） |
| 本轮新生成并入库 | 35（Button 15 + Border 2 + Card 7 + Slot 1 + Icon 10） |
| **Summer2026 合计** | **63** |
| 生成失败 / 拦截 | 0 |

主参考图：夏日标题屏 TitleScreen（`reference_image_paths` 必带）。舰娘图标额外参考 Cheshire Q 版辅图。

## 目录与数量

```
Summer2026/
├── BackGround/   15  （上轮已完成，本轮跳过）
├── Panels/       13  （上轮已完成，本轮跳过）
├── Button/       15  （本轮新增）
├── Border/        2  （本轮新增）
├── Card/          7  （本轮新增）
├── Slot/          1  （本轮新增）
└── Icon/
    ├── Shipgirl/  8  （本轮新增）
    └── Skills/    2  （本轮新增）
```

## 原 BackGround → Summer2026（已完成，本轮未重生成）

| 原文件 `Art/BackGround/` | Summer2026 文件 | 用途 |
|--------------------------|-----------------|------|
| `TitleScreen.png` | `title-screen-summer.png` | 标题屏 / TitlePanel |
| `MainScene.png` | `main-scene-summer.png` | 主场景 Hub / MainPanel |
| `Battle.png` | `battle-background-summer.png` | 对局场景 / BattleMainPanel |
| `Inventory.png` | `inventory-background-summer.png` | 仓库 / InventoryPanel |
| `LoginRewards.png` | `login-rewards-summer.png` | 登录奖励 |
| `BackGround.png` | `background-default-summer.png` | 默认底图 / 叠层菜单 |
| `BackGround (2).png` … `(10).png` | `background-02-summer.png` … `background-10-summer.png` | 通用场景变体 |

## Panel（PanelNames）→ Summer2026 Panels/

| PanelNames | Summer2026 文件 | 备注 |
|------------|-----------------|------|
| `LoginPanel` | `panel-login-background-summer.png` | |
| `ShipyardPanel` | `panel-shipyard-background-summer.png` | |
| `ShipgirlDetailPanel` | `panel-shipgirl-detail-background-summer.png` | |
| `FleetPanel` | `panel-fleet-background-summer.png` | FleetPick 可共用 |
| `BuildPanel` | `panel-build-background-summer.png` | |
| `CardRevealPanel` | `panel-card-reveal-background-summer.png` | |
| `ActivityPanel` | `panel-activity-background-summer.png` | |
| `LevelSelectPanel` | `panel-level-select-background-summer.png` | |
| `LevelAreaSelectPanel` | `panel-level-area-select-background-summer.png` | |
| `LevelStageSelectPanel` | `panel-level-stage-select-background-summer.png` | |
| `BattleDeckSelectPanel` | `panel-battle-deck-select-background-summer.png` | |
| `BattleActivePickPanel` | `panel-battle-active-pick-background-summer.png` | |
| `BattleSettlementPanel` | `panel-battle-settlement-background-summer.png` | |
| `TitlePanel` | 使用 `BackGround/title-screen-summer.png` | 无独立 panel 图 |
| `MainPanel` | 使用 `BackGround/main-scene-summer.png` | |
| `InventoryPanel` | 使用 `BackGround/inventory-background-summer.png` | |
| `BattleMainPanel` | 使用 `BackGround/battle-background-summer.png` | |
| `SaveSelectPanel` | 同 Title（预留） | |
| 战斗叠层（Emoji/Skill/Slot 等） | `background-default-summer.png` | 可选半透明叠层 |

## 原 Button → Summer2026/Button/（本轮）

| 原文件 `Art/Button/` | Summer2026 文件 |
|----------------------|-----------------|
| `Button.png` | `button-primary-summer.png` |
| `Button (2).png` | `button-variant-02-summer.png` |
| `Button (3).png` | `button-variant-03-summer.png` |
| `Button (4).png` | `button-variant-04-summer.png` |
| `Button (5).png` | `button-variant-05-summer.png` |
| `Button (6).png` | `button-variant-06-summer.png` |
| `Button_Blue.png` | `button-blue-summer.png` |
| `Button_White.png` | `button-white-summer.png` |
| `Back_Btn.png` | `button-back-summer.png` |
| `Back_Btn (2).png` | `button-back-alt-summer.png` |
| `Home_Btn.png` | `button-home-summer.png` |
| `Home_Btn (2).png` | `button-home-alt-summer.png` |
| `Next Page.png` | `button-next-page-summer.png` |
| `Previous Page.png` | `button-previous-page-summer.png` |
| `ItemSlot.png` | `button-item-slot-summer.png` |

## 原 Border / Card / Slot（本轮）

| 原文件 | Summer2026 文件 |
|--------|-----------------|
| `Border/Border_1.png` | `Border/border-frame-01-summer.png` |
| `Border/Border_3.png` | `Border/border-frame-03-summer.png` |
| `Card/Card_Normal.png` | `Card/card-frame-normal-summer.png` |
| `Card/Card_Rare.png` | `Card/card-frame-rare-summer.png` |
| `Card/Card_Elite.png` | `Card/card-frame-elite-summer.png` |
| `Card/Card_SuperRare.png` | `Card/card-frame-super-rare-summer.png` |
| `Card/Card_Activity.png` | `Card/card-frame-activity-summer.png` |
| `Card/Card_SeaLegend.png` | `Card/card-frame-sea-legend-summer.png` |
| `Card/Card_None.png` | `Card/card-frame-none-summer.png` |
| `Slot/InventorySlot.png` | `Slot/inventory-slot-summer.png` |

## 原 Icon → Summer2026/Icon/（本轮）

| 原文件 | Summer2026 文件 | 说明 |
|--------|-----------------|------|
| `Icon/Shipgirl/Enterprise.jpg` | `Icon/Shipgirl/enterprise-summer.png` | Q 版 bust |
| `Icon/Shipgirl/Laffey.jpg` | `Icon/Shipgirl/laffey-summer.png` | |
| `Icon/Shipgirl/Javelin.jpg` | `Icon/Shipgirl/javelin-summer.png` | |
| `Icon/Shipgirl/Z23.jpg` | `Icon/Shipgirl/z23-summer.png` | |
| `Icon/Shipgirl/Bogue.jpg` | `Icon/Shipgirl/bogue-summer.png` | |
| `Icon/Shipgirl/Cassin.jpg` | `Icon/Shipgirl/cassin-summer.png` | |
| `Icon/Shipgirl/Downes.jpg` | `Icon/Shipgirl/downes-summer.png` | |
| `Icon/Shipgirl/Ranger.jpg` | `Icon/Shipgirl/ranger-summer.png` | |
| `Icon/Skills/快速起飞.png` | `Icon/Skills/skill-quick-takeoff-summer.png` | 对应展示名「快速起飞」 |
| `Icon/Skills/浴火重生.png` | `Icon/Skills/skill-phoenix-rebirth-summer.png` | 对应展示名「浴火重生」 |

> **技能图标替换**：若 `SkillConfigSO` 使用默认路径 `Art/Icon/Skills/{DisplayName}`，需在 Inspector 将 `skillIconResourcePath` 改为 Summer2026 路径，或将文件复制为中文名并放在 `Resources` 下对应路径。

## 旧版 Galgame 重绘（保留，勿删）

`Art/Generated/Redesign/`（无 Summer2026 后缀）仍为 2026-05-27 旧批次，与夏日批次并存。策划替换时优先选用 **Summer2026** 目录。

## Unity 替换步骤（示例）

1. 打开目标 Prefab / Scene（如 `GameScene` → `MainPanel` 背景 Image）。
2. Inspector → **Source Image** → 选择 `Art/Generated/Redesign/Summer2026/...` 对应 Sprite。
3. Play 模式确认无 Missing Sprite、色温与标题屏一致。

## Prompt 安全词

- **避免**：battle, war, military, naval, weapon, warship, ship（易拦截）。
- **使用**：summer harbor, beach boardwalk, coastal sunset, game arena backdrop, swift launch, phoenix rebirth glow。

## 待办 / 可选

- [ ] 为 `FleetPickPanel` 单独生成 `panel-fleet-pick-background-summer.png`（当前与 Fleet 共用）。
- [ ] 战斗叠层（BattleEmoji / SkillSelect 等）独立半透明底图（非必须）。
- [ ] 在 `SkillConfigSO` 批量更新 `skillIconResourcePath` 指向 Summer2026 技能图标。
- [ ] Unity 批量替换 Prefab 引用（可后续 Editor 脚本）。

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-05-27 | 初版 Galgame 重绘 33 张 → `Redesign/` |
| 2026-05-28 | Summer2026 全量：存量 28 + 本轮 35 = **63** 张 |
