using Cysharp.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 编队应用服务接口。
/// </summary>
public interface IFleetService
{
    UniTask<List<string>> LoadGroupAsync(int groupIndex);
    UniTask SaveGroupAsync(int groupIndex, List<string> cardInstanceIds);

    /// <summary>
    /// 加载全部编队（序号/名称/成员实例 ID），供战斗前编队选择列表使用。
    /// 存档中尚无编队时返回两个空编队占位，保证界面始终有可选条目。
    /// </summary>
    UniTask<List<FleetGroupData>> LoadAllGroupsAsync();

    /// <summary>
    /// 保存指定编队的名称（重命名）；编队不存在时自动创建。
    /// </summary>
    /// <param name="groupIndex">编队序号</param>
    /// <param name="groupName">新名称（空白则回退默认"编队 N"）</param>
    UniTask SaveGroupNameAsync(int groupIndex, string groupName);

    /// <summary>
    /// 新增一个空编队，序号 = 当前最大序号 + 1（无编队时为 0），返回新编队序号。
    /// </summary>
    UniTask<int> CreateGroupAsync();
}