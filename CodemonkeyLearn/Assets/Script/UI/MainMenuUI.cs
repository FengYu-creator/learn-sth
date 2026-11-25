using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主界面控制器
/// 开始游戏 / 设置面板（音量 & 音效 Slider）/ 退出游戏
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("—— 按钮 ——")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("—— 设置面板 ——")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button closeSettingsButton;

    private void Start()
    {
        startButton?.onClick.AddListener(OnStartGame);
        settingsButton?.onClick.AddListener(OnToggleSettings);
        quitButton?.onClick.AddListener(OnQuitGame);

        // 初始隐藏设置面板
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // 初始化 Slider 值为当前音量
        if (AudioManager.Instance != null)
        {
            if (bgmVolumeSlider != null)
                bgmVolumeSlider.value = AudioManager.Instance.BgmVolume;
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = AudioManager.Instance.SfxVolume;
        }

        // 绑定 Slider 变化
        bgmVolumeSlider?.onValueChanged.AddListener(OnBgmVolumeChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSfxVolumeChanged);

        // 绑定关闭设置按钮
        closeSettingsButton?.onClick.AddListener(CloseSettings);
    }

    /// <summary>关闭设置面板</summary>
    private void CloseSettings()
    {
        Events.CallUIClick();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ToggleMenu()
    {
        Events.CallUIClick();
        bool isActive = gameObject.activeSelf;
        gameObject.SetActive(!isActive);
        // 打开菜单时关闭设置面板
        if (!isActive && settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnStartGame()
    {
        // 播放点击音效
        Events.CallUIClick();
        // 关闭主界面父物体
        gameObject.SetActive(false);
    }

    private void OnToggleSettings()
    {
        Events.CallUIClick();
        if (settingsPanel != null)
        {
            // 设置面板打开时只允许通过关闭按钮收起，防止 Slider 拖动穿透误触
            if (settingsPanel.activeSelf)
                return;
            settingsPanel.SetActive(true);
        }
    }

    private void OnQuitGame()
    {
        Events.CallUIClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBgmVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSfxVolume(value);
    }
}
