using UnityEngine;

/// <summary>
/// 可搜刮点 — 挂在场景中的搜刮点预制体上
/// 用 Box Collider (IsTrigger) 检测玩家进入范围
/// </summary>
public class LootablePoint : MonoBehaviour
{
    [SerializeField] private LootItemDatabase itemDatabase;  // 该点的物品库

    [Header("视觉提示")]
    [SerializeField] private ParticleSystem highlightParticle; // 可搜刮时的粒子提示

    private bool playerInRange = false;
    private bool hasBeenLooted = false;  // 是否已被搜刮过
    private Unit playerUnit;

    public bool PlayerInRange => playerInRange;
    public bool HasBeenLooted => hasBeenLooted;
    public LootItemDatabase ItemDatabase => itemDatabase;

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenLooted) return;

        Unit unit = other.GetComponent<Unit>();
        if (unit == null)
        {
            unit = other.GetComponentInParent<Unit>();
            if (unit == null) return;
        }

        if (unit.IsEnemy()) return;

        playerInRange = true;
        playerUnit = unit;
        if (highlightParticle != null)
            highlightParticle.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Unit>(out var unit) && !unit.IsEnemy())
        {
            playerInRange = false;
            playerUnit = null;
        }
    }

    /// <summary>标记为已搜刮，禁用该点</summary>
    public void MarkAsLooted()
    {
        hasBeenLooted = true;
        playerInRange = false;
        playerUnit = null;
        if (highlightParticle != null)
            highlightParticle.Stop();
        gameObject.SetActive(false);
    }

    /// <summary>重置搜刮点（切换关卡时调用）</summary>
    public void ResetPoint()
    {
        hasBeenLooted = false;
        playerInRange = false;
        playerUnit = null;
        if (highlightParticle != null)
            highlightParticle.Stop();
        gameObject.SetActive(true);
    }

    /// <summary>获取附近的玩家单位</summary>
    public Unit GetPlayerUnit() => playerUnit;
}
