using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 编辑器下：根据规则将 ScriptableObject 在 Project 中的资产文件名（不含扩展名）与字段同步；玩家构建中不执行任何操作。
/// </summary>
public static class ScriptableObjectAssetRenameUtility
{
    /// <summary>
    /// 若 <paramref name="desiredBaseName"/> 为空或仅空白则跳过；会去除非法文件名字符后与当前文件名比较，相同则不改。
    /// </summary>
    /// <param name="asset">目标资产（通常为 this）。</param>
    /// <param name="desiredBaseName">期望文件名，不含扩展名。</param>
    public static void TryRenameAsset(Object asset, string desiredBaseName)
    {
#if UNITY_EDITOR
        if (asset == null || string.IsNullOrWhiteSpace(desiredBaseName))
        {
            return;
        }

        var sanitized = SanitizeFileName(desiredBaseName.Trim());
        if (string.IsNullOrEmpty(sanitized))
        {
            return;
        }

        var path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var current = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrEmpty(current) || current == sanitized)
        {
            return;
        }

        var err = AssetDatabase.RenameAsset(path, sanitized);
        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogWarning($"[ScriptableObject 重命名] {path}: {err}");
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 资产期望主文件名（不含扩展名）：<c>{typePrefix}_{displayName}_{id}</c>；
    /// 无显示名时为 <c>{typePrefix}_{id}</c>；仅有显示名时为 <c>{typePrefix}_{displayName}</c>；
    /// 二者皆空时返回 <paramref name="fallbackIfBothEmpty"/>（可空则返回 <c>{typePrefix}_</c>）。
    /// </summary>
    /// <param name="typePrefix">类型简称（如 BuildPool、Card、LevelArea）。</param>
    /// <param name="displayName">显示名（可为 displayName、itemName 等）。</param>
    /// <param name="id">唯一 id（cardId、areaId、poolId、StageId 等）。</param>
    /// <param name="fallbackIfBothEmpty">显示名与 id 都空时的文件名；null 则使用 <c>{typePrefix}_</c>。</param>
    public static string BuildPreferredBaseNamePrefixDisplayId(
        string typePrefix,
        string displayName,
        string id,
        string fallbackIfBothEmpty = null)
    {
        if (string.IsNullOrWhiteSpace(typePrefix))
        {
            return string.IsNullOrWhiteSpace(fallbackIfBothEmpty) ? null : fallbackIfBothEmpty.Trim();
        }

        var p = typePrefix.Trim();
        var d = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        var i = string.IsNullOrWhiteSpace(id) ? null : id.Trim();

        if (d != null && i != null)
        {
            return $"{p}_{d}_{i}";
        }

        if (d != null)
        {
            return $"{p}_{d}";
        }

        if (i != null)
        {
            return $"{p}_{i}";
        }

        return string.IsNullOrWhiteSpace(fallbackIfBothEmpty) ? $"{p}_" : fallbackIfBothEmpty.Trim();
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Trim();
    }
#endif
}
