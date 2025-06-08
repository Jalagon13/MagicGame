using System.Collections.Generic;
using UnityEngine;

public class CharacterStats
{
    public Stat MaxHealth { get; }
    public Stat Defense { get; }
    public Stat MovementSpeed { get; }

    private List<Buff> _activeBuffs = new();

    public CharacterStats(CharacterDataSO data)
    {
        MaxHealth = new Stat(data.BaseHP);
        Defense = new Stat(data.BaseDefense);
        MovementSpeed = new Stat(data.BaseSpeed);
    }

    public void AddBuff(Buff buff)
    {
        Debug.Log($"Applying buff: {buff.Source}");
        buff.Apply();
        _activeBuffs.Add(buff);
    }

    public void RemoveBuffsFromSource(object source)
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = _activeBuffs[i];

            if (buff.Source == source)
            {
                Debug.Log($"Found buff to remove: {buff.Source}");
                buff.Remove();
                _activeBuffs.RemoveAt(i);
            }
        }
    }

    public void TickBuffs(float deltaTime)
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            Buff buff = _activeBuffs[i];
            if (buff.IsPermanent) continue;
            
            buff.Tick(deltaTime);
            if (buff.IsExpired)
            {
                buff.Remove();
                _activeBuffs.RemoveAt(i);
            }
        }
    }
}