using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂到任意 Button 上，自动在点击时播放 UI 点击音效
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => Events.CallUIClick());
    }
}
