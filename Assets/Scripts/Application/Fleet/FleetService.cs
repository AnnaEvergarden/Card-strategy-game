using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 编队应用服务实现。
/// </summary>
public class FleetService : IFleetService
{
    private readonly IFleetRepository _repository;

    public FleetService(IFleetRepository repository)
    {
        _repository = repository;
    }

    public async UniTask<List<string>> LoadGroupAsync(int groupIndex)
    {
        var data = await _repository.LoadAsync();
        var group = data.Groups.FirstOrDefault(g => g.GroupIndex == groupIndex);
        return group?.CardInstanceIds ?? new List<string>();
    }

    public async UniTask SaveGroupAsync(int groupIndex, List<string> cardInstanceIds)
    {
        var data = await _repository.LoadAsync();
        var group = data.Groups.FirstOrDefault(g => g.GroupIndex == groupIndex);
        if (group == null)
        {
            group = new FleetGroupData { GroupIndex = groupIndex, GroupName = $"编队 {groupIndex + 1}" };
            data.Groups.Add(group);
        }
        group.CardInstanceIds = cardInstanceIds ?? new List<string>();
        _repository.MarkDirty();
        await _repository.SaveAsync();
    }

    /// <summary>
    /// 加载全部编队；存档为空时返回两个空编队占位（与历史 FleetStore 的默认编队数一致）。
    /// </summary>
    public async UniTask<List<FleetGroupData>> LoadAllGroupsAsync()
    {
        var data = await _repository.LoadAsync();
        if (data.Groups == null)
            data.Groups = new List<FleetGroupData>();

        if (data.Groups.Count == 0)
        {
            data.Groups.Add(new FleetGroupData { GroupIndex = 0, GroupName = "编队 1" });
            data.Groups.Add(new FleetGroupData { GroupIndex = 1, GroupName = "编队 2" });
        }

        return data.Groups;
    }

    /// <summary>
    /// 保存指定编队名称（重命名）；编队不存在时自动创建。
    /// </summary>
    public async UniTask SaveGroupNameAsync(int groupIndex, string groupName)
    {
        var data = await _repository.LoadAsync();
        if (data.Groups == null)
            data.Groups = new List<FleetGroupData>();

        var name = string.IsNullOrWhiteSpace(groupName) ? $"编队 {groupIndex + 1}" : groupName.Trim();
        var group = data.Groups.FirstOrDefault(g => g.GroupIndex == groupIndex);
        if (group == null)
        {
            group = new FleetGroupData { GroupIndex = groupIndex, GroupName = name };
            data.Groups.Add(group);
        }
        else
        {
            group.GroupName = name;
        }

        _repository.MarkDirty();
        await _repository.SaveAsync();
    }

    /// <summary>
    /// 新增空编队：序号 = 当前最大序号 + 1（无编队时为 0），返回新序号。
    /// </summary>
    public async UniTask<int> CreateGroupAsync()
    {
        var data = await _repository.LoadAsync();
        if (data.Groups == null)
            data.Groups = new List<FleetGroupData>();

        var maxIndex = -1;
        for (var i = 0; i < data.Groups.Count; i++)
        {
            if (data.Groups[i].GroupIndex > maxIndex)
                maxIndex = data.Groups[i].GroupIndex;
        }

        var newIndex = maxIndex + 1;
        data.Groups.Add(new FleetGroupData { GroupIndex = newIndex, GroupName = $"编队 {newIndex + 1}" });
        _repository.MarkDirty();
        await _repository.SaveAsync();
        return newIndex;
    }
}