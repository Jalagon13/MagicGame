using System.Collections.Generic;
using UnityEngine;

namespace ProjectWizard
{
    public class CharacterStats
    {
        public Stat MaxHealth { get; }
        public Stat Defense { get; }
        public Stat MovementSpeed { get; }

        private readonly List<Buff> _activeBuffs = new();

        public CharacterStats(CharacterDataSO data)
        {
            MaxHealth = new Stat(data.BaseHealth);
            Defense = new Stat(data.BaseDefense);
            MovementSpeed = new Stat(data.BaseSpeed);
        }

        public void AddBuff(Buff buff)
        {
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

        /// <summary>
        /// Checks if the character is currently under restoration cooldown
        /// </summary>
        public bool IsUnderRestorationCooldown()
        {
            foreach (var buff in _activeBuffs)
            {
                if (buff.Source?.ToString() == RestorationCooldownDebuff.RESTORATION_COOLDOWN_SOURCE)
                {
                    return !buff.IsExpired;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the stat by type for buff application
        /// </summary>
        public Stat GetStatByType(StatType statType)
        {
            return statType switch
            {
                StatType.MaxHealth => MaxHealth,
                StatType.Defense => Defense,
                StatType.MovementSpeed => MovementSpeed,
                _ => null
            };
        }
    }
}
