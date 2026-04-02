using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using System;

public class StatManager : MonoBehaviour
{
    [SerializeField] private GameObject unitPrefab;
    private UnitStatData unitStatData;
    public int trainLevel = 0;
    public static StatManager Instance { get; private set; }
    public event EventHandler OnClickCreat;
    public event EventHandler OnClickPlace;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        unitStatData = new UnitStatData();
        RegistTestUnit();
    }

    public void RegistTestUnit()
    {
        unitStatData.RegisterUnit("alpha", 9, 9, 5, 5, 3, 3,Traits.Strong,Traits.Brave);
        unitStatData.RegisterUnit("beta", 4, 4, 5, 5, 3, 3, Traits.Agile, Traits.Lazy);

    }

    public void CreatUnit(string name, int stability, int handling, int precision, int constitution, int willpower, int speed,Traits traits1,Traits traits2)
    {

        unitStatData.RegisterUnit(name,stability,handling, precision,  constitution,  willpower, speed, traits1,  traits2);
        Debug.Log("创建角色" + name);
    }
    public GameObject PlaceUnit(int id, Vector3 position)
    {
        GameObject unitPref = Instantiate(unitPrefab, position, Quaternion.identity);
        Unit unit = unitPref.GetComponent<Unit>();

        UnitData unitdata = unitStatData.GetUnitData(id);

        unit.GetUnitStat().id = id;
        unit.GetUnitStat().unitData = unitdata;
        return unitPref;
    }
    public UnitData GetUnitData(int id)
    {
       return unitStatData.GetUnitData(id);
    }
    public int GetUnitNumber()
    {
        return unitStatData.GetUnitNumber();
    }
    public List<string> GetNamePool()
    {
        return unitStatData.namePool;
    }
    public void onClickCreat()
    {
        OnClickCreat?.Invoke(this, EventArgs.Empty);
    }
    public void onClickPlace()
    {
        OnClickPlace?.Invoke(this, EventArgs.Empty);
    }

}
