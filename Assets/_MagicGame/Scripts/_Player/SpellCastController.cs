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

public struct SpellCastGroup
{
    public List<SpellMetaData> SpellsToCast;

    public bool IsMultiCast => SpellsToCast.Count > 1;

    public SpellCastGroup(List<SpellMetaData> spells)
    {
        SpellsToCast = spells;
    }
}

public class SpellCastController
{
    private static readonly float _postCastDelayTimerDuration = 0.15f;

    private Player _player;
    private int _currentSpellIndex;
    private WandItemSO _currentWandItemSO;
    private List<SpellCastGroup> _spellCastGroups = new();
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
        if (previousValue && !newValue)
        {
            _postCastDelayTimer.Reset();

            if (InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem) && selectedInventoryItem.Item is WandItemSO wandItemSO)
            {
                if (_currentSpellIndex >= _spellCastGroups.Count - 1)
                {
                    _wandManaSystem.StartWandRecharge(selectedInventoryItem.Id, wandItemSO.RechargeTime);
                }

                _currentSpellIndex = (_currentSpellIndex + 1) % _spellCastGroups.Count;

                _wandManaSystem.TrySpendMana(selectedInventoryItem.Id, CalculateGroupManaCost(_spellCastGroups[_currentSpellIndex]));
            }
        }
    }

    public void SpellCastControllerUpdate()
    {
        _wandManaSystem.Tick(Time.deltaTime, _currentWandInventoryItem);
        _postCastDelayTimer.Tick(Time.deltaTime);

        if (CanCast())
        {
            _player.SpellCaster.TryCastSpell(_spellCastGroups[_currentSpellIndex], GetExecutionParams);
        }
    }

    private bool CanCast()
    {
        if (_spellCastGroups == null || _spellCastGroups.Count == 0) return false;

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
            hasEnoughMana = _wandManaSystem.GetCurrentMana(selectedInventoryItem.Id) >= CalculateGroupManaCost(_spellCastGroups[_currentSpellIndex]);
        }

        return !isOverUI && !isOverInteractable && hasEnoughMana && playerIsAlive && primaryHeldDown && !isCasting && !isWandRecharging && !postCastDelayTimerRunning && !isLoadingBiome;
    }

    public bool IsWandRecharging(out Timer rechargeTimer)
    {
        if (InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem))
        {
            return _wandManaSystem.IsWandRecharging(selectedInventoryItem.Id, out rechargeTimer);
        }

        rechargeTimer = null;
        return false;
    }

    private (Vector3 spawnPoint, Vector3 direction) GetExecutionParams()
    {
        var group = _spellCastGroups[_currentSpellIndex];
        var firstSpell = group.SpellsToCast[0];

        float wandAccuracy = _currentWandItemSO?.Accuracy ?? 0f;
        float spellAccuracy = firstSpell.SpellItem.Scatter;
        float totalSpellModAccuracy = 0;

        foreach (var mod in firstSpell.SpellMods)
        {
            totalSpellModAccuracy += mod.Scatter;
        }

        float totalAccuracy = Mathf.Max(0f, wandAccuracy + spellAccuracy + totalSpellModAccuracy);
        Vector2 point = _player.PlayerHand.SpellSpawnTransform.position;
        Vector2 baseDirection = (ActionManager.MouseWorldPosition - point).normalized;
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
            _currentWandItemSO = wandItemSO;
            MagicItemSO[] magicArray = (_currentWandInventoryItem as WandInventoryItem).MagicArray;

            _spellCastGroups = new();
            List<SpellModItemSO> currentMods = new();

            for (int i = 0; i < magicArray.Length;)
            {
                MagicItemSO item = magicArray[i];

                if (item is SpellModItemSO mod)
                {
                    currentMods.Add(mod);
                    i++;
                }
                else if (item is SpellItemSO spell)
                {
                    _spellCastGroups.Add(new SpellCastGroup(new List<SpellMetaData>
                    {
                        new SpellMetaData(spell, new List<SpellModItemSO>(currentMods))
                    }));
                    currentMods.Clear();
                    i++;
                }
                else if (item is MultiCastItemSO multi)
                {
                    List<SpellMetaData> groupedSpells = new();
                    int count = 0;
                    i++; // skip the multicast

                    while (i < magicArray.Length && count < multi.SpellCastAmount)
                    {
                        if (magicArray[i] is SpellModItemSO mod2)
                        {
                            currentMods.Add(mod2);
                            i++;
                        }
                        else if (magicArray[i] is SpellItemSO spell2)
                        {
                            groupedSpells.Add(new SpellMetaData(spell2, new List<SpellModItemSO>(currentMods)));
                            currentMods.Clear();
                            count++;
                            i++;
                        }
                        else
                        {
                            i++;
                        }
                    }

                    if (groupedSpells.Count > 0)
                    {
                        _spellCastGroups.Add(new SpellCastGroup(groupedSpells));
                    }
                }
                else
                {
                    i++;
                }
            }

            _currentSpellIndex = 0;
        }
        else
        {
            _currentWandInventoryItem = null;
            _currentWandItemSO = null;
            _spellCastGroups = null;
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
        // if (e.SelectedItemId == _player.SpellCaster.CurrentSpellData.SpellItemId)
        // {
        //     Debug.Log("Cannot cancel if the spell is the same one being cast.");
        //     return;
        // }

        // if (_player.SpellCaster.IsCasting.Value)
        // {
        //     _player.SpellCaster.TryToCancelCast();
        // }
    }

    private int CalculateTotalManaCost(SpellMetaData spellMeta)
    {
        int total = spellMeta.SpellItem.ManaCost;
        foreach (var mod in spellMeta.SpellMods)
        {
            total += mod.ManaCost;
        }
        return total;
    }

    private int CalculateGroupManaCost(SpellCastGroup group)
    {
        int total = 0;
        foreach (var spell in group.SpellsToCast)
        {
            total += CalculateTotalManaCost(spell);
        }
        return total;
    }
}