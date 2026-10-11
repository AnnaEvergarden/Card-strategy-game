using NUnit.Framework;

/// <summary>
/// UnitId 工具单元测试：生成与解析格式。
/// </summary>
public class UnitIdTests
{
    /// <summary>Build 生成 "P1_0" 格式字符串。</summary>
    [Test]
    public void Build_ReturnsSideAndIndexFormat()
    {
        Assert.AreEqual("P1_0", UnitId.Build(BattleSide.P1, 0));
        Assert.AreEqual("P2_5", UnitId.Build(BattleSide.P2, 5));
    }

    /// <summary>合法 UnitId 可正确解析出阵营与序号。</summary>
    [Test]
    public void TryParse_ValidId_ParsesSideAndIndex()
    {
        var ok = UnitId.TryParse("P2_3", out var side, out var index);

        Assert.IsTrue(ok);
        Assert.AreEqual(BattleSide.P2, side);
        Assert.AreEqual(3, index);
    }

    /// <summary>非法输入（空、缺段、未知阵营、负序号）解析失败。</summary>
    [Test]
    public void TryParse_InvalidId_ReturnsFalse()
    {
        Assert.IsFalse(UnitId.TryParse(null, out _, out _));
        Assert.IsFalse(UnitId.TryParse(string.Empty, out _, out _));
        Assert.IsFalse(UnitId.TryParse("P1", out _, out _));
        Assert.IsFalse(UnitId.TryParse("P1_", out _, out _));
        Assert.IsFalse(UnitId.TryParse("_0", out _, out _));
        Assert.IsFalse(UnitId.TryParse("P3_0", out _, out _));
        Assert.IsFalse(UnitId.TryParse("P1_abc", out _, out _));
    }
}
