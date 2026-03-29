using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStat : MonoBehaviour
{
    public int id;
    public string unitName;
    public CharacterStats battleStats;

    void Start()
    {
        battleStats = StatManager.Instance.GetCharacterStats(id);
        
    }


}
