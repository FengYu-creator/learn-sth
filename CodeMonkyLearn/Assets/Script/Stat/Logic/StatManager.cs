using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatManager : MonoBehaviour
{
    [SerializeField] private GameObject unitPrefab;
    private UnitStatData unitStatData;
    public static StatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        unitStatData = new UnitStatData();
        RegistUnit();
    }

    void Start()
    {


    }

    void Update()
    {

    }

    public void RegistUnit()
    {
        unitStatData.Add_id_Name(0, "alpha");
        unitStatData.Add_id_Name(1, "beta");
        unitStatData.Add_id_Stats(0, 4, 4, 5, 5, 3, 3);
        unitStatData.Add_id_Stats(1, 4, 4, 5, 5, 3, 3);
    }
    public void CreatUnit(string name)
    {
        unitStatData.RegisterUnit(name);
    }
    public void PlaceUnit(int id,Vector3 position)
    {
        GameObject unitPref = Instantiate(unitPrefab, position, Quaternion.identity);
        Unit unit = unitPref.GetComponent<Unit>();
        
        string name = unitStatData.GetName(id);
        CharacterStats characterStats = unitStatData.GetCharacterStats(id);

        unit.GetUnitStat().id = id;
        unit.GetUnitStat().unitName = name;
        unit.GetUnitStat().battleStats = characterStats;

    }
    public CharacterStats GetCharacterStats(int id)
    {
        return unitStatData.GetCharacterStats(id);
    }
    public string GetUnitName(int id)
    {
        return unitStatData.GetName(id);
    }


}
