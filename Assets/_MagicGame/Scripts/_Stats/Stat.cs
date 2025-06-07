using System.Collections.Generic;

public class Stat
{
    public float BaseValue { get; }
    private List<StatModifier> _modifiers = new();
    private bool _dirty = true;
    private float _finalValue;

    public Stat(float baseValue)
    {
        BaseValue = baseValue;
    }

    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        _dirty = true;
    }

    public void RemoveModifier(StatModifier mod)
    {
        _modifiers.Remove(mod);
        _dirty = true;
    }

    public float GetValue()
    {
        if (_dirty) Recalculate();
        return _finalValue;
    }

    private void Recalculate()
    {
        float flat = 0;
        float percent = 1f;

        foreach (var mod in _modifiers)
        {
            if (mod.Type == StatModifierType.Flat)
                flat += mod.Value;
            else if (mod.Type == StatModifierType.Percent)
                percent += mod.Value;
        }

        _finalValue = (BaseValue + flat) * percent;
        _dirty = false;
    }
}