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

public class SpellManager : MonoBehaviour
{  
    public static SpellManager Instance { get; private set; }
    
    public event EventHandler OnSpellbookUpdated;
    public event EventHandler OnSpellWheelOpened;
    public event EventHandler OnSpellWheelClosed;
    public event EventHandler OnSelectedSpellUpdated;
    public event EventHandler OnSpellCooldownTimersUpdated;
    
    public SpellbookInventoryItem EquippedSpellBook { get; private set; }
    public bool HasEquippedSpellBook => EquippedSpellBook != null;
    public SpellItemSO SelectedSpell { get; private set; }
    public Timer CastTimeTimer { get; private set; }
    public Dictionary<int, Timer> SpellCooldownTimers { get; private set; } = new(); // Id of the spell on CD and the CD timer associated with it
    
    private bool _isSpellWheelOpen;
    private LoadedSpell _loadedSpell;
    private List<SpellItemSO> _spellsEquipped = new();

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
        HandleTimers();

        if (CanCastSelectedSpell())
        {
            LoadSpell();
        }
    }
    
    private void HandleTimers()
    {
        CastTimeTimer.Tick(Time.deltaTime);

        foreach (int key in new List<int>(SpellCooldownTimers.Keys)) // To avoid modifying the collection while iterating
        {
            Timer spellCdTimer = SpellCooldownTimers[key];
            spellCdTimer.Tick(Time.deltaTime);

            if (spellCdTimer.RemainingSeconds <= 0)
            {
                SpellCooldownTimers.Remove(key);
            }
        }

        if (SpellCooldownTimers.Count > 0)
        {
            OnSpellCooldownTimersUpdated?.Invoke(this, EventArgs.Empty);
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
        InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
        _loadedSpell = new(SelectedSpell, SelectedSpell.LoadSpell(EquippedSpellBook.Item as SpellBookItemSO), selectedInventoryItem);

        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(SelectedSpell.HasteMultiplier);
        Player.LocalClientInstance.PlayerVisuals.PlayChargeVFXClientRpc(GameManager.Instance.GetItemIdFromItemSO(_loadedSpell.SpellToCast));

        CastTimeTimer = new(SelectedSpell.CastTime);
        CastTimeTimer.OnTimerEnd += ExecuteSpell;
    }

    private void ExecuteSpell(object sender, EventArgs e)
    {
        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1f);
        Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();
        PlayerStats.Instance.SubtractMana(_loadedSpell.SpellToCast.ManaCost);

        _loadedSpell.SpellToCast.ExecuteSpell(EquippedSpellBook.Item as SpellBookItemSO, _loadedSpell.SpellData.SpellId);
        
        int selectedSpellId = GameManager.Instance.GetItemIdFromItemSO(_loadedSpell.SpellToCast);
        SpellCooldownTimers[selectedSpellId] = new Timer(_loadedSpell.SpellToCast.Cooldown);

        _loadedSpell = new();

        CastTimeTimer.OnTimerEnd -= ExecuteSpell;
    }

    private void CancelSpellCharge()
    {
        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1f);
        Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();

        _loadedSpell.SpellToCast.CancelSpell(_loadedSpell.SpellData.SpellId);

        CastTimeTimer.OnTimerEnd -= ExecuteSpell;
        CastTimeTimer = new Timer(0);
    }

    private bool CanCastSelectedSpell()
    {
        bool primaryHeldDown = GameInput.Instance.GetPrimaryHeldDown();
        bool hasSpellbook = HasEquippedSpellBook;
        bool hasSelectedSpell = SelectedSpell != null;
        bool selectedItemExists = InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
        bool isStaffItem = selectedItemExists && selectedInventoryItem.Item is StaffItemSO;
        bool isCastTimeOver = CastTimeTimer.RemainingSeconds <= 0;
        bool hasEnoughMana = SelectedSpell != null && PlayerStats.Instance.CurrentMana >= SelectedSpell.ManaCost;
        bool selectedSpellOnCooldown = IsSelectedSpellOnCooldown();

        return primaryHeldDown && hasSpellbook && hasSelectedSpell && isStaffItem && isCastTimeOver && !_isSpellWheelOpen && !Pointer.IsOverUI() && !Pointer.IsOverInteractable() && hasEnoughMana && !selectedSpellOnCooldown;
    }

    private bool IsSelectedSpellOnCooldown()
    {
        int selectedSpellId = GameManager.Instance.GetItemIdFromItemSO(SelectedSpell);
        
        return SpellCooldownTimers.ContainsKey(selectedSpellId);
    }

    public List<SpellItemSO> GetSpells()
    {
        if (HasEquippedSpellBook)
        {
            List<SpellItemSO> spellList = new();
            
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

    public void SetSelectedSpell(SpellItemSO spell)
    {
        SelectedSpell = spell;
        OnSelectedSpellUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSelectedSpell()
    {
        SelectedSpell = null;
        OnSelectedSpellUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void EquipSpellBook(SpellbookInventoryItem spellbook)
    {
        EquippedSpellBook = spellbook;
        OnSpellbookUpdated?.Invoke(this, EventArgs.Empty);

        _spellsEquipped = GetSpells();
        if (_spellsEquipped != null && _spellsEquipped.Count > 0)
        {
            SetSelectedSpell(_spellsEquipped[0]);
        }
        else
        {
            ClearSelectedSpell();
        }
    }

    public SpellbookInventoryItem RemoveEquippedSpellBook()
    {
        ClearSelectedSpell();
        SpellbookInventoryItem oldSpellbook = EquippedSpellBook;
        EquippedSpellBook = null;
        
        OnSpellbookUpdated?.Invoke(this, EventArgs.Empty);
        
        return oldSpellbook;
    }

    public SpellbookInventoryItem SwapEquippedSpellBook(SpellbookInventoryItem spellbook)
    {
        SpellbookInventoryItem oldSpellbook = EquippedSpellBook;
        EquippedSpellBook = spellbook;

        OnSpellbookUpdated?.Invoke(this, EventArgs.Empty);

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
