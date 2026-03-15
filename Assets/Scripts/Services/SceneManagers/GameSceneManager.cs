using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    private void Start()
    {
        UIPanelManager.Instance.ShowPanel(UIPanelNames.Battle);
    }
}
