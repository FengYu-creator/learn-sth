using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStat : MonoBehaviour
{
    public int id;
    public string unitName;
    private CharacterStats battleStats;
    public UnitData unitData;

    void Start()
    {
        unitData = StatManager.Instance.GetUnitData(id);
        battleStats = unitData.stats;
        unitName = unitData.name;
    }


}
