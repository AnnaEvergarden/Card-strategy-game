# AI 上下文总结 (2026-03-12)

> 供新对话恢复上下文：架构、已做修改、技术决策、待办。  
> 本轮将「项目改动」「工作目的」「代码逐一说明」继续拆分为多份文档：`2026-03-12_项目改动清单.md`、`2026-03-12_工作目的.md`、`2026-03-12_代码改动说明.md`。

## 项目概述

- **类型**：Unity 卡牌策略游戏（C#，Unity 2022.3.61t7 / 团结编辑器）
- **解决方案**：`卡牌策略游戏.sln`，内含 Assembly-CSharp 项目

## 当前架构（类功能摘要，含本轮新增/变更）

| 类 | 路径 | 功能 |
|----|------|------|
| **CardData** | Core/Card/CardData.cs | 卡牌静态配置 ScriptableObject；基础属性，PVP/PVE 共用；GetBaseAttack/GetBaseHealth |
| **BattleContext** | Core/Battle/BattleContext.cs | 单局只读上下文：isPVP、PlayerDeck、EnemyDeck（List\<CardInstance\>） |
| **BattleManager** | Core/Battle/BattleManager.cs | 战斗驱动（极简）：CardStatsService 总战力对比；StartBattle、GetDeckTotalPower、RegisterCardData；日志前缀 `[BattleManager]` |
| **SaveData** | Services/SaveData.cs | 存档结构：playerName、gold、inventory、**decks**（List\<DeckSlot\>）、**currentDeckIndex**；无参构造时默认卡组 |
| **DeckSlot** | 同文件 | deckName、instanceIds |
| **AccountEntry / AccountListData** | Services/AccountEntry.cs | 账号条目（userName、accountId、password）；账号列表；本轮修复中文注释乱码 |
| **AccountManager** | Services/AccountManager.cs | 本地账号：accounts.json、LoadAccounts、Register、Login、Logout、GetCurrentAccountId、IsLoggedIn、GetAccountNum；本轮改为继承 Singleton\<AccountManager\>，Register 签名简化 |
| **SaveManager** | Services/SaveManager.cs | 按**账号+槽位**：`Save_{accountId}_{slot}.json`；currentSlotIndex / SelectSlot；HasSaveInSlot；GetCurrentDeck；**新增 GetCurrentAccountSaveCount / CreateDefaultSaveInSlot**；改为继承 Singleton\<SaveManager\> |
| **GameBootstrap** | Services/GameBootstrap.cs | 启动：Awake 取 SaveManager/AccountManager/BattleManager、Load、RegisterCardData；Start 若已登录 SelectSlot(0)；日志前缀 `[GameBootstrap]` |
| **Singleton\<T\>** | Common/Singleton.cs | 通用 MonoBehaviour 单例基类：线程安全 Instance、可配置跨场景持久化（PersistAcrossScenes + DontDestroyOnLoad）、重复实例自毁 |
| **UIPanelManager** | Services/UIPanelManager.cs | 面板管理单例：继承 Singleton\<UIPanelManager\>；持有 `Dictionary<string, BaseUIPanel>`；RegisterPanel、ShowPanel(name)、GetPanel；跨场景不销毁 |
| **UIPanelNames** | UI/BaseUIPanel.cs 内部静态类 | 统一管理 `TitlePanel` / `LoginPanel` / `SaveSelectPanel` 名称常量，避免字符串硬编码与拼写错误 |
| **BaseUIPanel** | UI/BaseUIPanel.cs | 所有 UI 面板抽象基类：序列化字段 PanelName；Awake 中向 UIPanelManager 注册并默认隐藏；ShowPanel/HidePanel 使用 SetActive；预留 OnShow/OnHide |
| **TitlePanel** | UI/TitlePanel.cs | 标题界面：登录→Login 面板、登出、设置（预留）、退出游戏；本轮改为通过序列化 `_LoginPanelName` 配置目标面板 |
| **LoginPanel** | UI/LoginPanel.cs | 登录/注册界面：账号/密码 TMP_InputField，支持 Login 与 Register；成功登录→SaveSelect，取消→Title；面板名通过序列化字段配置；修复多处中文日志乱码与逻辑问题 |
| **SaveSelectPanel** | UI/SaveSelectPanel.cs | 存档选择界面：继承 BaseUIPanel；根据 SaveManager.SaveSlotCount 动态生成槽位按钮，修复闭包 bug 与逻辑顺序，正确处理「空槽位创建默认存档」与「已有存档进 GameScene」 |
| **CardInstance / CardStatsService** | 引用，定义未在仓库 | 运行时实例；静态 GetAttack/GetHealth(instance, data, useEnhancement) |

## 技术决策（新增/确认点）

1. **统一 MonoBehaviour 单例基类**：新增 `Singleton<T>`，将原本分散在各 Manager 内部的单例/跨场景逻辑抽取出来，今后凡需跨场景持久存在的管理类均建议继承该基类。  
2. **UI 面板命名集中管理**：通过 `UIPanelNames` 统一面板名字常量，同时在 `BaseUIPanel` 中增加可序列化 `PanelName` 字段，既可用常量也可在 Inspector 中配置，避免硬编码字符串和拼写错误导致 ShowPanel 找不到面板。  
3. **UI 导航依赖字符串配置而非互相引用**：Title/Login/SaveSelect 等面板通过字符串面板名与 `UIPanelManager` 交互，不直接持有其他面板脚本引用，降低耦合。  
4. **存档槽位创建默认存档时机**：点击空槽位时，不再直接 SelectSlot 导致逻辑分支失效，而是通过 `CreateDefaultSaveInSlot` 显式创建默认存档，保留「空槽位」与「已有存档」两种行为的可控区分。  
5. **中文统一 UTF-8 简体**：所有本轮涉及的中文注释与日志均按 `.cursor/rules/code-chinese-format.mdc` 规范修正，包括 `LoginPanel`、`SaveSelectPanel`、`AccountEntry` 等，清理乱码。  

## 项目改动清单（增/删/改）摘要

- **2026-03-12**：  
  - **增** `Assets/Scripts/Common/Singleton.cs`（通用单例基类）；  
  - **改** `Assets/Scripts/Services/UIPanelManager.cs`（继承 Singleton、修复 ShowPanel 递归 bug）；  
  - **改** `Assets/Scripts/UI/BaseUIPanel.cs`（新增 `UIPanelNames`、序列化 PanelName、Awake 中注册并隐藏）；  
  - **改** `Assets/Scripts/UI/TitlePanel.cs`（使用 `_LoginPanelName` 配置目标面板）；  
  - **改** `Assets/Scripts/UI/LoginPanel.cs`（支持注册、修复登录失败仍跳转、改为可配置目标面板名、修复中文乱码）；  
  - **改** `Assets/Scripts/UI/SaveSelectPanel.cs`（继承 BaseUIPanel、动态生成槽位、修复闭包 bug、正确处理空槽位与 GameScene 加载、修复中文）；  
  - **改** `Assets/Scripts/Services/AccountManager.cs`（继承 Singleton、Register 签名简化、增加 GetAccountNum）；  
  - **改** `Assets/Scripts/Services/SaveManager.cs`（继承 Singleton、新增 GetCurrentAccountSaveCount 与 CreateDefaultSaveInSlot）；  
  - **改** `Assets/Scripts/Services/AccountEntry.cs`（修复中文注释乱码）；  
  - **改** `Documentation/脚本结构图.md`（若已根据本轮更新补充 UI 与 Services 描述——已在 03-10 基础上延续）。  

完整列表见 **`2026-03-12_项目改动清单.md`**。

## 本轮工作目的摘要

- **搭建通用的 Manager 单例与 UI 面板管理基础设施**，让跨场景管理类（账号、存档、UI 管理器等）有统一、可复用的实现，后续增加新 Manager 只需继承 `Singleton<T>`。  
- **补全并稳定「标题 → 登录/注册 → 存档选择 → 进入游戏场景」这一完整用户流程**，确保 UI 与 AccountManager、SaveManager 之间的交互逻辑正确、无明显漏洞。  
- **清理并统一代码中的中文显示**，杜绝乱码和不规范文案，为后续调试与协作打好基础。  

详细说明见 **`2026-03-12_工作目的.md`**。

## 已做修改（技术要点，按脚本）

- **Singleton\<T\>（新增）**  
  - 作用：提供线程安全的 `Instance` 访问、重复实例自毁、可选 `DontDestroyOnLoad` 支持的通用 MonoBehaviour 单例基类。  
  - 改动后要点：`protected virtual bool PersistAcrossScenes => true` 默认跨场景；`Awake` 中若已存在其他实例则销毁当前，否则缓存为 `_instance` 并视配置调用 `DontDestroyOnLoad`；`OnDestroy` 时若为当前实例则清空静态引用。  

- **UIPanelManager（改）**  
  - 作用：统一管理所有 UI 面板的注册与显隐切换。  
  - 改动前：内部自实现单例，`ShowPanel` 在 else 分支中错误递归调用 `ShowPanel(_panelName)`，存在无限递归风险。  
  - 改动后：继承 `Singleton<UIPanelManager>`，移除手写静态 `Instance` 与 `Awake`；`ShowPanel` 改为先遍历所有面板依次 Hide，再使用 `TryGetValue` 取目标面板并调用 `_panel.ShowPanel()`；未找到目标时输出警告提示。  
  - 目的：去除重复单例实现，修复递归 bug，使面板切换逻辑稳定可复用。  

- **BaseUIPanel + UIPanelNames（改）**  
  - 作用：所有 UI 面板的父类和面板名集中管理。  
  - 改动前：`BaseUIPanel` 使用 `GetPanelName()` 返回 `gameObject.name`，无统一常量；部分逻辑空实现或未统一隐藏。  
  - 改动后：  
    - 新增静态类 `UIPanelNames`，集中定义 `Title` / `Login` / `SaveSelect` 常量字符串；  
    - `BaseUIPanel` 新增 `[SerializeField] public string PanelName` 字段，Awake 中调用 `RegisterPanel()` 与 `HidePanel()`，确保所有面板初始时隐藏；  
    - `RegisterPanel()` 通过 `UIPanelManager.Instance.RegisterPanel(PanelName, this)` 完成注册，并在 PanelName 为空或 Manager 为空时输出中文警告；  
    - `ShowPanel`/`HidePanel` 直接调用 `gameObject.SetActive(true/false)`。  
  - 目的：统一面板注册流程与默认显隐行为，支持常量与 Inspector 配置两种命名方式，避免依赖 GameObject 名称与魔法字符串。  

- **TitlePanel（改）**  
  - 作用：标题界面入口。  
  - 改动前：登录时硬编码 `"Login"` 作为面板名。  
  - 改动后：新增序列化字符串 `_LoginPanelName`，Inspector 中配置实际目标面板名，`OnClickLogin` 调用 `UIPanelManager.Instance.ShowPanel(_LoginPanelName)`；保留登出、设置占位与退出逻辑。  
  - 目的：消除硬编码字符串，使 UI 流程可通过配置调整，避免未来改名时遗漏。  

- **LoginPanel（改）**  
  - 作用：账号登录/注册面板。  
  - 改动前：  
    - 日志中存在中文乱码；  
    - 登录失败仍会继续切换到存档选择面板；  
    - 目标面板名硬编码且存在拼写错误（如 "SaveSelcet"）；  
    - 仅支持登录，不支持注册。  
  - 改动后：  
    - 新增 `_SaveSelectPanelName`、`_TitlePanelName` 两个序列化字段，分别代表目标存档选择面板与标题面板名；  
    - `Start()` 中检查 `accountManager` 是否赋值，为空则输出 `"AccountManager为空!"`；  
    - `OnClickLogin()` 中先从输入框取用户名与密码，调用 `accountManager.Login`，若返回 false 则输出 `"登录失败!"` 并 `return`，仅在成功时才 `ShowPanel(_SaveSelectPanelName)`；  
    - 新增 `OnClickRegister()`：若 `Login` 已成功则提示用户已存在并登录，否则调用 `Register(userName, password)` 创建账号并输出注册成功日志；  
    - `OnClickCancel()` 使用 `_TitlePanelName` 切回标题面板；  
    - 所有中文日志统一为 UTF-8 简体，遵守 `code-chinese-format`。  
  - 目的：修复流程漏洞与中文乱码问题，补充注册能力，使登录/注册流程清晰可控且易于调试。  

- **SaveSelectPanel（改）**  
  - 作用：当前账号下存档槽位的选择与创建。  
  - 改动前：  
    - 未继承 `BaseUIPanel`，与 UI 体系不统一；  
    - 动态绑定按钮时直接捕获循环变量 `i`，存在闭包 bug；  
    - 先调用 `SelectSlot`（内部会 `Load` 并可能创建默认存档），再调用 `HasSaveInSlot`，导致逻辑分支形同虚设；  
    - 加载场景名使用 `"Game"`，与实际 `GameScene` 不一致；  
    - 部分中文日志乱码。  
  - 改动后：  
    - 继承 `BaseUIPanel`；  
    - `Start()` 中：  
      - 若 `saveManager` 为空则尝试使用 `SaveManager.Instance`；  
      - 校验 `container`、`slotPrefab`、`saveManager` 非空，否则输出带类名前缀的警告；  
      - 清空 `container` 现有子节点；  
      - 使用 `for (int i = 0; i < SaveManager.SaveSlotCount; i++)` 动态生成槽位，使用局部变量 `slotIndex = i` 绑定 `onClick.AddListener(() => OnClickSlot(slotIndex))` 以修复闭包；  
      - 为文本组件设置 `"存档位置 : {slotIndex}\n是否有存档 {有/无}"`。  
    - `OnClickSlot(int _slotIndex)` 中：  
      - 若 `saveManager` 为空则输出 `"SaveManager为空！"` 并返回；  
      - 若 `!HasSaveInSlot(_slotIndex)`，则调用 `CreateDefaultSaveInSlot(_slotIndex)` 创建默认存档并输出警告，然后返回（暂不直接进场景）；  
      - 若存在存档，则调用 `SelectSlot(_slotIndex)`，输出 `"即将加载游戏场景..."`，最后 `SceneManager.LoadScene("GameScene")`。  
    - 全部中文字符串修复为 UTF-8 简体。  
  - 目的：统一 UI 抽象、修复闭包与逻辑时序 bug，正确区分「创建默认存档」与「进入已有存档游戏」，并确保加载场景名与项目配置一致。  

- **AccountManager（改）**  
  - 作用：本地账号管理。  
  - 改动前：自行实现单例/生命周期；Register 需要传入 `_accountId` 虽然内部仍用 Guid，新旧接口不一致。  
  - 改动后：  
    - 继承 `Singleton<AccountManager>`，复用通用单例逻辑；  
    - `Register` 签名简化为 `Register(string _userName, string _password)`，内部统一通过 `Guid.NewGuid().ToString()` 生成 accountId，避免外部传入；  
    - 新增 `GetAccountNum()` 返回账号总数（`int?`），供 UI 或调试使用。  
  - 目的：收敛单例实现、简化外部调用接口，并为后续 UI 展示账号数量提供数据来源。  

- **SaveManager（改）**  
  - 作用：存档读写与槽位管理。  
  - 改动前：自行实现单例/生命周期；缺少针对「当前账号已有几份存档」和「在指定槽位创建默认存档」的直接方法。  
  - 改动后：  
    - 继承 `Singleton<SaveManager>`；  
    - 新增 `GetCurrentAccountSaveCount()`，遍历 `0..SaveSlotCount-1`，统计 `HasSaveInSlot(i)` 为 true 的数量，用于 UI 动态展示或统计；  
    - 新增 `CreateDefaultSaveInSlot(int _slotIndex)`，在索引合法时设置 `currentSlotIndex` 并调用 `Load()`，利用原有「文件不存在则创建默认存档并 Save」逻辑为该槽位生成默认存档。  
  - 目的：补齐与 UI 交互需要的接口，同时使「空槽位创建默认存档」语义清晰集中。  

- **AccountEntry（改）**  
  - 作用：账号条目数据结构。  
  - 改动前：`accountId`、`password` 字段的中文注释存在乱码。  
  - 改动后：将注释修正为 `// 账号唯一标识` 与 `// 密码`，完全符合 UTF-8 简体规范。  
  - 目的：消除中文乱码，保证代码阅读体验与调试信息一致。  

## 已知小问题 / 潜在优化

- `Singleton<T>` 为 MonoBehaviour 基类，部分继承类仍定义了 `private void Awake()`，在 Unity 中会**隐藏**基类 `protected virtual Awake()`，若未显式调用 `base.Awake()`，可能导致单例初始化与 `DontDestroyOnLoad` 未完全按预期执行；目前尚未暴露为致命问题，但后续可考虑统一改为 `protected override void Awake()` 并调用 `base.Awake()`。  
- `CardInstance`、`CardStatsService` 具体实现仍不在仓库中，后续需补全或确认存放位置。  
- UI 交互仍为基础版本：空存档创建默认存档后目前仍停留在存档选择界面，是否自动进入游戏可根据设计再调整。  

## 待办 / 未完成点

- 统一整理所有继承 `Singleton<T>` 的类（如 AccountManager、SaveManager、UIPanelManager），将其 `Awake` 改为 `protected override void Awake()` 并显式调用 `base.Awake()`，以确保单例与跨场景逻辑行为一致。  
- 根据实际 UI 设计，决定点击空槽位创建默认存档后是否自动进入游戏场景，或增加确认弹窗。  
- 补充 `CardInstance`、`CardStatsService` 的实现脚本，或在脚本结构图中说明其位置与职责。  
- 后续可扩展更多 UI 面板（设置界面、提示弹窗等），继续复用 `BaseUIPanel + UIPanelManager + Singleton<T>` 体系。  

---

*生成时间：2026-03-12；总结与存储流程（含多文档区分）。新对话请先读本文件，再按需查阅 项目改动清单 / 工作目的 / 代码改动说明。*

