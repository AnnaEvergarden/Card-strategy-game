# 夏日画风 V2 全量重绘记录（Summer2026V2）

**日期**：2026-05-29  
**风格**：`.cursor/rules/Art-Style-Unified.mdc`（Summer2026V2 — 场景写真 KV / 极简面板 / 夏日肖像）  
**输出根目录**：`Assets/Resources/Art/Generated/Redesign/Summer2026V2/`  
**Unity 加载前缀**：`Resources/Art/Generated/Redesign/Summer2026V2/<Category>/<filename>`

## 概要

| 项目 | 数量 |
|------|------|
| 原图清点（`Art/`，未删除） | **51** |
| Summer2026 旧批次（保留） | 63 |
| **Summer2026V2 本轮生成** | **63** |
| 生成失败 / 拦截 | **0** |

### V2 相对 Summer2026 的风格升级

| 维度 | Summer2026 | Summer2026V2 |
|------|------------|--------------|
| 标题屏 | 全员 Q 版合影感 | **场景写真 KV**：2–3 人部分入镜、浅景深 |
| 战斗背景 | 通用海滩 | **中央竞技场 + 两侧躺卧观众** |
| 面板 | 较复杂场景 | **极简**：大渐变 + ≤1 装饰元素 |
| 舰娘图标 | 统一 bust | **每人独立 pose/机位/表情/泳装** |
| 色板 | 高饱和旧色 | **现代清新** `#5EB8FF` `#3DD6C8` `#F5E6C8` |
| 分辨率 | 1920×1080 为主 | 背景 **2560×1440** 下限；图标 **512×512** |

## 目录与数量

```
Summer2026V2/
├── BackGround/   15
├── Panels/       13
├── Button/       15
├── Border/        2
├── Card/          7
├── Slot/          1
└── Icon/
    ├── Shipgirl/  8
    └── Skills/    2
```

## 原 BackGround → Summer2026V2

| 原文件 `Art/BackGround/` | V2 文件 | 用途 |
|--------------------------|---------|------|
| `TitleScreen.png` | `title-screen-summer-v2.png` | 标题屏 KV / TitlePanel |
| `MainScene.png` | `main-scene-summer-v2.png` | 主场景 Hub / MainPanel |
| `Battle.png` | `battle-background-summer-v2.png` | 对局 / BattleMainPanel |
| `Inventory.png` | `inventory-background-summer-v2.png` | 仓库 / InventoryPanel |
| `BackGround.png` | `background-default-summer-v2.png` | 默认叠层底图 |
| `BackGround (2).png` | `background-02-summer-v2.png` | 通用场景变体 |
| `BackGround (3).png` | `background-03-summer-v2.png` | 灯塔远景 |
| `BackGround (4).png` | `background-04-summer-v2.png` | 海滨 boardwalk 黄昏 |
| `BackGround (5).png` | `background-05-summer-v2.png` | 潮池倒影 |
| `BackGround (6).png` | `background-06-summer-v2.png` | 椰林荫影 |
| `BackGround (7).png` | `background-07-summer-v2.png` | 海上浮 dock |
| `BackGround (8).png` | `background-08-summer-v2.png` |  seaside cafe 露台 |
| `BackGround (9).png` | `background-09-summer-v2.png` | 沙滩排球场 |
| `BackGround (10).png` | `background-10-summer-v2.png` | 日落剪影 |
| *(Summer2026 有 login-rewards)* | `login-rewards-summer-v2.png` | 登录奖励（原 Art 无独立文件） |

## Panel（PanelNames）→ Summer2026V2/Panels/

| PanelNames | V2 文件 |
|------------|---------|
| `LoginPanel` | `panel-login-background-summer-v2.png` |
| `ShipyardPanel` | `panel-shipyard-background-summer-v2.png` |
| `ShipgirlDetailPanel` | `panel-shipgirl-detail-background-summer-v2.png` |
| `FleetPanel` / `FleetPickPanel` | `panel-fleet-background-summer-v2.png` |
| `BuildPanel` | `panel-build-background-summer-v2.png` |
| `CardRevealPanel` | `panel-card-reveal-background-summer-v2.png` |
| `ActivityPanel` | `panel-activity-background-summer-v2.png` |
| `LevelSelectPanel` | `panel-level-select-background-summer-v2.png` |
| `LevelAreaSelectPanel` | `panel-level-area-select-background-summer-v2.png` |
| `LevelStageSelectPanel` | `panel-level-stage-select-background-summer-v2.png` |
| `BattleDeckSelectPanel` | `panel-battle-deck-select-background-summer-v2.png` |
| `BattleActivePickPanel` | `panel-battle-active-pick-background-summer-v2.png` |
| `BattleSettlementPanel` | `panel-battle-settlement-background-summer-v2.png` |
| `TitlePanel` | 使用 `BackGround/title-screen-summer-v2.png` |
| `MainPanel` | 使用 `BackGround/main-scene-summer-v2.png` |
| `InventoryPanel` | 使用 `BackGround/inventory-background-summer-v2.png` |
| `BattleMainPanel` | 使用 `BackGround/battle-background-summer-v2.png` |

## 原 Button / Border / Card / Slot → V2

| 原文件 | V2 文件 |
|--------|---------|
| `Button/Button.png` | `Button/button-primary-summer-v2.png` |
| `Button/Button (2).png` … `(6).png` | `button-variant-02-summer-v2.png` … `06` |
| `Button/Button_Blue.png` | `button-blue-summer-v2.png` |
| `Button/Button_White.png` | `button-white-summer-v2.png` |
| `Button/Back_Btn.png` | `button-back-summer-v2.png` |
| `Button/Back_Btn (2).png` | `button-back-alt-summer-v2.png` |
| `Button/Home_Btn.png` | `button-home-summer-v2.png` |
| `Button/Home_Btn (2).png` | `button-home-alt-summer-v2.png` |
| `Button/Next Page.png` | `button-next-page-summer-v2.png` |
| `Button/Previous Page.png` | `button-previous-page-summer-v2.png` |
| `Button/ItemSlot.png` | `button-item-slot-summer-v2.png` |
| `Border/Border_1.png` | `Border/border-frame-01-summer-v2.png` |
| `Border/Border_3.png` | `Border/border-frame-03-summer-v2.png` |
| `Card/Card_*.png`（7 种稀有度） | `Card/card-frame-*-summer-v2.png` |
| `Slot/InventorySlot.png` | `Slot/inventory-slot-summer-v2.png` |

## 舰娘夏日写真肖像设计说明

> 角色为 **原创 archetype 气质**，不复刻版权角色外观。文件名沿用卡牌 ID。

| 角色 | Archetype | Pose | 机位 | 表情 | 夏日服装 |
|------|-----------|------|------|------|----------|
| **Enterprise** | 骄傲航母娘 | 单手叉腰 | 低角度仰拍 | 沉稳自信微笑 | 白金军官帽 + 敞开的藏青短夹克 + 白比基尼 |
| **Laffey** |  sleepy 驱逐娘 | 双臂交叠托腮 | 正面近 bust | 半闭眼打盹笑 |  oversized 蓝卫衣 + 泳衣 + 罐装饮料 |
| **Z23** | 认真学究驱逐 | 单指推眼镜 | 右侧 3/4 | 专注严肃 | 浅蓝夏校 vest + 无袖白 top + 锚发夹 |
| **Javelin** | 元气驱逐 | 比耶前倾 | 轻微 Dutch angle | 大笑单眼 wink | 黄白条纹比基尼 + 祭典花环 |
| **Bogue** | 温柔轻母 | 双手捧花于胸前 | 略俯正面 | 温柔 blush 微笑 |  floral  sundress + 草帽 |
| **Cassin** | 双子（活泼） | 吹泡泡糖 + 摇滚手势 | 仰角近景 | 恶作剧 smirk | 橙 tank + 牛仔短裤 + 星形发夹 |
| **Downes** | 双子（冷静） | 盘腿吃 popsicle | 侧面 3/4 | 酷 relaxed 半笑 | 蓝夏威夷衬衫 + 白花耳后 |
| **Ranger** |  veteran 训练航母 | 敬礼 + 纸扇遮阳 | 左侧 3/4 bust |  mentor 温暖笑 | 白夏 dress + 红 scarf + 船长遮阳帽 |

## 技能图标

| 原文件 | V2 文件 | 设计 |
|--------|---------|------|
| `Icon/Skills/快速起飞.png` | `skill-quick-takeoff-summer-v2.png` | 海鸥加速起飞 + 风 swirl |
| `Icon/Skills/浴火重生.png` | `skill-phoenix-rebirth-summer-v2.png` | 金色凤凰羽 + 夕阳波浪形 flame |

## Unity 替换步骤

1. **入口**：打开目标 Scene / Prefab（如 `TitleScene` → TitlePanel 背景 Image）。
2. **替换 Sprite**：Inspector → Source Image → 浏览至 `Assets/Resources/Art/Generated/Redesign/Summer2026V2/...`。
3. **Texture Import**：全屏背景 Max Size ≥ 2048；图标 512；UI chrome 透明 PNG，Alpha Is Transparency 勾选。
4. **技能图标**：若 `SkillConfigSO.skillIconResourcePath` 指向旧路径，改为 V2 路径或复制到运行时路径。
5. **验证**：Play 模式检查 Title / Battle / Main 三场景色温一致；Console 无 Missing Sprite。

## 批次关系（勿删）

| 批次 | 路径 | 状态 |
|------|------|------|
| 原图 | `Art/` | 保留 |
| Galgame 旧重绘 | `Generated/Redesign/` | 保留 |
| Summer2026 | `Generated/Redesign/Summer2026/` | 保留 |
| **Summer2026V2** | `Generated/Redesign/Summer2026V2/` | **当前推荐** |

## 待办 / 可选

- [ ] Unity Prefab 批量替换引用（Editor 脚本）
- [ ] `FleetPickPanel` 独立极简底图（当前与 Fleet 共用）
- [ ] 战斗叠层（Emoji/SkillSelect）独立半透明底
- [ ] 人工 QA：TitleScreen / Battle 观众席构图是否遮挡 UI 安全区

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-05-27 | Galgame 重绘 → `Redesign/` |
| 2026-05-28 | Summer2026 全量 63 张 |
| 2026-05-29 | **Summer2026V2** 新美术方向全量 63 张；规则文件升级 |
