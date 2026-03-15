using UnityEngine;

public class TitlePanel : BaseUIPanel
{
    [Header("引用面板")]
    [Tooltip("需要引用的面板名称")]
    [SerializeField] public string _LoginPanelName;

    [SerializeField] private AccountManager accountManager;

    public void OnClickLogin()
    {
        UIPanelManager.Instance.ShowPanel(_LoginPanelName);
    }

    public void OnClickLogout()
    {
        if (accountManager == null) return;
        accountManager.Logout();
    }

    public void OnClickSetting()
    {
        //----------------------------待添加
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
