using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class UnitStatData
{
    private int id;
    Dictionary<int,CharacterStats>id_Stats = new Dictionary<int,CharacterStats>();
    Dictionary<int,string>id_Name = new Dictionary<int,string>();
    Dictionary<int,Traits>id_Traits = new Dictionary<int,Traits>();


    public void RegisterUnit(string name)
    {
        id = id_Stats.Count; 
        id_Stats.Add(id, new CharacterStats(4,4,5,5,3,3));
        id_Name.Add(id, name);
    }
    public string  GetName(int id)
    {
        return id_Name[id];
    }
    public CharacterStats GetCharacterStats(int id)
    {
        return id_Stats[id];
    }
    public void Add_id_Name(int id, string name)
    {
        id_Name[id] = name;
    }
    public void Add_id_Stats(int id, int stability, int handling, int precision, int constitution, int willpower, int speed)
    {
        id_Stats.Add(id, new CharacterStats(stability, handling, precision, constitution, willpower, speed));
    }


}



public enum StatName
{
    //   稳定     操控  精准              体质     意志   速度
    Stability,Handling,Precision, Constitution,Willpower,Speed
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
    public int GetStat(StatName statName)
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








public struct Traits
{

}