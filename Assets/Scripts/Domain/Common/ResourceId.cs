/// <summary>
/// 资源标识符：业务层通过此 ID 表达资源需求，不包含具体路径信息。
/// ResourceService 内部通过 ResourceConfig 映射为 Addressables 地址进行加载。
/// 隐式支持与 int 的互相转换，便于配置表中直接赋值。
/// </summary>
[System.Serializable]
public struct ResourceId
{
    /// <summary>
    /// 资源标识数值。
    /// </summary>
    public int Value;

    /// <summary>
    /// 使用指定数值构造资源标识符。
    /// </summary>
    public ResourceId(int value)
    {
        Value = value;
    }

    /// <summary>
    /// int 到 ResourceId 的隐式转换。
    /// </summary>
    public static implicit operator ResourceId(int value) => new ResourceId(value);

    /// <summary>
    /// ResourceId 到 int 的隐式转换。
    /// </summary>
    public static implicit operator int(ResourceId id) => id.Value;

    /// <summary>
    /// 返回字符串表示。
    /// </summary>
    public override string ToString() => $"ResourceId({Value})";
}
