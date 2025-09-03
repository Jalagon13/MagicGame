using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedSpellChangedEventArgs : EventArgs
{
    public SpellItemSO SelectedSpell { get; }
    public int SelectedSpellIndex { get; }

    public SelectedSpellChangedEventArgs(SpellItemSO selectedSpell, int selectedSpellIndex)
    {
        SelectedSpell = selectedSpell;
        SelectedSpellIndex = selectedSpellIndex;
    }
}

public class SpellArrayChangedEventArgs : EventArgs
{
    public SpellItemSO[] SelectedSpellArray { get; }

    public SpellArrayChangedEventArgs(SpellItemSO[] selectedSpellArray)
    {
        SelectedSpellArray = selectedSpellArray;
    }
}

public class SpellCastController
{
    public event EventHandler<SelectedSpellChangedEventArgs> OnSelectedSpellUpdated;
    public event EventHandler<SpellArrayChangedEventArgs> OnSpellArrayUpdated;
    private static readonly float _postCastDelayTimerDuration = 0.15f;

    private Player _player;
    private WandItemSO _currentWandItemSO;
    
    private Timer _postCastDelayTimer;

    private SpellCooldownSystem _spellCooldownSystem;
    public SpellCooldownSystem SpellCooldownSystem => _spellCooldownSystem;

    private WandInventoryItem _selectedWandInventoryItem;
    public WandInventoryItem SelectedWandInventoryItem => _selectedWandInventoryItem;

    public SpellCastController(Player player)
    {
        _player = player;
        _postCastDelayTimer = new(_postCastDelayTimerDuration);
        _spellCooldownSystem = new();

        _player.SelectedItemId.OnValueChanged += OnItemSelectedChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged += OnIsCastingChanged;
        _player.SpellCaster.OnActiveHoldToCastSpellEnded += SetCooldownForHoldToCastSpell;
        _player.SpellCaster.OnRightBeforeSpellExecuted += OnRightBeforeSpellExecuted;

        HotbarManager.Instance.OnFocusSlotUpdated += CheckForSelectedItemChange;
        GameInput.Instance.OnPrimaryAction += CheckForNotHeldDownPrimaryAction;
        GameInput.Instance.OnSpaceStarted += OnSpellWheelOpen;
    }

    public void Dispose()
    {
        _player.SelectedItemId.OnValueChanged -= OnItemSelectedChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged -= OnIsCastingChanged;
        _player.SpellCaster.OnActiveHoldToCastSpellEnded -= SetCooldownForHoldToCastSpell;
        _player.SpellCaster.OnRightBeforeSpellExecuted -= OnRightBeforeSpellExecuted;

        HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSelectedItemChange;
        GameInput.Instance.OnPrimaryAction -= CheckForNotHeldDownPrimaryAction;
        GameInput.Instance.OnSpaceStarted -= OnSpellWheelOpen;
    }

    private void OnRightBeforeSpellExecuted(object sender, SpellCaster.SpellExecutedEventArgs e)
    {
        TryToConsumeCastingMaterials();
    }
    
    public void TryToConsumeCastingMaterials()
    {
        if (_selectedWandInventoryItem.GetSelectedSpell().CastingMaterials.Count == 0)
            return;

        if (InventoryManager.Instance.HasAllIngredients(_selectedWandInventoryItem.GetSelectedSpell().CastingMaterials))
        {
            InventoryManager.Instance.RemoveItems(_selectedWandInventoryItem.GetSelectedSpell().CastingMaterials);
        }
        else
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }

    private void SetCooldownForHoldToCastSpell(object sender, EventArgs e)
    {
        var holdToCastSpell = GameDataRegistry.Instance.GetItemDataFromItemId(_player.SpellCaster.HoldToCastSpell.SpellData.Value.SpellItemId) as SpellItemSO;
        _spellCooldownSystem.ApplySpellCooldown(holdToCastSpell);
    }

    private void OnIsCastingChanged(bool previousValue, bool newValue)
    {
        if (previousValue && !newValue)
        {
            _postCastDelayTimer.Reset();
        }
    }

    public void SpellCastControllerUpdate()
    {
        _spellCooldownSystem.Tick(Time.deltaTime);
        _postCastDelayTimer.Tick(Time.deltaTime);

        if (CanCast())
        {
            SpellItemSO spell = _selectedWandInventoryItem.GetSelectedSpell();
            if (_spellCooldownSystem.CanCastSpell(spell)) // Check if the first spell in the group can be cast SUBJECT TO CHANGE
            {
                _player.SpellCaster.TryCastSpell(spell, GetExecutionParams);
            }
        }
    }

    private bool CanCast()
    {
        if (_selectedWandInventoryItem == null || !_selectedWandInventoryItem.HasSpells()) return false;

        bool isOverUI = Pointer.IsOverUI();
        bool isOverInteractable = Pointer.IsOverInteractable();
        bool playerIsAlive = _player.ServerCharacter.LifeState == LifeState.Alive;
        bool primaryHeldDown = GameInput.Instance.GetPrimaryHeldDown();
        bool isCasting = _player.SpellCaster.IsCasting.Value;
        bool postCastDelayTimerRunning = _postCastDelayTimer.IsRunning;
        bool isLoadingBiome = GameWorld.Instance.IsLoadingBiome;
        bool hasEnoughMana = _spellCooldownSystem.CanCastSpell(_selectedWandInventoryItem.GetSelectedSpell());
        bool hasAllCastingMaterials = _selectedWandInventoryItem.GetSelectedSpell().CastingMaterials.Count == 0 ||
          InventoryManager.Instance.HasAllIngredients(_selectedWandInventoryItem.GetSelectedSpell().CastingMaterials);

        return !isOverUI && !isOverInteractable && hasEnoughMana && playerIsAlive && hasAllCastingMaterials &&
        primaryHeldDown && !isCasting && !postCastDelayTimerRunning && !isLoadingBiome && !SpellWheelUI.SpellWheelOpen;
    }

    private (Vector3 spawnPoint, Vector3 direction) GetExecutionParams()
    {
        float wandAccuracy = _currentWandItemSO?.Accuracy ?? 0f;
        float spellAccuracy = _selectedWandInventoryItem.GetSelectedSpell().Scatter;

        float totalAccuracy = Mathf.Max(0f, wandAccuracy + spellAccuracy);
        Vector2 point = _player.PlayerHand.SpellSpawnTransform.position;
        Vector2 baseDirection = (ActionManager.MouseWorldPosition - point).normalized;
        float angleOffset = UnityEngine.Random.Range(-totalAccuracy, totalAccuracy);
        Vector2 direction = Quaternion.Euler(0, 0, angleOffset) * baseDirection;

        if(!_selectedWandInventoryItem.GetSelectedSpell().HoldToCast)
        {
            _spellCooldownSystem.ApplySpellCooldown(_selectedWandInventoryItem.GetSelectedSpell());
        }
        return (point, direction);
    }

    private void OnItemSelectedChanged(ushort previousValue, ushort newValue)
    {
        if (GameDataRegistry.Instance.GetItemDataFromItemId(newValue) is WandItemSO wandItemSO)
        {
            _currentWandItemSO = wandItemSO;
            
            InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
            _selectedWandInventoryItem = selectedInventoryItem as WandInventoryItem;
            SpellItemSO[] magicArray = _selectedWandInventoryItem.MagicArray;

            // If the wand has no spells selected, we can set the first spell as the selected spell
            if(_selectedWandInventoryItem.HasSpells() && _selectedWandInventoryItem.SelectedSpellIndex == -1)
            {
                for (int i = 0; i < magicArray.Length; i++)
                {
                    if(magicArray[i] != null)
                    {
                        // Set the first non-null spell as the selected spell
                        _selectedWandInventoryItem.SetSelectedSpellIndex(i);
                        break;
                    }
                }
            }

            OnSpellArrayUpdated?.Invoke(this, new SpellArrayChangedEventArgs(_selectedWandInventoryItem.MagicArray));
            OnSelectedSpellUpdated?.Invoke(this, new SelectedSpellChangedEventArgs(_selectedWandInventoryItem.GetSelectedSpell(), _selectedWandInventoryItem.SelectedSpellIndex));
        }
        else if (GameDataRegistry.Instance.GetItemDataFromItemId(newValue) is SpellItemSO spellItemSO)
        {
            _selectedWandInventoryItem = new WandInventoryItem(spellItemSO, 1, 1, 0);
            _selectedWandInventoryItem.SetMagic(spellItemSO, 0);
            _currentWandItemSO = null;

            // Do this so the fake WandInventoryItem has the same Id as the inventory slot currently being held so the CheckForSelectedItemChange's check works
            InventoryManager.Instance.SelectedItemExists(out InventoryItem inventoryItem);
            _selectedWandInventoryItem.SetId(inventoryItem.Id);

            OnSpellArrayUpdated?.Invoke(this, new SpellArrayChangedEventArgs(new SpellItemSO[1] { spellItemSO }));
            OnSelectedSpellUpdated?.Invoke(this, new SelectedSpellChangedEventArgs(spellItemSO, 0));
        }
        else
        {
            _selectedWandInventoryItem = null;
            _currentWandItemSO = null;

            OnSpellArrayUpdated?.Invoke(this, new SpellArrayChangedEventArgs(Array.Empty<SpellItemSO>()));
            OnSelectedSpellUpdated?.Invoke(this, new SelectedSpellChangedEventArgs(null, -1));
        }
    }

    public void SelectSpellByIndex(int index)
    {
        if (_selectedWandInventoryItem.MagicArray == null || !_selectedWandInventoryItem.HasSpells() || index < 0 || index >= _selectedWandInventoryItem.MagicArray.Length)
        {
            Debug.LogError($"SelectSpellByIndex: Invalid index or spell array is empty for index {index}");
            return;
        }
        
        _selectedWandInventoryItem.SetSelectedSpellIndex(index);
        OnSelectedSpellUpdated?.Invoke(this, new SelectedSpellChangedEventArgs(_selectedWandInventoryItem.GetSelectedSpell(), _selectedWandInventoryItem.SelectedSpellIndex));
    }

    private void OnPlayerLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if (previousValue == LifeState.Alive && newValue == LifeState.Dead)
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }

    private void CheckForNotHeldDownPrimaryAction(object sender, GameInput.OnPrimaryOrSecondaryActionEventArgs e)
    {
        if (!e.IsHeldDown)
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }

    private void OnSpellWheelOpen(object sender, EventArgs e)
    {
        _player.SpellCaster.TryToCancelCast();
    }

    private void CheckForSelectedItemChange(object sender, HotbarManager.OnFocusItemSetEventArgs e)
    {
        if(InventoryManager.Instance.SelectedItemExists(out InventoryItem inventoryItem))
        {
            if(_selectedWandInventoryItem != null && inventoryItem.Id == _selectedWandInventoryItem.Id) 
                return;
        }
        
        if (_player.SpellCaster.IsCasting.Value)
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }
}