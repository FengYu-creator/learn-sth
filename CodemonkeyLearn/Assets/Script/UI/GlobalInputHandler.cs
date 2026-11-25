using UnityEngine;

/// <summary>
/// 全局输入处理器 — 挂在一个始终激活的 GameObject 上
/// 负责监听全局快捷键（如 M 键切换主菜单）
/// </summary>
public class GlobalInputHandler : MonoBehaviour
{
    [SerializeField] private MainMenuUI mainMenuUI;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (mainMenuUI != null)
                mainMenuUI.ToggleMenu();
        }
    }
}
