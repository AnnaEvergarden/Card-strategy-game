using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 建造界面中「当前卡池详情」区块：横幅、名称、消耗说明、卡池切换按钮容器、单抽/十连按钮。
/// 由 <see cref="BuildPanel"/> 在运行时实例化预制体并绑定数据。
/// </summary>
public sealed class BuildPoolDetailView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 当前卡池展示图。
    /// </summary>
    [SerializeField] private Image poolBannerImage;

    /// <summary>
    /// 当前卡池名称。
    /// </summary>
    [SerializeField] private TMP_Text poolNameText;

    /// <summary>
    /// 当前卡池消耗说明（单抽/十连摘要）。
    /// </summary>
    [SerializeField] private TMP_Text poolCostText;

    /// <summary>
    /// 可选卡池按钮的父节点（建议挂 VerticalLayoutGroup）。
    /// </summary>
    [SerializeField] private RectTransform poolButtonRoot;

    /// <summary>
    /// 建造一次按钮。
    /// </summary>
    [SerializeField] private Button buildOneBtn;

    /// <summary>
    /// 建造十次按钮。
    /// </summary>
    [SerializeField] private Button buildTenBtn;

    #endregion

    #region Public API（供 BuildPanel 绑定）

    /// <summary>
    /// 卡池横幅图组件。
    /// </summary>
    public Image PoolBannerImage => poolBannerImage;

    /// <summary>
    /// 卡池名称文本。
    /// </summary>
    public TMP_Text PoolNameText => poolNameText;

    /// <summary>
    /// 卡池消耗说明文本。
    /// </summary>
    public TMP_Text PoolCostText => poolCostText;

    /// <summary>
    /// 卡池切换按钮挂载点。
    /// </summary>
    public RectTransform PoolButtonRoot => poolButtonRoot;

    /// <summary>
    /// 单抽按钮。
    /// </summary>
    public Button BuildOneButton => buildOneBtn;

    /// <summary>
    /// 十连按钮。
    /// </summary>
    public Button BuildTenButton => buildTenBtn;

    #endregion
}
