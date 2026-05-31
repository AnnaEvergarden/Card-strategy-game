# 夏日画风 V3 全量重绘记录（Summer2026V3）

**日期**：2026-05-30  
**风格**：`.cursor/rules/Art-Style-Unified.mdc`（Summer2026V3 — 场景写真 + **色键 UI** + 夏日肖像）  
**输出根目录**：`Assets/Resources/Art/Generated/Redesign/Summer2026V3/`  
**Unity 加载前缀**：`Resources/Art/Generated/Redesign/Summer2026V3/<Category>/<filename>`

## 概要

| 项目 | 数量 |
|------|------|
| 前序子 agent 已完成 | BackGround **9**（中断前） |
| 本轮续作新增 | **54** |
| **Summer2026V3 合计** | **63** |
| 生成失败 / 拦截（已重试成功） | 见下方「错误记录」 |

### V3 相对 V2 的核心升级

| 维度 | V2 | V3 |
|------|----|----|
| Button/Border/Card/Slot | 透明底或 AI 假透明 | **纯色键 `#FF00FF`**，PS 抠图工作流 |
| Icon/Skills | 渐变底 | **色键 `#FF00FF`**（便于提取） |
| BackGround/Panels/Shipgirl | 同 V2 | 延续，后缀 `-summer-v3` |
| Generated/ 旧批次 | V2 等保留 | **已清空 Generated/ 全部 PNG**（123 张）后仅留 V3 |

## 色键规范（Photoshop 抠图）

| 项目 | 值 |
|------|-----|
| **标准色键色** | `#FF00FF`（纯品红 magenta） |
| **备选色键** | `#00FF00`（纯绿，当 UI 含大量粉/红时） |
| **适用类别** | `Button/`、`Border/`、`Card/`、`Slot/`、`Icon/Skills/` |
| **不适用** | `BackGround/`、`Panels/`、`Icon/Shipgirl/`（全场景/渐变底） |

### PS 快速步骤

1. 色彩范围 → 取样 `#FF00FF` → 容差 40–80 → 删除/蒙版。
2. 或 Select Subject → 选择并遮住 → 导出 PNG-24 透明。
3. Unity：Sprite (2D and UI) + Alpha Is Transparency。

## 目录与数量

```
Summer2026V3/
├── BackGround/   15
├── Panels/       13
├── Button/       15  ← 色键
├── Border/        2  ← 色键
├── Card/          7  ← 色键
├── Slot/          1  ← 色键
└── Icon/
    ├── Shipgirl/  8
    └── Skills/    2  ← 色键
```

## BackGround → Summer2026V3

| 原文件 `Art/BackGround/` | V3 文件 | 用途 |
|--------------------------|---------|------|
| `TitleScreen.png` | `title-screen-summer-v3.png` | 标题屏 KV |
| `MainScene.png` | `main-scene-summer-v3.png` | 主场景 Hub |
| `Battle.png` | `battle-background-summer-v3.png` | 对局场 |
| `Inventory.png` | `inventory-background-summer-v3.png` | 仓库 |
| `BackGround.png` | `background-default-summer-v3.png` | 默认叠层 |
| `BackGround (2).png` | `background-02-summer-v3.png` | 场景变体 |
| `BackGround (3).png` | `background-03-summer-v3.png` | 灯塔远景 |
| `BackGround (4).png` | `background-04-summer-v3.png` | 海滨 boardwalk 黄昏 |
| `BackGround (5).png` | `background-05-summer-v3.png` | 潮池倒影 |
| `BackGround (6).png` | `background-06-summer-v3.png` | 椰林荫影 |
| `BackGround (7).png` | `background-07-summer-v3.png` | 海上浮 dock |
| `BackGround (8).png` | `background-08-summer-v3.png` | 海边露台 |
| `BackGround (9).png` | `background-09-summer-v3.png` | 沙滩排球场 |
| `BackGround (10).png` | `background-10-summer-v3.png` | 日落剪影 |
| *(登录奖励)* | `login-rewards-summer-v3.png` | 登录奖励 |

## Panel → Summer2026V3/Panels/

| PanelNames | V3 文件 |
|------------|---------|
| `LoginPanel` | `panel-login-background-summer-v3.png` |
| `ShipyardPanel` | `panel-shipyard-background-summer-v3.png` |
| `ShipgirlDetailPanel` | `panel-shipgirl-detail-background-summer-v3.png` |
| `FleetPanel` / `FleetPickPanel` | `panel-fleet-background-summer-v3.png` |
| `BuildPanel` | `panel-build-background-summer-v3.png` |
| `CardRevealPanel` | `panel-card-reveal-background-summer-v3.png` |
| `ActivityPanel` | `panel-activity-background-summer-v3.png` |
| `LevelSelectPanel` | `panel-level-select-background-summer-v3.png` |
| `LevelAreaSelectPanel` | `panel-level-area-select-background-summer-v3.png` |
| `LevelStageSelectPanel` | `panel-level-stage-select-background-summer-v3.png` |
| `BattleDeckSelectPanel` | `panel-battle-deck-select-background-summer-v3.png` |
| `BattleActivePickPanel` | `panel-battle-active-pick-background-summer-v3.png` |
| `BattleSettlementPanel` | `panel-battle-settlement-background-summer-v3.png` |

## UI Chrome（色键 `#FF00FF`）

### Button（15）

`button-primary-summer-v3.png`、`button-variant-02` … `06`、`button-blue`、`button-white`、`button-back`、`button-back-alt`、`button-home`、`button-home-alt`、`button-next-page`、`button-previous-page`、`button-item-slot`

### Border（2）

`border-frame-01-summer-v3.png`、`border-frame-03-summer-v3.png`

### Card（7）

`card-frame-normal`、`rare`、`elite`、`super-rare`、`sea-legend`、`activity`、`none`（均 `-summer-v3.png`）

### Slot（1）

`inventory-slot-summer-v3.png`

## Icon/Shipgirl（8）

| 角色 | V3 文件 | 设计要点 |
|------|---------|----------|
| Enterprise | `enterprise-summer-v3.png` | 叉腰仰拍、军官帽+夹克 |
| Laffey | `laffey-summer-v3.png` | 托腮打盹、蓝卫衣 |
| Z23 | `z23-summer-v3.png` | 推眼镜 3/4、夏校 vest |
| Javelin | `javelin-summer-v3.png` | 比耶、黄白夏装+花环（重试后 modest 版） |
| Bogue | `bogue-summer-v3.png` | 捧花、 floral 连衣裙 |
| Cassin | `cassin-summer-v3.png` | 吹泡泡、橙 tank |
| Downes | `downes-summer-v3.png` | 吃冰棒、夏威夷衬衫 |
| Ranger | `ranger-summer-v3.png` | 敬礼+纸扇、白夏 dress |

## Icon/Skills（2，色键）

| 原语义 | V3 文件 | 设计 |
|--------|---------|------|
| 快速起飞 | `skill-quick-takeoff-summer-v3.png` | 海鸥加速 + 风 swirl |
| 浴火重生 | `skill-phoenix-rebirth-summer-v3.png` | 金凤凰羽 + 夕阳 flame |

## Unity 替换步骤

1. **色键类**：PS 抠图 → 透明 PNG → 再拖入 Unity。
2. **全屏背景**：Texture Max Size ≥ 2048。
3. **入口**：各 Panel Prefab → Background Image → 浏览 `Summer2026V3/...`。
4. **验证**：Play 模式 Title / Main / 对局三场景；Console 无 Missing Sprite。

## 错误记录

| 资源 | 问题 | 处理 |
|------|------|------|
| `background-08-summer-v3` | 首次 content safety 拦截 | 改 prompt（parasol terrace）后成功 |
| `javelin-summer-v3` |  bikini 相关多次拦截 + 1 次 504 | 改为 modest 夏装+花环后成功 |
| `title-screen-summer-v3` | 前序 agent 首次拦截 | 简化 prompt 后成功 |

## 批次关系

| 批次 | 路径 | 状态 |
|------|------|------|
| 原图 | `Art/` | 保留 |
| Summer2026 / V2 | `Generated/Redesign/Summer2026*` | **PNG 已删**（仅 .meta 可能残留） |
| **Summer2026V3** | `Generated/Redesign/Summer2026V3/` | **当前推荐（63 张）** |

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-05-29 | Summer2026V2 全量 63 张 |
| 2026-05-30 | 清空 Generated PNG；**Summer2026V3** 色键 UI 工作流全量 63 张 |
