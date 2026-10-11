/// <summary>
/// ShipFaction 扩展方法：获取中文阵营显示名称。
/// </summary>
public static class ShipFactionExtensions
{
    public static string GetLocalizedName(this ShipFaction faction)
    {
        return faction switch
        {
            ShipFaction.EagleUnion        => "白鹰联邦",
            ShipFaction.RoyalNavy         => "皇家海军",
            ShipFaction.SakuraEmpire      => "重樱",
            ShipFaction.IronBlood         => "铁血",
            ShipFaction.DragonEmpery      => "东煌",
            ShipFaction.NorthernParliament => "北方联合",
            ShipFaction.IrisOrthodoxy     => "鸢尾教国",
            ShipFaction.VichyaDominion    => "维希教廷",
            ShipFaction.SardegnaEmpire    => "撒丁帝国",
            _                             => "其他",
        };
    }
}
