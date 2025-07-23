using System;
using System.Collections.Generic;
using UnityEngine;

public class WandManaSystem
{
    public event EventHandler<ManaChangedEventArgs> OnManaChanged;
    
    public class ManaChangedEventArgs : EventArgs
    {
        public ulong WandId { get; }
        public int CurrentMana { get; }
        public int MaxMana { get; }

        public ManaChangedEventArgs(ulong wandId, int currentMana, int maxMana)
        {
            WandId = wandId;
            CurrentMana = currentMana;
            MaxMana = maxMana;
        }
    }

    private class WandEntry
    {
        public int CurrentMana;
        public int MaxMana;
        public int RegenRate;
        public ulong InventoryItemId;
        public float RechargeTime;
        
        internal Timer _rechargeTimer;
        private float _manaRegenTimer;

        public bool IsRecharging => _rechargeTimer?.IsRunning ?? false;

        public bool Tick(float deltaTime)
        {
            bool manaChanged = false;

            if (_rechargeTimer != null && _rechargeTimer.IsRunning)
                _rechargeTimer.Tick(deltaTime);

            // Accumulate regen time
            _manaRegenTimer += deltaTime;

            // Only regenerate whole numbers of mana, matching PlayerManaSystem.UpdateManaRegen logic
            float totalManaToAdd = RegenRate * _manaRegenTimer;

            if (totalManaToAdd >= 1f)
            {
                int manaGain = Mathf.FloorToInt(totalManaToAdd);
                float oldMana = CurrentMana;
                CurrentMana = Mathf.Min(CurrentMana + manaGain, MaxMana);

                if (CurrentMana != oldMana)
                {
                    manaChanged = true;
                }

                // Subtract time proportional to the amount of mana regenerated
                _manaRegenTimer -= manaGain / (RegenRate > 0 ? RegenRate : 1f);
            }

            return manaChanged;
        }

        public void StartRecharge(float duration)
        {
            RechargeTime = duration;
            _rechargeTimer = new Timer(duration);
        }
    }

    private Dictionary<ulong, WandEntry> _wandEntries = new();

    private bool _isSelectingWand;
    public bool IsSelectingWand => _isSelectingWand;

    public void AddOrUpdateWand(WandItemSO wandItemSO, ulong inventoryItemId)
    {
        if (!_wandEntries.ContainsKey(inventoryItemId))
        {
            var entry = new WandEntry
            {
                CurrentMana = wandItemSO.BaseMana,
                MaxMana = wandItemSO.BaseMana,
                RegenRate = wandItemSO.BaseManaRegen,
                InventoryItemId = inventoryItemId,
                RechargeTime = wandItemSO.RechargeTime
            };

            entry.StartRecharge(wandItemSO.RechargeTime);
            _wandEntries[inventoryItemId] = entry;
        }
    }

    public void Tick(float deltaTime, InventoryItem currentWandInventoryItem)
    {
        ulong currentId = currentWandInventoryItem?.Id ?? 0;
        _isSelectingWand = currentWandInventoryItem != null && _wandEntries.ContainsKey(currentId);

        foreach (var entry in _wandEntries.Values)
        {
            bool manaChanged = entry.Tick(deltaTime);
            if (entry.InventoryItemId == currentId && (manaChanged || _isSelectingWand))
            {
                OnManaChanged?.Invoke(this, new ManaChangedEventArgs(entry.InventoryItemId, entry.CurrentMana, entry.MaxMana));
            }
        }
    }

    public float GetCurrentMana(ulong inventoryItemId)
    {
        return _wandEntries.TryGetValue(inventoryItemId, out var entry) ? entry.CurrentMana : 0f;
    }

    public bool TrySpendMana(ulong inventoryItemId, int amount)
    {
        if (_wandEntries.TryGetValue(inventoryItemId, out var entry))
        {
            if (entry.CurrentMana >= amount)
            {
                entry.CurrentMana -= amount;
                return true;
            }
        }
        return false;
    }

    public bool IsWandRecharging(ulong inventoryItemId, out Timer rechargeTimer)
    {
        if (_wandEntries.TryGetValue(inventoryItemId, out var entry))
        {
            rechargeTimer = entry.IsRecharging ? entry._rechargeTimer : null;
            return entry.IsRecharging;
        }

        rechargeTimer = null;
        return false;
    }

    public void StartWandRecharge(ulong inventoryItemId, float duration)
    {
        if (_wandEntries.TryGetValue(inventoryItemId, out var entry))
        {
            entry.StartRecharge(duration);
        }
    }
}