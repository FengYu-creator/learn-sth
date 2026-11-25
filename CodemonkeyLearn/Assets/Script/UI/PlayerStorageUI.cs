using TMPro;
using UnityEngine;

/// <summary>
/// 仓库 UI — 显示累计资源数量，监听 ResourceManager 变化
/// </summary>
public class PlayerStorageUI : MonoBehaviour
{
    [Header("资源显示文本")]
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI partsText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI toolsText;
    [SerializeField] private TextMeshProUGUI medicineText;

    private void Start()
    {
        RefreshAll();
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged += RefreshAll;
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged -= RefreshAll;
    }

    private void RefreshAll()
    {
        if (ResourceManager.Instance == null) return;

        if (foodText != null)     foodText.text = ResourceManager.Instance.GetResource(LootItemType.Food).ToString();
        if (partsText != null)    partsText.text = ResourceManager.Instance.GetResource(LootItemType.Parts).ToString();
        if (moneyText != null)    moneyText.text = ResourceManager.Instance.GetResource(LootItemType.Money).ToString();
        if (toolsText != null)    toolsText.text = ResourceManager.Instance.GetResource(LootItemType.Tools).ToString();
        if (medicineText != null) medicineText.text = ResourceManager.Instance.GetResource(LootItemType.Medicine).ToString();
    }
}
