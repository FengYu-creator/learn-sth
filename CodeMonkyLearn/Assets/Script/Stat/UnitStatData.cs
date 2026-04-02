using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.ParticleSystem;


[System.Serializable]
public struct UnitData
{
    public string name;
    public CharacterStats stats;
    public List<Traits> traits;

    public UnitData(string name, CharacterStats stats, List<Traits> traits)
    {
        this.name = name;
        this.stats = stats;
        this.traits = traits;
    }
}

public class UnitStatData
{

    Dictionary<int, UnitData> id_UnitData = new Dictionary<int, UnitData>();

    public int RegisterUnit(string name,int stability, int handling, int precision, int constitution, int willpower, int speed,Traits traits1, Traits traits2)
    {
        int id = id_UnitData.Count;
        id_UnitData.Add(id, new UnitData(name, new CharacterStats(stability,handling,precision,constitution,willpower,speed), new List<Traits>() { traits1,traits2}));
        return id;
    }

    public CharacterStats GetCharacterStats(int id)
    {
        return id_UnitData.TryGetValue(id, out var data) ? data.stats : default;
    }

    public UnitData GetUnitData(int id)
    {
        return id_UnitData.TryGetValue(id, out var data) ? data : default;
    }

    public int GetUnitNumber()
    {
        return id_UnitData.Count;
    }

    public void Add_id_UnitData(int id, string name,int stability, int handling, int precision, int constitution, int willpower, int speed)
    {
            id_UnitData.Add(id, new UnitData(name, new CharacterStats(stability, handling, precision, constitution, willpower, speed), new List<Traits>()));
    }
    public void Add_id_UnitData_Traits(int id, Traits traits)
    {
        var data = id_UnitData[id];//UnitData是构造体，直接修改会作用在值类型的副本上
        data.traits.Add(traits);
        id_UnitData[id] = data;

    }
    public List<string> namePool = new List<string>
    {
    "Clint", "Dweller", "MacReady", "Drifter", "Solo", "奶龙", "Man！", "Ranger", "Nomad", "Ash", "Dr_Braun", "Zimmer", "Marrow", "Archimedes", "Faker", "Elijah", "Chronos", "Vector", "Miku", "Artyom", "牛头人", "Hansa", "Rust", "Echo", "D6", "Chernobyl", "Spetsnaz", "狗蛋", "Train", "Leon", "Ada", "Chris", "Sergei", "碍事梨", "Victor", "Ghost", "Lilith", "Zero", "Dante", "Popsicle", "Postman", "LoneWolf", "GrayFox", "Sam", "Ethan", "Carlos"
    };

}



public enum StatName
{
    //   稳定     操控  精准              体质     意志   速度
    Stability,Handling,Precision, Constitution,Willpower,Speed
}
public enum Traits
{
    Strong,
    Agile,
    Brave,
    MagicResist,
    Lazy,
    SteelArm,
    Thrifty
}

[System.Serializable]
public struct CharacterStats
{
    public int Stability;
    public int Handling;
    public int Precision;
    public int Constitution;
    public int Willpower;
    public int Speed;

    public CharacterStats(int stability,int handling,int precision,int constitution,int willpower,int speed)
    {
        this.Stability = stability;
        this.Handling = handling;
        this.Precision = precision;
        this.Constitution = constitution;
        this.Willpower = willpower;
        this.Speed = speed;
    }
    public void SetStats(int stability, int handling, int precision, int constitution, int willpower, int speed)
    {
        this.Stability = stability;
        this.Handling = handling;
        this.Precision = precision;
        this.Constitution = constitution;
        this.Willpower = willpower;
        this.Speed = speed;
    }
    public void AddManyStats(int stability, int handling, int precision, int constitution, int willpower, int speed)
    {
        this.Stability += stability;
        this.Handling += handling;
        this.Precision += precision;
        this.Constitution += constitution;
        this.Willpower += willpower;
        this.Speed += speed;
    }
    public CharacterStats AddStat(StatName statName, int addCount)
    {
        var clone = this;
        switch (statName)
        {
            case StatName.Stability: clone.Stability += addCount; break;
            case StatName.Handling: clone.Handling += addCount; break;
            case StatName.Precision: clone.Precision += addCount; break;
            case StatName.Constitution: clone.Constitution += addCount; break;
            case StatName.Willpower: clone.Willpower += addCount; break;
            case StatName.Speed: clone.Speed += addCount; break;
        }
        return clone;
    }
    public int GetStatValue(StatName statName)
    {
        return statName switch
        {
            StatName.Stability => Stability,
            StatName.Handling => Handling,
            StatName.Precision => Precision,
            StatName.Constitution => Constitution,
            StatName.Willpower => Willpower,
            StatName.Speed => Speed,
            _ => 0
        };
    }

}




    