using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManaSystem
{
    public event EventHandler OnSpellCooldownTimersUpdated;
    public event EventHandler<ManaChangedEventArgs> OnManaChanged;

    public class ManaChangedEventArgs : EventArgs
    {
        public int CurrentMana { get; }
        public int MaxMana { get; }
        public ManaChangedEventArgs(int currentMana, int maxMana)
        {
            CurrentMana = currentMana;
            MaxMana = maxMana;
        }
    }

    private int _currentMana;
    private int _maxMana;

    private Dictionary<SpellItemSO, Timer> _spellCooldowns = new();
    public Dictionary<SpellItemSO, Timer> SpellCoolDownTimers => _spellCooldowns;


    public PlayerManaSystem(int maxMana)
    {
        _maxMana = maxMana;
        _currentMana = maxMana;
    }

    public void Tick(float deltaTime)
    {
        bool anyCooldownRunning = false;

        foreach (var kvp in _spellCooldowns)
        {
            kvp.Value.Tick(deltaTime);
            if (kvp.Value.IsRunning)
                anyCooldownRunning = true;
        }

        if (anyCooldownRunning)
        {
            OnSpellCooldownTimersUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool CanCastSpell(SpellItemSO spell)
    {
        if(spell == null)
            return false;
    
        if (_currentMana < 1)
            return false;

        if (_spellCooldowns.TryGetValue(spell, out Timer cooldown) && cooldown.IsRunning)
            return false;

        return true;
    }

    public void ApplySpellCooldown(SpellItemSO spell)
    {
        if (_spellCooldowns.ContainsKey(spell))
        {
            _spellCooldowns[spell].Reset();
        }
        else
        {
            _spellCooldowns[spell] = new Timer(spell.Cooldown);
        }

        TryToDrainMana(spell);
    }
    
    public void TryToDrainMana(SpellItemSO spell)
    {
        bool drained = spell.ManaDrainProbability > UnityEngine.Random.value;
        _currentMana -= drained ? 1 : 0;
        OnManaChanged?.Invoke(this, new ManaChangedEventArgs(_currentMana, _maxMana));
    }

    public void AddMana(int amount)
    {
        Debug.Log($"Adding mana: {amount}, CurrentMana: {_currentMana}, MaxMana: {_maxMana}");
        _currentMana = Mathf.Min(_currentMana + amount, _maxMana);
        OnManaChanged?.Invoke(this, new ManaChangedEventArgs(_currentMana, _maxMana));
    }
}