using UnityEngine;
using UnityEngine.UI;

public class LevelSwitchUI : MonoBehaviour
{
    [SerializeField] private Button btnTogglePanel;   // 展开/收起关卡选择面板
    [SerializeField] private Button btnPlain;         // 平原
    [SerializeField] private Button btnSnow;          // 雪地
    [SerializeField] private Button btnDesert;        // 沙漠
    [SerializeField] private GameObject panelLevels;  // 三个关卡按钮的父物体

    private void Start()
    {
        btnTogglePanel?.onClick.AddListener(ToggleLevelPanel);
        btnPlain?.onClick.AddListener(() => SwitchToLevel(1));
        btnSnow?.onClick.AddListener(() => SwitchToLevel(2));
        btnDesert?.onClick.AddListener(() => SwitchToLevel(3));

        if (panelLevels != null)
            panelLevels.SetActive(false);
    }

    private void SwitchToLevel(int levelIndex)
    {
        LevelGrid.Instance?.SwitchToLevel(levelIndex);
        TrainController.Instance?.EnterGameMode();

        // EnterGameMode 后再显示按钮，确保面板切换已完成
        var battleStart = FindObjectOfType<BattleStart>();
        if (battleStart != null)
            battleStart.ShowButton();

        if (panelLevels != null)
            panelLevels.SetActive(false);
    }

    private void ToggleLevelPanel()
    {
        if (panelLevels != null)
            panelLevels.SetActive(!panelLevels.activeSelf);
    }
}
