using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using System;

public class StatManager : MonoBehaviour
{
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private GameObject enemyPrefab;
    private UnitStatData unitStatData;
    public int trainLevel = 0;
    public static StatManager Instance { get; private set; }
    public event EventHandler OnClickCreat;
    public event EventHandler OnClickPlace;
    public event EventHandler OnClickToPlacePosition;

    private Vector3 placePosition;

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

    /// <summary>
    /// 生成敌人单位（通过 EnemyData SO）
    /// 属性在生成时直接赋予，不依赖 BattleStarted 事件
    /// </summary>
    public GameObject PlaceEnemy(EnemyData data, Vector3 position)
    {
        GameObject unitPref = Instantiate(enemyPrefab, position, Quaternion.identity);
        Unit unit = unitPref.GetComponent<Unit>();

        // 标记为敌人
        unit.SetEnemy(true);

        // 设置敌人移动距离
        var moveAction = unit.GetComponent<MoveAction>();
        if (moveAction != null)
            moveAction.SetMoveDistance(data.moveDistance);

        // 注册临时属性到 UnitStat
        var unitStat = unit.GetComponent<UnitStat>();
        unitStat.id = -1;
        unitStat.unitData = new UnitData(data.enemyName,
            new CharacterStats(data.stability, data.handling, data.precision,
                               data.constitution, data.willpower, data.speed, data.baseHP),
            new System.Collections.Generic.List<Traits>());
        unitStat.enemyData = data;

        // 立即赋予敌人属性（不等战斗开始事件）
        unitStat.ApplyEnemyData();

        // 发布敌人生成事件
        Events.CallEnemySpawned(unit, data);

        Debug.Log($"[StatManager] 生成敌人: {data.enemyName} at {position}");
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
    public void  SetPlacePosition(Vector3 p)
    {
        placePosition = p;
    }
    public Vector3 GetPlacePosition()
    {
        return placePosition;
    }
    public void onClickCreat()
    {
        OnClickCreat?.Invoke(this, EventArgs.Empty);
    }
    public void onClickPlace()
    {
        OnClickPlace?.Invoke(this, EventArgs.Empty);
    }
    public void onClickToPlacePosition()
    {
        OnClickToPlacePosition?.Invoke(this, EventArgs.Empty);
    }

}
