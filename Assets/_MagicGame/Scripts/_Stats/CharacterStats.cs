using System.Collections.Generic;

public class ServerCharacterStats
{
    public Stat MaxHealth { get; }
    public Stat Defense { get; }
    public Stat MovementSpeed { get; }

    private List<Buff> _activeBuffs = new();

    public ServerCharacterStats(CharacterDataSO data)
    {
        MaxHealth = new Stat(data.BaseHP);
        Defense = new Stat(data.BaseDefense);
        MovementSpeed = new Stat(data.BaseSpeed);
    }

    public void AddBuff(Buff buff)
    {
        buff.Apply();
        _activeBuffs.Add(buff);
    }

    public void Tick(float deltaTime)
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = _activeBuffs[i];
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