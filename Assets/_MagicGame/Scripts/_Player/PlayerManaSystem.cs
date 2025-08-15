using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManaSystem
{
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
    private int _regenRate;

    private Dictionary<SpellItemSO, Timer> _spellCooldowns = new();

    private float _regenTimer;

    public PlayerManaSystem(int maxMana, int regenRate)
    {
        _maxMana = maxMana;
        _currentMana = maxMana;
        _regenRate = regenRate;
    }

    public void Tick(float deltaTime)
    {
        // Regen
        _regenTimer += deltaTime;
        float manaToAdd = _regenRate * _regenTimer;
        if (manaToAdd >= 1f)
        {
            int gain = Mathf.FloorToInt(manaToAdd);
            _currentMana = Mathf.Min(_currentMana + gain, _maxMana);
            _regenTimer -= gain / (float)_regenRate;
            OnManaChanged?.Invoke(this, new ManaChangedEventArgs(_currentMana, _maxMana));
        }

        // Tick spell cooldowns
        foreach (var kvp in _spellCooldowns)
            kvp.Value.Tick(deltaTime);
    }

    public bool CanCastSpell(SpellItemSO spell)
    {
        if (_currentMana < spell.ManaCost)
            return false;

        if (_spellCooldowns.TryGetValue(spell, out Timer cooldown) && cooldown.IsRunning)
            return false;
        
        return true;
    }
    
    public void ApplySpellCooldown(SpellItemSO spell)
    {
        Debug.Log($"Applying cooldown for spell: {spell.name}, Mana Cost: {spell.ManaCost}, Current Mana: {_currentMana}");
        if (_spellCooldowns.ContainsKey(spell))
        {
            _spellCooldowns[spell].Reset();
        }
        else
        {
            _spellCooldowns[spell] = new Timer(spell.Cooldown);
        }

        _currentMana -= spell.ManaCost;
        OnManaChanged?.Invoke(this, new ManaChangedEventArgs(_currentMana, _maxMana));
    }
}