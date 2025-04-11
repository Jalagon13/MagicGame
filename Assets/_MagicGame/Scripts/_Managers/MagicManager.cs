using System;
using System.Collections.Generic;
using UnityEngine;

public struct LoadedSpell
{
    public SpellItemSO SpellToCast;
    public SyncSpellData SpellData;
    public InventoryItem StaffUsedForCast;

    public LoadedSpell(SpellItemSO spellToCast, SyncSpellData spellData, InventoryItem staffUsedForCast)
    {
        SpellToCast = spellToCast;
        SpellData = spellData;
        StaffUsedForCast = staffUsedForCast;
    }
}

public class MagicManager : MonoBehaviour
{
    public event EventHandler OnSpellWheelOpened;
    public event EventHandler OnSpellWheelClosed;
    public static MagicManager Instance { get; private set; }
    
    public SpellbookInventoryItem EquippedSpellBook { get; private set; }
    public bool HasEquippedSpellBook => EquippedSpellBook != null;
    public MagicItemSO SelectedSpell { get; private set; }
    public Timer CastTimeTimer { get; private set; }
    
    private bool _isSpellWheelOpen;
    private LoadedSpell _loadedSpell;
    
    private void Awake()
    {
        Instance = this;
        CastTimeTimer = new Timer(0);
    }
    
    private void Start()
    {
        GameInput.Instance.OnSpaceStarted += OpenSpellWheel;
        GameInput.Instance.OnSpaceCanceled += CloseSpellWheel;
        HotbarManager.Instance.OnFocusSlotUpdated += CheckForSelectedItemChange;
    }

    private void Update()
    {
        CastTimeTimer.Tick(Time.deltaTime);

        if (CanCastSelectedSpell())
        {
            LoadSpell();
        }
    }

    private void CheckForSelectedItemChange(object sender, HotbarManager.OnFocusItemSetEventArgs e)
    {
        InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);

        if (CastTimeTimer.RemainingSeconds > 0)
        {
            Debug.Log($"Id: {selectedInventoryItem.Id} | _loadedSpell.Id: {_loadedSpell.StaffUsedForCast.Id}");
            if(selectedInventoryItem.Id != _loadedSpell.StaffUsedForCast.Id)
            {
                // Selected another item that wasn't the item used on to cast the spell
                CancelSpellCharge();
            }
        }
    }

    private void LoadSpell()
    {
        // Your spell-casting logic here
        SpellItemSO spell = SelectedSpell as SpellItemSO;
        InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
        _loadedSpell = new(spell, spell.LoadSpell(EquippedSpellBook.Item as SpellBookItemSO, null), selectedInventoryItem);

        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(spell.HasteMultiplier);
        Player.LocalClientInstance.PlayerVisuals.PlayChargeVFXClientRpc(GameManager.Instance.GetItemIdFromItemSO(_loadedSpell.SpellToCast));

        CastTimeTimer = new((SelectedSpell as SpellItemSO).CastTime);
        CastTimeTimer.OnTimerEnd += ExecuteSpell;
        
        Debug.Log($"Casting {SelectedSpell.Name}... ({(SelectedSpell as SpellItemSO).CastTime}sec)");
    }

    private void ExecuteSpell(object sender, EventArgs e)
    {
        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1f);
        Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();
        PlayerStats.Instance.SubtractMana(_loadedSpell.SpellToCast.ManaCost);

        _loadedSpell.SpellToCast.ExecuteSpell(EquippedSpellBook.Item as SpellBookItemSO, _loadedSpell.SpellData.SpellId);
        CastTimeTimer.OnTimerEnd -= ExecuteSpell;
        
        Debug.Log($"Executing {SelectedSpell.Name}!!!!");
    }

    private void CancelSpellCharge()
    {
        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1f);
        Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();

        _loadedSpell.SpellToCast.CancelSpell(_loadedSpell.SpellData.SpellId);

        CastTimeTimer.OnTimerEnd -= ExecuteSpell;
        CastTimeTimer = new Timer(0);
        
        Debug.Log($"Cast was interrupted. Reseting spellToCast values");
    }

    private bool CanCastSelectedSpell()
    {
        bool primaryHeld = GameInput.Instance.GetPrimaryHeldDown();
        bool hasSpellbook = HasEquippedSpellBook;
        bool hasSelectedSpell = SelectedSpell != null;
        bool selectedItemExists = InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
        bool isStaffItem = selectedItemExists && selectedInventoryItem.Item is StaffItemSO;
        bool isCastTimeOver = CastTimeTimer.RemainingSeconds <= 0;
        bool hasEnoughMana = SelectedSpell != null && PlayerStats.Instance.CurrentMana >= (SelectedSpell as SpellItemSO).ManaCost;

        return primaryHeld && hasSpellbook && hasSelectedSpell && isStaffItem && isCastTimeOver && !_isSpellWheelOpen && !Pointer.IsOverUI() && !Pointer.IsOverInteractable() && hasEnoughMana;
    }

    public List<MagicItemSO> GetSpells()
    {
        if (HasEquippedSpellBook)
        {
            List<MagicItemSO> spellList = new();
            
            foreach (var spell in EquippedSpellBook.MagicArray)
            {
                if(spell != null) spellList.Add(spell);
            }
            
            return spellList.Count > 0 ? spellList : null;
        }
        
        return null;
    }

    #region Setters and Clears
    
    private void OpenSpellWheel(object sender, EventArgs e)
    {
        _isSpellWheelOpen = true;
        OnSpellWheelOpened?.Invoke(this, EventArgs.Empty);
    }

    private void CloseSpellWheel(object sender, EventArgs e)
    {
        _isSpellWheelOpen = false;
        OnSpellWheelClosed?.Invoke(this, EventArgs.Empty);
    }

    public void SetSelectedSpell(MagicItemSO spell)
    {
        SelectedSpell = spell;
    }

    public void ClearSelectedSpell()
    {
        SelectedSpell = null;
    }

    public void SetEquippedSpellBook(SpellbookInventoryItem spellbook)
    {
        EquippedSpellBook = spellbook;
    }

    public SpellbookInventoryItem RemoveEquippedSpellBook()
    {
        SpellbookInventoryItem oldSpellbook = EquippedSpellBook;
        EquippedSpellBook = null;

        return oldSpellbook;
    }

    public SpellbookInventoryItem SwapEquippedSpellBook(SpellbookInventoryItem spellbook)
    {
        SpellbookInventoryItem oldSpellbook = EquippedSpellBook;
        EquippedSpellBook = spellbook;

        return oldSpellbook;
    }
    #endregion

    private void OnDestroy()
    {
        GameInput.Instance.OnSpaceStarted -= OpenSpellWheel;
        GameInput.Instance.OnSpaceCanceled -= CloseSpellWheel;
        HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSelectedItemChange;
    }
}
