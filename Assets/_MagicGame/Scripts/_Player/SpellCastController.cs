using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SpellMetaData
{
    public SpellItemSO SpellItem;
    public List<SpellModItemSO> SpellMods;

    public SpellMetaData(SpellItemSO spellItem, List<SpellModItemSO> spellMods)
    {
        SpellItem = spellItem;
        SpellMods = spellMods;
    }
}

public class SpellCastController
{
    private static readonly float _postCastDelayTimerDuration = 0.15f; // Duration after casting a spell before the next can be cast
    
    private Player _player;
    private int _currentSpellIndex;
    private WandItemSO _currentWandItemSO;
    private List<SpellMetaData> _spellMetaDataList = new();
    private Dictionary<ulong, Timer> _rechargeTimers = new();
    private Timer _postCastDelayTimer;

    public SpellCastController(Player player)
    {
        _player = player;
        
        _player.SelectedItemIdNetworkVariable.OnValueChanged += OnItemIdChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged += OnIsCastingChanged;
        HotbarManager.Instance.OnFocusSlotUpdated += CheckForSelectedItemChange;

        _postCastDelayTimer = new(_postCastDelayTimerDuration);
    }
    
    public void Dispose()
    {
        _player.SelectedItemIdNetworkVariable.OnValueChanged -= OnItemIdChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged -= OnIsCastingChanged;
        HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSelectedItemChange;
    }

    private void OnIsCastingChanged(bool previousValue, bool newValue)
    {
        // Right after the spell has been cast
        if(previousValue && !newValue)
        {
            _postCastDelayTimer.Reset();

            // If _currentSpellIndex is the last spell, get the inventoryitem Id
            if (_currentSpellIndex >= _spellMetaDataList.Count - 1)
            {
                if (InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem) && selectedInventoryItem.Item is WandItemSO wandItemSO)
                {
                    _rechargeTimers.Add(selectedInventoryItem.Id, new Timer(wandItemSO.RechargeTime));
                }
            }

            _currentSpellIndex = (_currentSpellIndex + 1) % _spellMetaDataList.Count; // Cycle through spells
            
            // Subtract Mana
            int totalMana = CalculateTotalManaCost(_spellMetaDataList[_currentSpellIndex]);
            
            _player.PlayerManaSystem.TrySpendMana(totalMana);
        }
    }

    public void SpellCastControllerUpdate()
    {
        UpdateRechargeTimes();
    
        if(CanCast())
        {
            Debug.Log($"Casting spell: {_spellMetaDataList[_currentSpellIndex].SpellItem.name} at index {_currentSpellIndex} with mods: {_spellMetaDataList[_currentSpellIndex].SpellMods.Count}, iscasting: {_player.SpellCaster.IsCasting.Value}");
            _player.SpellCaster.TryCastSpell(_spellMetaDataList[_currentSpellIndex], GetExecutionParams);
        }
    }

    private void UpdateRechargeTimes()
    {
        _postCastDelayTimer.Tick(Time.deltaTime);

        List<ulong> timersToRemove = new();

        foreach (var kvp in _rechargeTimers)
        {
            if (kvp.Value.IsRunning)
            {
                kvp.Value.Tick(Time.deltaTime);
                if (!kvp.Value.IsRunning)
                {
                    timersToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var key in timersToRemove)
        {
            _rechargeTimers.Remove(key);
            Debug.Log($"Recharge timer for item {key} has ended.");
        }
    }

    private (Vector3 spawnPoint, Vector3 direction) GetExecutionParams()
    {
        float wandAccuracy = _currentWandItemSO?.Accuracy ?? 0f;
        float spellAccuracy = _spellMetaDataList[_currentSpellIndex].SpellItem.Accuracy;
        float totalSpellModAccuracy = 0;

        foreach (var mod in _spellMetaDataList[_currentSpellIndex].SpellMods)
        {
            totalSpellModAccuracy += mod.Accuracy;
        }

        float totalAccuracy = Mathf.Max(0f, wandAccuracy + spellAccuracy + totalSpellModAccuracy);

        Vector2 point = _player.PlayerHand.SpellSpawnTransform.position;
        
        // Calculate base direction
        Vector2 baseDirection = (ActionManager.MouseWorldPosition - point).normalized;
        
        // Apply random angle offset within ±accuracy degrees
        float angleOffset = UnityEngine.Random.Range(-totalAccuracy, totalAccuracy);
        Vector2 direction = Quaternion.Euler(0, 0, angleOffset) * baseDirection;

        return (point, direction);
    }

    private bool CanCast()
    {
        bool isHoldingWand = _spellMetaDataList != null;
        bool isOverUI = Pointer.IsOverUI();
        bool isOverInteractable = Pointer.IsOverInteractable();
        bool playerIsAlive = _player.ServerCharacter.LifeState == LifeState.Alive;
        bool primaryHeldDown = GameInput.Instance.GetPrimaryHeldDown();
        bool isCasting = _player.SpellCaster.IsCasting.Value;
        bool postCastDelayTimerRunning = _postCastDelayTimer.IsRunning;
        
        InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
        bool wandOnCooldown = _rechargeTimers.ContainsKey(selectedInventoryItem.Id);
        
        int totalMana = CalculateTotalManaCost(_spellMetaDataList[_currentSpellIndex]);
        bool hasEnoughMana = Player.Instance.PlayerManaSystem.HasEnoughMana(totalMana);

        return isHoldingWand && !isOverUI && !isOverInteractable && hasEnoughMana && playerIsAlive && primaryHeldDown && !isCasting && !wandOnCooldown && !postCastDelayTimerRunning;
    }

    private void OnItemIdChanged(int previousValue, int newValue)
    {
        if (GameManager.Instance.GetItemSOFromItemId(newValue) is WandItemSO wandItemSO)
        {
            InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
            MagicItemSO[] currentMagicArray = (selectedInventoryItem as WandInventoryItem).MagicArray;

            if (currentMagicArray == null || currentMagicArray.Length == 0)
            {
                return;
            }

            _spellMetaDataList.Clear();
            List<SpellModItemSO> currentMods = new();

            foreach (var item in currentMagicArray)
            {
                if (item is SpellModItemSO mod)
                {
                    currentMods.Add(mod);
                }
                else if (item is SpellItemSO spell)
                {
                    _spellMetaDataList.Add(new SpellMetaData(spell, new List<SpellModItemSO>(currentMods)));
                    currentMods.Clear();
                }
            }

            _currentSpellIndex = 0; // Reset to the first spell
            _currentWandItemSO = wandItemSO;
        }
        else
        {
            _currentWandItemSO = null;
            _spellMetaDataList = null;
        }
    }

    private void OnPlayerLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if (previousValue == LifeState.Alive && newValue == LifeState.Dead)
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }

    private void CheckForSelectedItemChange(object sender, HotbarManager.OnFocusItemSetEventArgs e)
    {
        if (e.SelectedItemId == _player.SpellCaster.CurrentSpellData.SpellItemId)
        {
            Debug.Log($"Cannot cancel if the spell is the same spell that is currently being cast.");
            return;
        }

        // If an invetnory slot was selected, cancel spell casting
        if (_player.SpellCaster.IsCasting.Value)
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }

    private int CalculateTotalManaCost(SpellMetaData spellMeta)
    {
        int totalMana = spellMeta.SpellItem.ManaCost;
        foreach (var mod in spellMeta.SpellMods)
        {
            totalMana += mod.ManaCost;
        }
        return totalMana;
    }
}
