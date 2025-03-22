using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Wand
{
	public WandInventoryItem WandInvItem { get; private set; }
	public WandItemSO WandSO { get; private set; }
	public Timer CastTimeTimer { get; private set; }
	public float CurrentReload { get; private set; }
	public float TotalReloadDuration { get; private set; }
	public bool IsSelected { get; private set; }
	
	private float _castDelayTimer;
	private Queue<int> _validMagicArrayIndexes = new();
	private List<LoadedSpell> _loadedSpells = new();
	private struct LoadedSpell
	{
		public SpellItemSO SpellToCast;
		public SyncSpellData SpellData;
		
		public LoadedSpell(SpellItemSO spellToCast, SyncSpellData spellData)
		{
			SpellToCast = spellToCast;
			SpellData = spellData;
		}
	}


	public Wand(WandInventoryItem wandInventoryItem)
	{
		WandInvItem = wandInventoryItem;
		WandSO = WandInvItem.Item as WandItemSO;
		WandInvItem.OnWandContentsUpdated += OnWandContentsUpdated;

		CastTimeTimer = new Timer(0);

		ResetValidMagicIndexes();
	}

	private void OnWandContentsUpdated(object sender, EventArgs e)
	{
		Debug.Log($"Resseting valid magic idnexes");
	
		ResetValidMagicIndexes();
	}

	public void Tick(float deltaTime)
	{
		CastTimeTimer.Tick(deltaTime);
		
		if(CastTimeTimer.RemainingSeconds > 0 && !IsSelected)
		{
			// Cast was interrupted
			CancelSpellCharge();
		}

		if(CastTimeTimer.RemainingSeconds <= 0)
		{
			if (_castDelayTimer > 0)
			{
				_castDelayTimer -= deltaTime;
			}

			CurrentReload += deltaTime; // Regen recharge over time
			CurrentReload = Mathf.Min(CurrentReload, TotalReloadDuration); // Clamp to prevent overfilling
		}
	}
	
	public void CastSpell()
	{
		if (_castDelayTimer > 0 || CurrentReload < TotalReloadDuration) return; // Cast Delay or recharge ongoing return

		TryToRefillValidMagicArrayIndexes();

		if (_validMagicArrayIndexes.Count == 0) return; // If still empty after fill, return

		switch (WandInvItem.MagicArray[_validMagicArrayIndexes.Peek()])
		{
			case MultiCastItemSO multiCast:
				HandleMultiCast(multiCast);
				break;
			case SpellItemSO spellToCast:
				HandleSingleSpellCast(spellToCast);
				break;
			case SpellModItemSO spellMod:
				HandleSpellModCast(spellMod);
				break;
		}
	}
	
	private bool OverFriendlyNpc(out Npc friendlyNpc)
	{
		var colliders = Physics2D.OverlapPointAll(ActionManager.MouseWorldPosition);
		foreach (var collider in colliders)
		{
			if (collider.CompareTag("FriendlyNpc"))
			{
				friendlyNpc = collider.GetComponent<Npc>();
				return true;
			}
		}
		friendlyNpc = null;
		return false;
	}

    private void HandleSingleSpellCast(SpellItemSO spellToCast, List<int> spellModsFound = null)
	{
		int totalManaCost = spellToCast.ManaCost;
		
		if (spellModsFound != null)
		{
			foreach (int spellModIndex in spellModsFound)
			{
				totalManaCost += (GameManager.Instance.GetItemSOFromItemId(spellModIndex) as SpellModItemSO).ManaCost;
			}
		}
	
	    if(totalManaCost > PlayerStats.Instance.CurrentMana) return;
	    
		_validMagicArrayIndexes.Dequeue();
	    
		StartSpellCharges(new List<KeyValuePair<SpellItemSO, List<int>>> { new KeyValuePair<SpellItemSO, List<int>>(spellToCast, spellModsFound) });
	}

    private void HandleSpellModCast(SpellModItemSO spellMod)
    {
		if(RemainingSpellExists())
		{
			List<int> spellModsFound = new();
			int totalManaCost = 0;

			foreach (int validMagicIndex in _validMagicArrayIndexes)
			{
				MagicItemSO magic = WandInvItem.MagicArray[validMagicIndex];

				if (magic is SpellModItemSO spellModItemSO)
				{
					spellModsFound.Add(GameManager.Instance.GetItemIdFromItemSO(spellModItemSO));
					totalManaCost += spellModItemSO.ManaCost;
				}
				else if (magic is SpellItemSO spellToCast)
				{
					totalManaCost += spellToCast.ManaCost;

					if (totalManaCost <= PlayerStats.Instance.CurrentMana)
					{
						for (int i = 0; i < spellModsFound.Count; i++)
						{
							_validMagicArrayIndexes.Dequeue();
						}

						HandleSingleSpellCast(spellToCast, spellModsFound);
					}

					return;
				}
			}
		}
	}

    private void HandleMultiCast(MultiCastItemSO multiCast)
	{
		_validMagicArrayIndexes.Dequeue();

		if (!RemainingSpellExists()) return;

		List<KeyValuePair<SpellItemSO, List<int>>> spellsAndModsFound = new();
		List<int> modIndexHolder = new();
		int totalManaCost = 0;

		while (RemainingSpellExists())
		{
			if(totalManaCost >= PlayerStats.Instance.CurrentMana || spellsAndModsFound.Count == multiCast.MultiCastAmount) break;
			
			MagicItemSO nextMagic = WandInvItem.MagicArray[_validMagicArrayIndexes.Peek()];

			if (nextMagic is SpellItemSO spell)
			{
				spellsAndModsFound.Add(new(spell, modIndexHolder));
				modIndexHolder = new();
				totalManaCost += spell.ManaCost;
				_validMagicArrayIndexes.Dequeue();
			}
			else if (nextMagic is SpellModItemSO spellMod)
			{
				modIndexHolder.Add(GameManager.Instance.GetItemIdFromItemSO(spellMod));
				totalManaCost += spellMod.ManaCost;
				_validMagicArrayIndexes.Dequeue();
			}
		}

		StartSpellCharges(spellsAndModsFound);
	}

	private void StartSpellCharges(List<KeyValuePair<SpellItemSO, List<int>>> spellsAndMods)
	{
		_loadedSpells = new();
		
		float longestCastTime = 0;
		float highestHasteMult = 0;
		int totalManaCost = 0;
		float totalCastDelay = 0;
	
	    foreach (var spellAndMod in spellsAndMods)
		{
			LoadedSpell loadedSpell = new(spellAndMod.Key, spellAndMod.Key.LoadSpell(WandSO, spellAndMod.Value));
			if(spellAndMod.Value != null)
			{
				foreach (int modifierIndex in spellAndMod.Value)
				{
					SpellModItemSO modifier = GameManager.Instance.GetItemSOFromItemId(modifierIndex) as SpellModItemSO;
					loadedSpell.SpellData = modifier.SpellModifierPrefab.GetComponent<ISpellModifier>().ModifiySpellData(loadedSpell.SpellData);
				}
			}

			_loadedSpells.Add(loadedSpell);
			
			totalManaCost += spellAndMod.Key.ManaCost;
			totalCastDelay += spellAndMod.Key.CastDelay;

			if (spellAndMod.Key.CastTime > longestCastTime)
			{
				longestCastTime = spellAndMod.Key.CastTime;
			}
			
			if(loadedSpell.SpellData.HasteMultiplier > highestHasteMult)
			{
			    highestHasteMult = loadedSpell.SpellData.HasteMultiplier;
			}
		}

		PlayerStats.Instance.SubtractMana(totalManaCost);

		if (RemainingSpellExists())
		{
			_castDelayTimer = WandSO.BaseCastDelay + totalCastDelay;
		}
		else
		{
			CurrentReload = 0;
			TotalReloadDuration = WandSO.ReloadDuration + totalCastDelay;
		}
		
		Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(highestHasteMult);
		Player.LocalClientInstance.PlayerVisuals.PlayChargeVFXClientRpc(GameManager.Instance.GetItemIdFromItemSO(_loadedSpells[0].SpellToCast));

		CastTimeTimer = new(longestCastTime);
		CastTimeTimer.OnTimerEnd += ExecuteSpells;
		Debug.Log($"Charging Spells...");
	}
	
    private void ExecuteSpells(object sender, EventArgs e)
    {
		Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1f);
		Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();

		foreach (LoadedSpell loadedSpell in _loadedSpells)
		{
			loadedSpell.SpellToCast.ExecuteSpell(WandSO, loadedSpell.SpellData.SpellId);
		}
    
		CastTimeTimer.OnTimerEnd -= ExecuteSpells;
		Debug.Log($"Executing Spells");
	}
	
	private void CancelSpellCharge()
	{
		foreach (LoadedSpell loadedSpell in _loadedSpells)
		{
			loadedSpell.SpellToCast.CancelSpell(loadedSpell.SpellData.SpellId);
		}
	
		_loadedSpells.Clear();

		CastTimeTimer.OnTimerEnd -= ExecuteSpells;
		CastTimeTimer = new Timer(0);
		Debug.Log($"Cast was interrupted. Reseting spellToCast values");
	}
	
	private bool RemainingSpellExists()
	{
		if(_validMagicArrayIndexes.Count <= 0) return false;
	    
		foreach (int validMagicIndex in _validMagicArrayIndexes)
		{
			if(WandInvItem.MagicArray[validMagicIndex] is SpellItemSO) 
			{
				return true;
			}
		}
		
		return false;
	}

    private void ResetValidMagicIndexes()
	{
		_validMagicArrayIndexes.Clear();
	}

	private void TryToRefillValidMagicArrayIndexes()
	{
		if(!RemainingSpellExists() || _validMagicArrayIndexes.Count == 0)
		{
			ResetValidMagicIndexes();

			for (int i = 0; i < WandInvItem.MagicArray.Length; i++)
			{
				if (WandInvItem.MagicArray[i] != null)
				{
					_validMagicArrayIndexes.Enqueue(i);
				}
			}
		}
	}
	
	public void SetSelected(bool value)
	{
	    IsSelected = value;

		if (CastTimeTimer.RemainingSeconds > 0 && IsSelected)
		{
			CancelSpellCharge();
		}
	}
}
