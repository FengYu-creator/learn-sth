using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单位血条 UI（世界空间，挂载在单位预制体上）
/// 用 Image.fillAmount 显示血量，自动朝向摄像头
/// </summary>
public class UnitHealthBarUI : MonoBehaviour
{
    [Header("血条 UI")]
    [SerializeField] private Image hpFillImage;

    private HealthSystem healthSystem;

    private void Start()
    {
        healthSystem = GetComponentInParent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogWarning($"[UnitHealthBarUI] {gameObject.name} 未找到 HealthSystem 组件");
            return;
        }

        // 初始化时可能还没 Init，等 Init 完成再赋值
        if (healthSystem.IsInitialized())
            Refresh();

        healthSystem.OnHealthInitialized += OnHealthChanged;
        healthSystem.OnDamaged += OnDamaged;
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthInitialized -= OnHealthChanged;
            healthSystem.OnDamaged -= OnDamaged;
        }
    }

    private void Update()
    {
        // 世界空间 UI 始终朝向摄像头
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }

    private void OnHealthChanged(int hp, int max)
    {
        Refresh();
    }

    private void OnDamaged(Unit attacker, int damage, int currentHP)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (hpFillImage != null && healthSystem != null)
            hpFillImage.fillAmount = healthSystem.GetHPPercent();
    }
}
