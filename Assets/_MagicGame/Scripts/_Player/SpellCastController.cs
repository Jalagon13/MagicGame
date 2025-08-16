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
    public List<SpellItemSO> SelectedSpellArray { get; }

    public SpellArrayChangedEventArgs(List<SpellItemSO> selectedSpellArray)
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
    
    private List<SpellItemSO> _spellArray = new();
    public List<SpellItemSO> SpellArray => _spellArray;

    private int _selectedSpellIndex = -1;
    public int SelectedSpellIndex => _selectedSpellIndex;
    
    private Timer _postCastDelayTimer;

    private PlayerManaSystem _playerManaSystem;
    public PlayerManaSystem PlayerManaSystem => _playerManaSystem;

    private InventoryItem _currentWandInventoryItem;

    private SpellItemSO _selectedSpell;
    public SpellItemSO SelectedSpell => _selectedSpell;
    
    public SpellCastController(Player player)
    {
        _postCastDelayTimer = new(_postCastDelayTimerDuration);
        _player = player;
        _playerManaSystem = new(player.ServerCharacter.Data.BaseMana);

        _player.SelectedItemIdNetworkVariable.OnValueChanged += OnItemSelectedChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged += OnIsCastingChanged;
        _player.SpellCaster.OnActiveHoldToCastSpellEnded += SetCooldownForHoldToCastSpell;

        HotbarManager.Instance.OnFocusSlotUpdated += CheckForSelectedItemChange;
        GameInput.Instance.OnPrimaryAction += CheckForNotHeldDownPrimaryAction;
        GameInput.Instance.OnSpaceStarted += OnSpellWheelOpen;
    }

    public void Dispose()
    {
        _player.SelectedItemIdNetworkVariable.OnValueChanged -= OnItemSelectedChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged -= OnIsCastingChanged;
        _player.SpellCaster.OnActiveHoldToCastSpellEnded -= SetCooldownForHoldToCastSpell;

        HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSelectedItemChange;
        GameInput.Instance.OnPrimaryAction -= CheckForNotHeldDownPrimaryAction;
        GameInput.Instance.OnSpaceStarted -= OnSpellWheelOpen;
    }

    private void SetCooldownForHoldToCastSpell(object sender, EventArgs e)
    {
        var holdToCastSpell = GameManager.Instance.GetItemSOFromItemId(_player.SpellCaster.HoldToCastSpell.SpellData.Value.SpellItemId) as SpellItemSO;
        _playerManaSystem.ApplySpellCooldown(holdToCastSpell);
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
        _playerManaSystem.Tick(Time.deltaTime);
        _postCastDelayTimer.Tick(Time.deltaTime);

        if (CanCast())
        {
            SpellItemSO spell = _selectedSpell;
            if (_playerManaSystem.CanCastSpell(spell)) // Check if the first spell in the group can be cast SUBJECT TO CHANGE
            {
                _player.SpellCaster.TryCastSpell(spell, GetExecutionParams);
            }
        }
    }

    private bool CanCast()
    {
        if (_spellArray == null || _spellArray.Count == 0) return false;

        bool isOverUI = Pointer.IsOverUI();
        bool isOverInteractable = Pointer.IsOverInteractable();
        bool playerIsAlive = _player.ServerCharacter.LifeState == LifeState.Alive;
        bool primaryHeldDown = GameInput.Instance.GetPrimaryHeldDown();
        bool isCasting = _player.SpellCaster.IsCasting.Value;
        bool postCastDelayTimerRunning = _postCastDelayTimer.IsRunning;
        bool isLoadingBiome = WorldManager.Instance.IsLoadingBiome;

        bool hasEnoughMana = false;
        if (_spellArray.Count > 0)
        {
            hasEnoughMana = _playerManaSystem.CanCastSpell(_selectedSpell); // Check if the first spell in the group can be cast SUBJECT TO CHANGE
        }

        return !isOverUI && !isOverInteractable && hasEnoughMana && playerIsAlive && 
        primaryHeldDown && !isCasting && !postCastDelayTimerRunning && !isLoadingBiome && !SpellWheelUI.SpellWheelOpen;
    }

    private (Vector3 spawnPoint, Vector3 direction) GetExecutionParams()
    {
        float wandAccuracy = _currentWandItemSO?.Accuracy ?? 0f;
        float spellAccuracy = _selectedSpell.Scatter;

        float totalAccuracy = Mathf.Max(0f, wandAccuracy + spellAccuracy);
        Vector2 point = _player.PlayerHand.SpellSpawnTransform.position;
        Vector2 baseDirection = (ActionManager.MouseWorldPosition - point).normalized;
        float angleOffset = UnityEngine.Random.Range(-totalAccuracy, totalAccuracy);
        Vector2 direction = Quaternion.Euler(0, 0, angleOffset) * baseDirection;

        if(!_selectedSpell.HoldToCast)
        {
            _playerManaSystem.ApplySpellCooldown(_selectedSpell);
        }

        return (point, direction);
    }

    private void OnItemSelectedChanged(int previousValue, int newValue)
    {
        if (GameManager.Instance.GetItemSOFromItemId(newValue) is WandItemSO wandItemSO)
        {
            InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
            _currentWandInventoryItem = selectedInventoryItem;
            _currentWandItemSO = wandItemSO;
            SpellItemSO[] magicArray = (_currentWandInventoryItem as WandInventoryItem).MagicArray;

            _spellArray = new();

            for (int i = 0; i < magicArray.Length; i++)
            {
                SpellItemSO item = magicArray[i];
                _spellArray.Add(item);
            }

            _selectedSpellIndex = 0;
            _selectedSpell = _spellArray[_selectedSpellIndex]; // Default set to the first spell in the group might change later

            OnSpellArrayUpdated?.Invoke(this, new SpellArrayChangedEventArgs(_spellArray));
            OnSelectedSpellUpdated?.Invoke(this, new SelectedSpellChangedEventArgs(_selectedSpell, _selectedSpellIndex));
        }
        else
        {
            _currentWandInventoryItem = null;
            _currentWandItemSO = null;
            _spellArray = null;
            _selectedSpell = null;
            _selectedSpellIndex = -1;
            
            OnSpellArrayUpdated?.Invoke(this, new SpellArrayChangedEventArgs(_spellArray));
            OnSelectedSpellUpdated?.Invoke(this, new SelectedSpellChangedEventArgs(_selectedSpell, _selectedSpellIndex));
        }
    }

    public void SelectSpellByIndex(int index)
    {
        if (_spellArray == null || _spellArray.Count == 0 || index < 0 || index >= _spellArray.Count)
        {
            Debug.LogError($"SelectSpellByIndex: Invalid index or spell array is empty for index {index}");
            return;
        }

        _selectedSpellIndex = index;
        _selectedSpell = _spellArray[_selectedSpellIndex];
        OnSelectedSpellUpdated?.Invoke(this, new SelectedSpellChangedEventArgs(_selectedSpell, _selectedSpellIndex));
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
            if(_currentWandInventoryItem != null && inventoryItem.Id == _currentWandInventoryItem.Id) 
                return;
        }
        
        if (_player.SpellCaster.IsCasting.Value)
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }
}