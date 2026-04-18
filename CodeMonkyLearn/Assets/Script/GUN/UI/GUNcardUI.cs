using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUNcardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gunNameText;
    [SerializeField] private TextMeshProUGUI whoHaveText;
    [SerializeField] private TextMeshProUGUI gunLevelText;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;

    /// <summary>当前卡片对应的枪仓库ID</summary>
    private int gunId = -1;

    /// <summary>卡片数据变更时调用（由 GunInventoryUI 调用）</summary>
    public void SetGunData(int gunInventoryId)
    {
        gunId = gunInventoryId;

        if (!GunManager.Instance.TryGetGun(gunId, out var gun))
        {
            Debug.LogWarning($"[GUNcardUI] 枪ID={gunId} 不存在");
            return;
        }

        gunNameText.text = gun.template.gunName;
        gunLevelText.text = $"Lv.{gun.equippedAttachments.Count}";

        RefreshOwnerText();

        // 绑定事件
        equipButton.onClick.RemoveAllListeners();
        unequipButton.onClick.RemoveAllListeners();
        equipButton.onClick.AddListener(OnEquipClicked);
        unequipButton.onClick.AddListener(OnUnequipClicked);
    }

    private void OnEquipClicked()
    {
        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit == null)
        {
            Debug.LogWarning("[GUNcardUI] 没有选中角色，无法装备!");
            return;
        }

        UnitStat stat = selectedUnit.GetComponent<UnitStat>();
        if (stat == null)
        {
            Debug.LogWarning("[GUNcardUI] 选中角色没有 UnitStat!");
            return;
        }

        // 检查角色是否已有枪
        int currentGunId = GunManager.Instance.GetAssignedGunId(stat.id);
        if (currentGunId >= 0)
        {
            Debug.LogWarning($"[GUNcardUI] 角色 {stat.unitData.name} 已装备枪 ID={currentGunId}，请先卸下!");
            return;
        }

        GunManager.Instance.AssignGun(stat.id, gunId);
        RefreshOwnerText();
    }

    private void OnUnequipClicked()
    {
        int ownerUnitId = GunManager.Instance.GetAssignedOwnerUnitId(gunId);
        if (ownerUnitId < 0) return;

        GunManager.Instance.UnassignGun(ownerUnitId);
        RefreshOwnerText();
    }

    /// <summary>刷新持有者文字和按钮</summary>
    public void RefreshOwnerText()
    {
        int ownerUnitId = GunManager.Instance.GetAssignedOwnerUnitId(gunId);

        if (ownerUnitId >= 0)
        {
            // 通过 StatManager 获取名字（战前）
            string unitName = StatManager.Instance.GetUnitData(ownerUnitId).name;
            whoHaveText.text = unitName;
            unequipButton.gameObject.SetActive(true);
            equipButton.gameObject.SetActive(false);
        }
        else
        {
            whoHaveText.text = "仓库中";
            unequipButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(true);
        }
    }
}
