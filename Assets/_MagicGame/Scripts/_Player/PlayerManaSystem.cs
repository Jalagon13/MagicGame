using System;
using UnityEngine;

public class PlayerManaSystem : MonoBehaviour
{
    public event EventHandler<PointsChangedEventArgs> OnManaChanged;

    private CharacterStats _characterStats;
    private int _currentMana;
    private float _manaRegenTimer;

    public int CurrentMana => _currentMana;

    private void Start()
    {
        _characterStats = Player.Instance.ServerCharacter.Stats;

        if (_characterStats != null)
        {
            _currentMana = Mathf.FloorToInt(_characterStats.MaxMana.GetValue());
        }
        else
        {
            Debug.LogError("CharacterStats is not initialized.");
        }
    }

    public void UpdateManaRegen(float deltaTime)
    {
        if (_characterStats == null) return;

        _manaRegenTimer += deltaTime;

        float manaPerSecond = _characterStats.ManaRegen.GetValue();

        // Calculate how much mana should be added based on time passed
        float totalManaToAdd = manaPerSecond * _manaRegenTimer;

        if (totalManaToAdd >= 1f)
        {
            int manaGain = Mathf.FloorToInt(totalManaToAdd);
            int maxMana = Mathf.FloorToInt(_characterStats.MaxMana.GetValue());

            int oldMana = _currentMana;
            _currentMana = Mathf.Min(_currentMana + manaGain, maxMana);

            if (oldMana != _currentMana)
            {
                OnManaChanged?.Invoke(this, new PointsChangedEventArgs(_currentMana, maxMana));
            }

            // Remove the time equivalent of the mana we just added
            _manaRegenTimer -= manaGain / (manaPerSecond > 0 ? manaPerSecond : 1f);
        }
    }

    public bool TrySpendMana(int amount)
    {
        if (_currentMana < amount)
            return false;
        Debug.Log($"Spending mana: {amount}, Current Mana: {_currentMana}");
        _currentMana = Mathf.Clamp(_currentMana - amount, 0, Mathf.FloorToInt(_characterStats.MaxMana.GetValue()));
        OnManaChanged?.Invoke(this, new PointsChangedEventArgs(_currentMana, Mathf.FloorToInt(_characterStats.MaxMana.GetValue())));
        return true;
    }
    
    public bool HasEnoughMana(int amount)
    {
        return _currentMana >= amount;
    }
}
