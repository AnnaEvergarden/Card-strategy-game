using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// Addressables 批量重命名工具。
/// 同时重命名文件和 Address，保持文件系统与 Addressables 注册表一致。
///
/// 用法：
///   1. Project 窗口选中资源（可多选）
///   2. Tools > Addressables > Batch Rename
///   3. 设置前缀/后缀/替换规则
///   4. 预览 → 执行
/// </summary>
public sealed class AddressablesBatchRenamer : EditorWindow
{
    private string _prefix = "";
    private string _suffix = "";
    private string _replaceOld = "";
    private string _replaceNew = "";
    private bool _dryRun = true;
    private bool _renameFiles = true;

    private Vector2 _scrollPos;
    private List<RenamePreview> _previews = new();

    [MenuItem("Tools/Addressables/Batch Rename", priority = 210)]
    public static void Open()
    {
        var window = GetWindow<AddressablesBatchRenamer>("Batch Rename");
        window.minSize = new Vector2(560, 360);
        window.Show();
    }

    private void OnEnable() => RefreshPreviews();

    private void OnGUI()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorGUILayout.HelpBox("未找到 AddressableAssetSettings。", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"选中: {Selection.objects.Length} 个资源", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("重命名规则", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        _prefix = EditorGUILayout.TextField("前缀", _prefix);
        _suffix = EditorGUILayout.TextField("后缀", _suffix);

        EditorGUILayout.Space(2);
        _replaceOld = EditorGUILayout.TextField("查找文本", _replaceOld);
        _replaceNew = EditorGUILayout.TextField("替换为", _replaceNew);

        EditorGUILayout.Space(4);
        _renameFiles = EditorGUILayout.Toggle("同时重命名文件", _renameFiles);
        _dryRun = EditorGUILayout.Toggle("预览模式", _dryRun);

        if (EditorGUI.EndChangeCheck()) RefreshPreviews();

        EditorGUILayout.Space(4);

        if (_previews.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "在 Project 窗口选中资源后在此设置。\n" +
                "• 同时重命名文件 — 勾选后文件名和 Address 同步修改\n" +
                "• 预览模式 — 只显示效果，不实际修改\n\n" +
                "示例: 选中所有舰娘图标 → 前缀 icon_ → Laffey.png → icon_Laffey.png",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"预览 ({_previews.Count} 项)", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));

            foreach (var p in _previews)
            {
                EditorGUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(p.Warning))
                    GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(20));

                // 文件名变更
                if (_renameFiles && p.OldFileName != p.NewFileName)
                {
                    EditorGUILayout.LabelField(p.OldFileName, EditorStyles.miniLabel, GUILayout.Width(140));
                    GUILayout.Label("→", GUILayout.Width(14));
                    var s = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
                    EditorGUILayout.LabelField(p.NewFileName, s, GUILayout.Width(140));
                }
                else
                {
                    EditorGUILayout.LabelField(p.OldFileName, EditorStyles.miniLabel, GUILayout.Width(140));
                    GUILayout.Label("", GUILayout.Width(158));
                }

                // Address 变更
                if (p.OldAddress != p.NewAddress)
                {
                    GUILayout.Label("Addr:", EditorStyles.miniLabel, GUILayout.Width(32));
                    EditorGUILayout.LabelField(p.OldAddress, EditorStyles.miniLabel, GUILayout.Width(100));
                    GUILayout.Label("→", GUILayout.Width(14));
                    var s2 = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
                    EditorGUILayout.LabelField(p.NewAddress, s2, GUILayout.Width(120));
                }

                if (!string.IsNullOrEmpty(p.Warning))
                {
                    var ws = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.yellow } };
                    EditorGUILayout.LabelField(p.Warning, ws);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4);

            var hasConflicts = _previews.Exists(p => !string.IsNullOrEmpty(p.Warning));
            EditorGUI.BeginDisabledGroup(hasConflicts && !_dryRun);
            if (GUILayout.Button(_dryRun ? "预览" : "执行重命名", GUILayout.Height(30)))
            {
                if (_dryRun)
                    EditorUtility.DisplayDialog("预览模式", "取消 \"预览模式\" 勾选后点击按钮即可执行。", "确定");
                else
                    ApplyRename();
            }
            EditorGUI.EndDisabledGroup();
        }
    }

    private void RefreshPreviews()
    {
        _previews.Clear();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        // 收集已存在的地址和文件名
        var usedAddrs = new HashSet<string>();
        var usedFileNames = new HashSet<string>();
        foreach (var group in settings.groups)
        {
            if (group == null) continue;
            foreach (var entry in group.entries)
            {
                if (!string.IsNullOrEmpty(entry.address))
                    usedAddrs.Add(entry.address);
            }
        }

        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) continue;

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null) continue;

            var oldFileName = Path.GetFileName(path);
            var oldNameNoExt = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            var oldAddr = entry.address;

            // 应用替换
            var newName = oldNameNoExt;
            var newAddr = oldAddr;
            if (!string.IsNullOrEmpty(_replaceOld))
            {
                newName = newName.Replace(_replaceOld, _replaceNew);
                newAddr = newAddr.Replace(_replaceOld, _replaceNew);
            }

            // 应用前缀/后缀
            newName = _prefix + newName + _suffix;
            newAddr = _prefix + newAddr + _suffix;

            var newFileName = newName + ext;
            if (newFileName == oldFileName && newAddr == oldAddr) continue;

            // 冲突检测
            var warning = "";
            if (newAddr != oldAddr && usedAddrs.Contains(newAddr))
                warning = "Address 冲突";
            if (_renameFiles && newFileName != oldFileName)
            {
                var newPath = Path.Combine(Path.GetDirectoryName(path), newFileName);
                if (File.Exists(newPath) && newPath != path)
                    warning = (string.IsNullOrEmpty(warning) ? "" : warning + " / ") + "文件名冲突";
            }

            _previews.Add(new RenamePreview
            {
                AssetPath = path,
                OldFileName = oldFileName,
                NewFileName = _renameFiles ? newFileName : oldFileName,
                OldAddress = oldAddr,
                NewAddress = newAddr,
                Entry = entry,
                Warning = warning,
            });
        }
    }

    private void ApplyRename()
    {
        var fileCount = 0;
        var addrCount = 0;
        var skipped = 0;

        foreach (var p in _previews)
        {
            if (!string.IsNullOrEmpty(p.Warning))
            {
                skipped++;
                continue;
            }

            // 重命名文件
            if (p.NewFileName != p.OldFileName)
            {
                var dir = Path.GetDirectoryName(p.AssetPath);
                var newPath = Path.Combine(dir, p.NewFileName);
                var error = AssetDatabase.RenameAsset(p.AssetPath, p.NewFileName);
                if (string.IsNullOrEmpty(error))
                {
                    fileCount++;
                    p.AssetPath = newPath; // 更新路径以便后续操作
                }
                else
                {
                    Debug.LogError($"[BatchRenamer] 文件重命名失败: {p.AssetPath} → {p.NewFileName}: {error}");
                }
            }

            // 重命名 Address
            if (p.NewAddress != p.OldAddress)
            {
                p.Entry.SetAddress(p.NewAddress);
                addrCount++;
                Debug.Log($"[BatchRenamer] Address: {p.OldAddress} → {p.NewAddress}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var msg = $"文件重命名: {fileCount}\nAddress 重命名: {addrCount}";
        if (skipped > 0) msg += $"\n因冲突跳过: {skipped}";
        EditorUtility.DisplayDialog("完成", msg, "确定");

        RefreshPreviews();
        Repaint();
    }

    private class RenamePreview
    {
        public string AssetPath;
        public string OldFileName;
        public string NewFileName;
        public string OldAddress;
        public string NewAddress;
        public AddressableAssetEntry Entry;
        public string Warning;
    }
}
