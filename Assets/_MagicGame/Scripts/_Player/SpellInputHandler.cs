using System;
using UnityEngine;

public class SpellInputHandler
{
    public event EventHandler OnSpellArrayUpdated;

    private SpellItemSO[] _equippedSpells;
    private Player _player;
    
    public SpellInputHandler(Player player)
    {
        _player = player;
        _player.SelectedItemIdNetworkVariable.OnValueChanged += OnItemIdChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
        HotbarManager.Instance.OnFocusSlotUpdated += CheckForSelectedItemChange;
    }
    
    public void Dispose()
    {
        _player.SelectedItemIdNetworkVariable.OnValueChanged -= OnItemIdChanged;
        _player.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
        HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSelectedItemChange;
    }

    private void CheckForSelectedItemChange(object sender, HotbarManager.OnFocusItemSetEventArgs e)
    {
        // If an invetnory slot was selected, cancel spell casting
        if (_player.SpellCaster.CastTimer.IsRunning)
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }

    private void OnPlayerLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if (previousValue == LifeState.Alive && newValue == LifeState.Dead)
        {
            _player.SpellCaster.TryToCancelCast();
        }
    }

    private readonly int[] _spellSlotPriority = new int[]
    {
        0, // Left Click (Primary)
        1, // Right Click (Secondary)
        2, // Shift
        3  // Space
    };
    
    public void DetectSpellInputs()
    {
        if (_player.ServerCharacter.LifeState == LifeState.Alive && _equippedSpells != null)
        {
            foreach (int slotIndex in _spellSlotPriority)
            {
                if (slotIndex < _equippedSpells.Length && IsSpellKeyHeld(slotIndex))
                {
                    // Insert spell casting logic here
                    SpellItemSO spell = _equippedSpells[slotIndex];
                    if (spell != null && CanCastSelectedSpell(spell))
                    {
                        _player.SpellCaster.TryCastSpell(spell);
                    }
                    break;
                }
            }
        }
    }

    private bool CanCastSelectedSpell(SpellItemSO spell)
    {
        bool isOverUI = Pointer.IsOverUI();
        bool isOverInteractable = Pointer.IsOverInteractable();
        // bool hasEnoughMana = Player.LocalClientInstance.PlayerStats.CurrentMana >= spell.ManaCost;

        return !isOverUI && !isOverInteractable /* && hasEnoughMana */;
    }

    public bool IsSpellKeyHeld(int slotIndex)
    {
        return slotIndex switch
        {
            0 => GameInput.Instance.GetPrimaryHeldDown(),
            1 => GameInput.Instance.GetSecondaryHeldDown(),
            2 => GameInput.Instance.GetShiftHeldDown(),
            3 => GameInput.Instance.GetSpaceHeldDown(),
            _ => false,
        };
    }

    private void OnItemIdChanged(int previousValue, int newValue)
    {
        Debug.Log($"SpellInputHandler New item id: {newValue}");
        if (GameManager.Instance.GetItemSOFromItemId(newValue) is SpellItemSO spellItemSO)
        {
            _equippedSpells = new SpellItemSO[] { spellItemSO };
        }
        else if (GameManager.Instance.GetItemSOFromItemId(newValue) is WandItemSO wandItemSO)
        {
            InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
            _equippedSpells = (selectedInventoryItem as WandInventoryItem).MagicArray;
        }
        else
        {
            _equippedSpells = null;
        }

        OnSpellArrayUpdated?.Invoke(this, EventArgs.Empty);
    }
}
