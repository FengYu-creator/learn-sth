using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人模板数据库 — 管理所有 EnemyData SO 模板
/// 类似 GunDatabase
/// </summary>
public class EnemyDatabase : MonoBehaviour
{
    public static EnemyDatabase Instance { get; private set; }

    [Header("所有敌人模板 (Inspector 拖入)")]
    public List<EnemyData> enemyList = new List<EnemyData>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"[EnemyDatabase] 已加载 {enemyList.Count} 个敌人模板");
    }

    /// <summary>通过索引获取敌人模板</summary>
    public EnemyData GetEnemy(int index)
    {
        return (index >= 0 && index < enemyList.Count) ? enemyList[index] : null;
    }

    /// <summary>通过名称获取敌人模板</summary>
    public EnemyData GetEnemyByName(string name)
    {
        foreach (var e in enemyList)
        {
            if (e != null && e.enemyName == name)
                return e;
        }
        Debug.LogWarning($"[EnemyDatabase] 未找到敌人: {name}");
        return null;
    }
}
