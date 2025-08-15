using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellCastController
{
    private static readonly float _postCastDelayTimerDuration = 0.15f;

    private Player _player;
    private WandItemSO _currentWandItemSO;
    private List<SpellItemSO> _selectedSpellArray = new();
    private Timer _postCastDelayTimer;

    private PlayerManaSystem _playerManaSystem;
    public PlayerManaSystem PlayerManaSystem => _playerManaSystem;

    private InventoryItem _currentWandInventoryItem;

    private SpellItemSO _selectedSpell;
    public SpellItemSO SelectedSpell => _selectedSpell;
    
    private int _selectedSpellIndex;

    public SpellCastController(Player player)
    {
        _postCastDelayTimer = new(_postCastDelayTimerDuration);
        _player = player;
        _playerManaSystem = new(player.ServerCharacter.Data.BaseMana);

        _player.SelectedItemIdNetworkVariable.OnValueChanged += OnItemSelectedChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged += OnIsCastingChanged;

        HotbarManager.Instance.OnFocusSlotUpdated += CheckForSelectedItemChange;
    }

    public void Dispose()
    {
        _player.SelectedItemIdNetworkVariable.OnValueChanged -= OnItemSelectedChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
        _player.SpellCaster.IsCasting.OnValueChanged -= OnIsCastingChanged;

        HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSelectedItemChange;
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
            SpellItemSO spell = _selectedSpellArray[_selectedSpellIndex];
            if (_playerManaSystem.CanCastSpell(spell)) // Check if the first spell in the group can be cast SUBJECT TO CHANGE
            {
                _player.SpellCaster.TryCastSpell(spell, GetExecutionParams);
            }
        }
    }

    private bool CanCast()
    {
        if (_selectedSpellArray == null || _selectedSpellArray.Count == 0) return false;

        bool isOverUI = Pointer.IsOverUI();
        bool isOverInteractable = Pointer.IsOverInteractable();
        bool playerIsAlive = _player.ServerCharacter.LifeState == LifeState.Alive;
        bool primaryHeldDown = GameInput.Instance.GetPrimaryHeldDown();
        bool isCasting = _player.SpellCaster.IsCasting.Value;
        bool postCastDelayTimerRunning = _postCastDelayTimer.IsRunning;
        bool isLoadingBiome = WorldManager.Instance.IsLoadingBiome;

        bool hasEnoughMana = false;
        if (_selectedSpellArray.Count > 0)
        {
            hasEnoughMana = _playerManaSystem.CanCastSpell(_selectedSpell); // Check if the first spell in the group can be cast SUBJECT TO CHANGE
        }

        return !isOverUI && !isOverInteractable && hasEnoughMana && playerIsAlive && primaryHeldDown && !isCasting && !postCastDelayTimerRunning && !isLoadingBiome;
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
        
        _playerManaSystem.ApplySpellCooldown(_selectedSpell);

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

            _selectedSpellArray = new();

            for (int i = 0; i < magicArray.Length; i++)
            {
                SpellItemSO item = magicArray[i];

                _selectedSpellArray.Add(item);
            }

            _selectedSpellIndex = 0; // Default to the first spell in the wand for now
            _selectedSpell = magicArray.Length > 0 ? _selectedSpellArray[_selectedSpellIndex] : null; // Default set to the first spell in the group might change later
            Debug.Log($"magic array length: {magicArray.Length}, _selectedSpell null?: {_selectedSpell == null}");
        }
        else
        {
            _currentWandInventoryItem = null;
            _currentWandItemSO = null;
            _selectedSpellArray = null;
            _selectedSpell = null;
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