using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSelectPanel : BaseUIPanel
{
    [SerializeField] private Transform container;
    [SerializeField] private GameObject slotPrefab;

    [SerializeField] private SaveManager saveManager;

    private void Start()
    {
        for(int i = container.childCount - 1; i > 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        int count = SaveManager.SaveSlotCount;
        for (int i = 0; i < count && i < SaveManager.SaveSlotCount; i++)
        {
            int slotIndex = i;

            var go = GameObject.Instantiate(slotPrefab, container);
            go.name = $"SaveSlot_{slotIndex}";

            Button btn = go.GetComponent<Button>();
            TMP_Text info = go.GetComponentInChildren<TMP_Text>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnClickSlot(slotIndex));
            }
            if(info != null)
            {
                string hasSave = saveManager.HasSaveInSlot(slotIndex) ? "有" : "无";
                info.text = $"存档位置 : {slotIndex};" +
                    $"\n是否有存档 {hasSave}";
            }
        }
    }

    private void OnClickSlot(int _slotIndex)
    {
        if (saveManager == null)
        {
            Debug.LogWarning("SaveManager为空！");
            return;
        }

        if (!saveManager.HasSaveInSlot(_slotIndex))
        {
            saveManager.CreateDefaultSaveInSlot(_slotIndex);
            Debug.LogWarning($"该位置没有存档! 将创建默认存档。");
            return;
        }

        saveManager.SelectSlot(_slotIndex); //-----------------Load()有问题

        Debug.Log("即将加载游戏场景...");
        SceneManager.LoadScene("GameScene");  //------------------
    }
}
