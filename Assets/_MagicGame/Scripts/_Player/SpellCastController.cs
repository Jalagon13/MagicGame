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
    private Timer _postCastDelayTimer;
    
    private WandManaSystem _wandManaSystem;
    public WandManaSystem WandManaSystem => _wandManaSystem;
    
    private InventoryItem _currentWandInventoryItem;

    public SpellCastController(Player player)
    {
        _postCastDelayTimer = new(_postCastDelayTimerDuration);
        _player = player;
        _wandManaSystem = new();
        
        _player.SelectedItemIdNetworkVariable.OnValueChanged += OnItemIdChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged += OnIsCastingChanged;
        
        HotbarManager.Instance.OnFocusSlotUpdated += CheckForSelectedItemChange;
        InventoryManager.Instance.OnInventoryUpdated += CheckForWand;

    }
    
    public void Dispose()
    {
        _player.SelectedItemIdNetworkVariable.OnValueChanged -= OnItemIdChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged -= OnIsCastingChanged;
        
        HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSelectedItemChange;
        InventoryManager.Instance.OnInventoryUpdated -= CheckForWand;
    }

    private void CheckForWand(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
    {
        // Whenever the inventory is updated, gather all the wands in the inventory and create 
        foreach (InventoryItem item in e.InventoryItems)
        {
            if (item.Item is WandItemSO wandItemSO)
            {
                _wandManaSystem.AddOrUpdateWand(wandItemSO, item.Id);
            }
        }
    }

    private void OnIsCastingChanged(bool previousValue, bool newValue)
    {
        // Right after the spell has been cast
        if(previousValue && !newValue)
        {
            _postCastDelayTimer.Reset();

            if (InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem) && selectedInventoryItem.Item is WandItemSO wandItemSO)
            {
                // If _currentSpellIndex is the last spell, get the inventoryitem Id
                if (_currentSpellIndex >= _spellMetaDataList.Count - 1)
                {
                    _wandManaSystem.StartWandRecharge(selectedInventoryItem.Id, wandItemSO.RechargeTime);
                }

                // Cycle through spells
                _currentSpellIndex = (_currentSpellIndex + 1) % _spellMetaDataList.Count; 

                // Subtract Mana
                _wandManaSystem.TrySpendMana(selectedInventoryItem.Id, CalculateTotalManaCost(_spellMetaDataList[_currentSpellIndex]));
            }
        }
    }

    public void SpellCastControllerUpdate()
    {
        _wandManaSystem.Tick(Time.deltaTime, _currentWandInventoryItem);
        _postCastDelayTimer.Tick(Time.deltaTime);

        if(CanCast())
        {
            _player.SpellCaster.TryCastSpell(_spellMetaDataList[_currentSpellIndex], GetExecutionParams);
        }
    }

    private bool CanCast()
    {
        if (_spellMetaDataList == null) return false;

        bool isOverUI = Pointer.IsOverUI();
        bool isOverInteractable = Pointer.IsOverInteractable();
        bool playerIsAlive = _player.ServerCharacter.LifeState == LifeState.Alive;
        bool primaryHeldDown = GameInput.Instance.GetPrimaryHeldDown();
        bool isCasting = _player.SpellCaster.IsCasting.Value;
        bool postCastDelayTimerRunning = _postCastDelayTimer.IsRunning;
        bool isLoadingBiome = WorldManager.Instance.IsLoadingBiome;
        bool isWandRecharging = IsWandRecharging(out Timer rechargeTimer);
        bool hasEnoughMana = false;

        if (InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem))
        {
            hasEnoughMana = _wandManaSystem.GetCurrentMana(selectedInventoryItem.Id) >= CalculateTotalManaCost(_spellMetaDataList[_currentSpellIndex]);
        }

        return !isOverUI && !isOverInteractable && hasEnoughMana && playerIsAlive && primaryHeldDown && !isCasting && !isWandRecharging && !postCastDelayTimerRunning && !isLoadingBiome;
    }
    
    public bool IsWandRecharging(out Timer rechargeTimer)
    {
        if (InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem))
        {
            bool isRecharging = _wandManaSystem.IsWandRecharging(selectedInventoryItem.Id, out rechargeTimer);
            return isRecharging;
        }

        rechargeTimer = null;
        return false;
    }

    private (Vector3 spawnPoint, Vector3 direction) GetExecutionParams()
    {
        float wandAccuracy = _currentWandItemSO?.Accuracy ?? 0f;
        float spellAccuracy = _spellMetaDataList[_currentSpellIndex].SpellItem.Scatter;
        float totalSpellModAccuracy = 0;

        foreach (var mod in _spellMetaDataList[_currentSpellIndex].SpellMods)
        {
            totalSpellModAccuracy += mod.Scatter;
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

    private void OnItemIdChanged(int previousValue, int newValue)
    {
        if (GameManager.Instance.GetItemSOFromItemId(newValue) is WandItemSO wandItemSO)
        {
            InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
            _currentWandInventoryItem = selectedInventoryItem;
            MagicItemSO[] currentMagicArray = (_currentWandInventoryItem as WandInventoryItem).MagicArray;

            if (currentMagicArray == null || currentMagicArray.Length == 0)
            {
                return;
            }

            _spellMetaDataList = new();
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
            _currentWandInventoryItem = null;
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
