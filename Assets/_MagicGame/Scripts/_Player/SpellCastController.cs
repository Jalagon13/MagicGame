using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellCastController
{
    private Player _player;
    private int _currentSpellIndex;
    private List<SpellMetaData> _spellMetaDataList = new List<SpellMetaData>();
    
    private struct SpellMetaData
    {
        public SpellItemSO SpellItem;
        public List<SpellModItemSO> SpellMods;

        public SpellMetaData(SpellItemSO spellItem, List<SpellModItemSO> spellMods)
        {
            SpellItem = spellItem;
            SpellMods = spellMods;
        }
        
        
    }

    public SpellCastController(Player player)
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

    public void DetectSpellInputs()
    {
        if(CanCast())
        {
            SpellItemSO spell = _spellMetaDataList[_currentSpellIndex].SpellItem;
            Debug.Log($"Casting spell: {spell.name} at index {_currentSpellIndex} with mods: {_spellMetaDataList[_currentSpellIndex].SpellMods.Count}");
            _player.SpellCaster.TryCastSpell(spell, GetExecutionParams);
            _currentSpellIndex = (_currentSpellIndex + 1) % _spellMetaDataList.Count; // Cycle through spells
        }
    }

    private (Vector3 spawnPoint, Vector3 direction) GetExecutionParams()
    {
        Vector2 point = _player.PlayerHand.SpellSpawnTransform.position;
        Vector2 direction = (ActionManager.MouseWorldPosition - point).normalized;

        return (point, direction);
    }

    private bool CanCast()
    {
        bool isOverUI = Pointer.IsOverUI();
        bool isOverInteractable = Pointer.IsOverInteractable();
        bool playerIsAlive = _player.ServerCharacter.LifeState == LifeState.Alive;
        bool primaryHeldDown = GameInput.Instance.GetPrimaryHeldDown();
        bool isCasting = _player.SpellCaster.IsCasting.Value;
        // bool hasEnoughMana = Player.LocalClientInstance.PlayerStats.CurrentMana >= spell.ManaCost;

        return !isOverUI && !isOverInteractable /* && hasEnoughMana */ && playerIsAlive && primaryHeldDown && !isCasting;
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
}
