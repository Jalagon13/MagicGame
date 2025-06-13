using System;
using UnityEngine;

public class SpellInputHandler
{
    public event EventHandler OnSpellArrayUpdated;

    private SpellItemSO[] _equippedSpells;
    public SpellItemSO[] EquippedSpells => _equippedSpells;
    
    private Player _player;
    private int? _currentEquippedSpellIndexCasting = null;
    public int? CurrentEquippedSpellIndexCasting => _currentEquippedSpellIndexCasting;
    
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
        if(e.SelectedItemId == _player.SpellCaster.CurrentSpellData.SpellItemId)
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
                        _currentEquippedSpellIndexCasting = slotIndex;
                        _player.SpellCaster.TryCastSpell(spell, GetExecutionParams);
                    }
                    else
                    {
                        // If the spell is not valid or cannot be cast, reset the casting index
                        _currentEquippedSpellIndexCasting = null;
                    }
                    break;
                }
            }
        }
    }

    private (Vector3 spawnPoint, Vector3 direction) GetExecutionParams()
    {
        Vector2 point = _player.PlayerHand.SpellSpawnTransform.position;
        Vector2 direction = (ActionManager.MouseWorldPosition - point).normalized;
        
        return (point, direction);
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
