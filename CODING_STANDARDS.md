# C# Coding Standards & Naming Conventions

## Microsoft Design Guidelines + Unity Community Conventions

| Identifier | Case | Example |
|---|---|---|
| Class | PascalCase | `TitlePanel`, `ResourceService` |
| Method | PascalCase | `OnClickLogin()`, `LoadAsync()` |
| Property | PascalCase | `PanelId`, `Container` |
| Interface | I + PascalCase | `IResourceService` |
| Enum type | PascalCase | `PanelId`, `SceneId` |
| Enum value | PascalCase | `BattleDeckSelect` |
| Private field | `_` + camelCase | `_panelId`, `_configMap` |
| Serialized field | `[SerializeField] private _camelCase` | `[SerializeField] private Button _loginButton` |
| Parameter | camelCase | `(string username, string password)` |
| Local variable | camelCase | `var result = ...` |
| Const | PascalCase | `private const string DefaultAddress` |
| Readonly field | `_` + camelCase | `private readonly IResourceService _resourceService` |

## Unity Special Rules

- `[SerializeField] private Type _name` — shows as `_name` in Inspector
- `[Tooltip("中文描述")]` — Chinese tooltips in Inspector
- Script class name = file name = Prefab name (e.g. `TitlePanel.cs` → `TitlePanel` component → `TitlePanel.prefab`)

## Prohibited

- ❌ `private int badName;` — missing `_` prefix
- ❌ `public int BadField;` — use `{ get; set; }` property instead (except `[Serializable]` data classes)
- ❌ `OnclickLogin()` — must be PascalCase `OnClickLogin()`
- ❌ `void Load_Async()` — no underscore separators, use `LoadAsync()` instead
- ❌ Methods without Chinese XML comments

## XML Comment Rule

Every method, property, and class must have Chinese XML comments:

```csharp
/// <summary>
/// 登录面板：提供账号密码输入、记住密码、注册与登录功能。
/// </summary>
public sealed class LoginPanel : PanelBase
```

## Exception: Data Classes

`[System.Serializable]` data classes may use public fields (required for Unity JsonUtility / Inspector):

```csharp
[System.Serializable]
public class CardConfig : IConfigData
{
    public string Id => CardId; // property
    public string CardId;       // public field (allowed)
}
```
