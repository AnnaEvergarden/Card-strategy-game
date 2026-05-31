using System.IO;

/// <summary>
/// 存储层共享工具。
/// </summary>
internal static class StoreUtil
{
    /// <summary>
    /// 安全写入：先写入临时文件再移至目标路径，备份旧文件避免中途崩溃导致数据丢失。
    /// </summary>
    internal static void AtomicWrite(string path, byte[] data)
    {
        var tempPath = path + ".tmp";
        var backupPath = path + ".bak";
        File.WriteAllBytes(tempPath, data);
        if (File.Exists(path))
        {
            File.Delete(backupPath);
            File.Move(path, backupPath);
        }

        File.Move(tempPath, path);
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }
    }

    /// <summary>
    /// 检查磁盘上是否存在有效数据文件（长度 > 16 表示至少包含有效 IV + 密文）。
    /// </summary>
    internal static bool HasValidDataOnDisk(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            return new FileInfo(filePath).Length > 16;
        }
        catch
        {
            return false;
        }
    }
}
