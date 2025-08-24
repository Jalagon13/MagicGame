using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellCooldownSystem
{
    public event EventHandler OnSpellCooldownTimersUpdated;

    private Dictionary<SpellItemSO, Timer> _spellCooldowns = new();
    public Dictionary<SpellItemSO, Timer> SpellCoolDownTimers => _spellCooldowns;


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
    }
}