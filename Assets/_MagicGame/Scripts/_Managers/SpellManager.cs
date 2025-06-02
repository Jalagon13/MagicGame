using System;
using System.Collections.Generic;
using Unity.Netcode;
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

public class SpellManager : NetworkBehaviour
{  
    public static SpellManager Instance { get; private set; }
    
    public event EventHandler OnSpellArrayUpdated;
    public event EventHandler OnSpellCooldownTimersUpdated;
    public event EventHandler<ExecuteSpellsEventArgs> OnExecuteSpells;
    public class ExecuteSpellsEventArgs : EventArgs
    {
        public Vector2 SpawnPoint;
        public Vector2 Direction;
    }
    public event EventHandler OnCancelSpells;

    public Timer CastTimeTimer { get; private set; }
    public Dictionary<int, Timer> SpellCooldownTimers { get; private set; } = new(); // Id of the spell on CD and the CD timer associated with it
    public SpellItemSO[] SpellItemArray { get; private set; } // Holds the array of spells from the wand that is currently selected
    public bool IsContinuouslyCasting { get; set; }

    private LoadedSpell _loadedSpell;
    private readonly int[] _spellSlotPriority = new int[]
    {
        0, // Left Click (Primary)
        1, // Right Click (Secondary)
        2, // Shift
        3  // Space
    };

    private void Awake()
    {
        Instance = this;
        CastTimeTimer = new Timer(0);

        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback += RegisterSelectedItemIndexChangeFunctionality;
        }
    }

    private void RegisterSelectedItemIndexChangeFunctionality(ulong clientId)
    {
        if (NetworkManager.LocalClientId != clientId) return;

        Player.LocalClientInstance.SelectedItemIndexNetworkVariable.OnValueChanged += HandleItemIndexChanged;
    }

    private void Start()
    {
        HotbarManager.Instance.OnFocusSlotUpdated += CheckForSelectedItemChange;
    }

    private void Update()
    {
        if(Player.LocalClientInstance == null) return;
    
        HandleTimers();

        if (SpellItemArray != null && !IsContinuouslyCasting && CastTimeTimer.RemainingSeconds <= 0)
        {
            foreach (int slotIndex in _spellSlotPriority)
            {
                if (slotIndex < SpellItemArray.Length && IsSpellKeyHeld(slotIndex))
                {
                    // Insert spell casting logic here
                    AttemptToCastSpellAtSlot(slotIndex);
                    break;
                }
            }
        }
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

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SpawnSpellServerRpc(SyncSpellData spellData, Vector2 loadPoint, RpcParams rpcParams = default)
    {
        Spell spell = Instantiate((GameManager.Instance.GetItemSOFromItemId(spellData.SpellIndex) as SpellItemSO).SpellProjectilePrefab, loadPoint, Quaternion.identity);

        NetworkObject no = spell.GetComponent<NetworkObject>();
        no.SpawnWithObservers = false;
        no.SpawnWithOwnership(spellData.OwnerPlayerId, true);

        spell.SpellData.Value = spellData;
        spell.GetComponent<SpellNetworkComponent>().InitializeSpellNetwork(spellData);
    }

    private void AttemptToCastSpellAtSlot(int slotIndex)
    {
        SpellItemSO spell = SpellItemArray[slotIndex];
        if (spell != null && CanCastSelectedSpell(spell))
        {
            spell.StartSpell(slotIndex);
        }
    }

    private void HandleItemIndexChanged(int previousValue, int newValue)
    {
        if(GameManager.Instance.GetItemSOFromItemId(newValue) is SpellItemSO spellItemSO)
        {
            SpellItemArray = new SpellItemSO[] { spellItemSO };
        }
        else if(GameManager.Instance.GetItemSOFromItemId(newValue) is WandItemSO wandItemSO)
        {
            InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
            SpellItemArray = (selectedInventoryItem as WandInventoryItem).MagicArray;
        }
        else
        {
            SpellItemArray = null;
        }

        OnSpellArrayUpdated?.Invoke(this, EventArgs.Empty);
    }

    private bool CanCastSelectedSpell(SpellItemSO spell)
    {
        bool isOverUI = Pointer.IsOverUI();
        bool isOverInteractable = Pointer.IsOverInteractable();
        bool isOnCooldown = SpellCooldownTimers.ContainsKey(GameManager.Instance.GetItemIdFromItemSO(spell));
        // bool hasEnoughMana = Player.LocalClientInstance.PlayerStats.CurrentMana >= spell.ManaCost;

        return !isOverUI && !isOverInteractable && !isOnCooldown /* && hasEnoughMana */;
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
            if(selectedInventoryItem.Id != _loadedSpell.StaffUsedForCast.Id)
            {
                // Selected another item that wasn't the item used on to cast the spell
                CancelSpellCharge();
            }
        }
    }

    public void LoadSpell(SpellItemSO spellToCast, LoadedSpell loadedSpell)
    {
        _loadedSpell = loadedSpell;

        CastTimeTimer = new Timer(spellToCast.CastTime);
        CastTimeTimer.OnTimerEnd += ExecuteSpell;
    }
    
    public void SubtractManaAndSetCooldown(SpellItemSO spellToCast)
    {
        // PlayerStats.Instance.SubtractMana(spellToCast.ManaCost);

        int selectedSpellId = GameManager.Instance.GetItemIdFromItemSO(spellToCast);
        SpellCooldownTimers[selectedSpellId] = new Timer(spellToCast.Cooldown);
    }

    private void ExecuteSpell(object sender, EventArgs e)
    {
        // Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1f);
        Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();
        
        Vector2 spawnPoint = NetworkManager.Singleton.ConnectedClients[Player.LocalClientInstance.OwnerClientId].PlayerObject.GetComponent<Player>().PlayerHand.SpellSpawnTransform.position;
        Vector2 baseDirection = (ActionManager.MouseWorldPosition - spawnPoint).normalized;
        // Player.LocalClientInstance.PlayerKnockback.ApplyKnockback(ActionManager.MouseWorldPosition, 0, _loadedSpell.SpellToCast.Recoil);
        SoundManager.Instance.PlayOneShot(_loadedSpell.SpellToCast.SpellCastSound, Player.LocalClientInstance.PlayerHand.SpellSpawnTransform.position);

        OnExecuteSpells?.Invoke(this, new ExecuteSpellsEventArgs 
        { 
            SpawnPoint = spawnPoint, 
            Direction = baseDirection 
        });
        
        int selectedSpellId = GameManager.Instance.GetItemIdFromItemSO(_loadedSpell.SpellToCast);
        
        if(_loadedSpell.SpellToCast.IsContinuousCast)
        {
            IsContinuouslyCasting = true;
        }
        else
        {
            SubtractManaAndSetCooldown(_loadedSpell.SpellToCast);
        }

        _loadedSpell = new();

        CastTimeTimer.OnTimerEnd -= ExecuteSpell;
    }

    private void CancelSpellCharge()
    {
        if(_loadedSpell.SpellToCast.IsContinuousCast)
        {
            IsContinuouslyCasting = false;
        }

        _loadedSpell = new();

        // Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1f);
        Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();

        OnCancelSpells?.Invoke(this, EventArgs.Empty);

        CastTimeTimer.OnTimerEnd -= ExecuteSpell;
        CastTimeTimer = new Timer(0);
    }

    // private bool IsSelectedSpellOnCooldown()
    // {
    //     int selectedSpellId = GameManager.Instance.GetItemIdFromItemSO(SelectedSpell);
        
    //     return SpellCooldownTimers.ContainsKey(selectedSpellId);
    // }

    public override void OnDestroy()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= RegisterSelectedItemIndexChangeFunctionality;
        }

        HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSelectedItemChange;
    }
}
