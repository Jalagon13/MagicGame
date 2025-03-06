using System;
using System.Collections.Generic;
using UnityEngine;

public class Wand
{
	public WandInventoryItem WandInvItem { get; private set; }
	public float CurrentMana { get; private set; }
	public float CurrentReload { get; private set; }
	public float TotalReloadDuration { get; private set; }
	public WandItemSO WandSO { get; private set; }
	public bool IsSelected { get; private set; }
	
	private Queue<int> _validMagicIndexes = new();
	private float _castDelayTimer;
	private Timer _castTimeTimer;
	private ulong _spellToCastId; // Reference to the spell that is currently being cast
	private SpellItemSO _spellToCast;


	public Wand(WandInventoryItem wandInventoryItem)
	{
		WandInvItem = wandInventoryItem;
		WandSO = WandInvItem.Item as WandItemSO;
		CurrentMana = WandSO.MaxMana;
		WandInvItem.OnWandContentsUpdated += OnWandContentsUpdated;

		_castTimeTimer = new Timer(0);

		ResetValidMagicIndexes();
	}

	private void OnWandContentsUpdated(object sender, EventArgs e)
	{
		Debug.Log($"Resseting valid magic idnexes");
	
		ResetValidMagicIndexes();
	}

	public void Tick(float deltaTime)
	{
		_castTimeTimer.Tick(deltaTime);
		
		if(_castTimeTimer.RemainingSeconds > 0 && !IsSelected)
		{
			// Cast was interrupted
			CancelSpellCharge();
		}

		if(_castTimeTimer.RemainingSeconds <= 0)
		{
			if (_castDelayTimer > 0)
			{
				_castDelayTimer -= deltaTime;
			}

			CurrentReload += deltaTime; // Regen recharge over time
			CurrentReload = Mathf.Min(CurrentReload, TotalReloadDuration); // Clamp to prevent overfilling

			CurrentMana += WandSO.ManaRegenSpeed * deltaTime; // Regenerate mana over time
			CurrentMana = Mathf.Min(CurrentMana, WandSO.MaxMana); // Clamp to prevent overfilling
		}
	}
	
	public void CastSpell()
	{
		if (_castDelayTimer > 0 || CurrentReload < TotalReloadDuration) return; // Cast Delay or recharge ongoing return

		if (_validMagicIndexes.Count == 0) // If validspells is empty, try to fill it up
		{
			TryToRefillValidMagicIndexes();
		}

		if (_validMagicIndexes.Count == 0) return; // If still empty after fill, return

		MagicItemSO magic = WandInvItem.MagicArray[_validMagicIndexes.Peek()];

		switch (magic)
		{
			case MultiCastItemSO multiCast:
				HandleMultiCast(multiCast);
				break;
			case SpellItemSO spellToCast:
				HandleSingleSpellCast(spellToCast);
				break;
			case DestructionCataylstItemSO miningSpell:
				HandleMiningCast(miningSpell);
				break;
			case SpellModItemSO spellMod:
				HandleSpellModCast(spellMod);
				break;
		}
	}

    private void HandleSpellModCast(SpellModItemSO spellMod)
    {
		// Keep looking for valid spell and keep track of any spell mods you come across to also apply it to the spell
		List<int> spellModsFound = new()
        {
            GameManager.Instance.GetItemIdFromItemSO(WandInvItem.MagicArray[_validMagicIndexes.Dequeue()])
        };

		while (_validMagicIndexes.Count > 0)
		{
			MagicItemSO nextMagic = WandInvItem.MagicArray[_validMagicIndexes.Peek()];
			
			if(nextMagic is SpellItemSO spellToCast)
			{
				// Found spell to apply mods to
				HandleSingleSpellCast(spellToCast, spellModsFound);
			}
			else if(nextMagic is SpellModItemSO)
			{
			    spellModsFound.Add(GameManager.Instance.GetItemIdFromItemSO(WandInvItem.MagicArray[_validMagicIndexes.Dequeue()]));
			}
		}
	}

    private void HandleMultiCast(MultiCastItemSO multiCast)
	{
		List<int> spellsShot = new();

		_validMagicIndexes.Dequeue();

		if (_validMagicIndexes.Count == 0)
		{
			TryToRefillValidMagicIndexes();
		}

		int numOfSpellsCast = 0;
		float cumulativeCastDelay = 0;

		while (_validMagicIndexes.Count > 0)
		{
			if (numOfSpellsCast == multiCast.MultiCastAmount)
				break;

			// Dequeue the next valid index to process it
			int validMagicIndex = _validMagicIndexes.Dequeue();

			if (WandInvItem.MagicArray[validMagicIndex] is SpellItemSO potentialSpellToShoot)
			{
				if (spellsShot.Contains(validMagicIndex)) continue; // If spell has been shot already, skip it

				if (potentialSpellToShoot.ManaCost <= CurrentMana)
				{
					CurrentMana -= potentialSpellToShoot.ManaCost;

					StartSpellCharge(potentialSpellToShoot, null);
					numOfSpellsCast++;
					cumulativeCastDelay += potentialSpellToShoot.CastDelay;
					spellsShot.Add(validMagicIndex);
				}
			}

			// If the queue is empty but we still need more spells, try refilling it
			if (_validMagicIndexes.Count == 0 && numOfSpellsCast < multiCast.MultiCastAmount)
			{
				TryToRefillValidMagicIndexes();
			}
		}

		if (_validMagicIndexes.Count > 0)
		{
			_castDelayTimer = WandSO.BaseCastDelay + cumulativeCastDelay;
		}
		else
		{
			CurrentReload = 0;
			TotalReloadDuration = WandSO.ReloadDuration + cumulativeCastDelay;
		}
	}

	private void HandleSingleSpellCast(SpellItemSO spellToCast, List<int> spellModsFound = null)
	{
		if (spellToCast.ManaCost > CurrentMana) return;

		_validMagicIndexes.Dequeue();
		_castDelayTimer = WandSO.BaseCastDelay + spellToCast.CastDelay;
		CurrentMana -= spellToCast.ManaCost;

		if (_validMagicIndexes.Count <= 0)
		{
			CurrentReload = 0;
			TotalReloadDuration = WandSO.ReloadDuration + _castDelayTimer;
		}

		StartSpellCharge(spellToCast, spellModsFound);
	}
	
	private void HandleMiningCast(DestructionCataylstItemSO miningSpell)
	{
		if (miningSpell.ManaCost > CurrentMana || !miningSpell.PlayerInRangeOfMouse()) return;

		if (Environment.Instance.WallTm.HasTile(Vector3Int.FloorToInt(ActionManager.MouseWorldPosition)))
		{
			Environment.Instance.HitWallTile(Player.LocalClientInstance.CurrentPlayerBiome.Value, Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), miningSpell.MiningPower);
			// SoundManager.Instance.PlayOneShot(FMODEvents.Instance.WandCast, Player.LocalClientInstance.transform.position);

			miningSpell.SpawnMiningVisuals();

			MiningCastCleanUp(miningSpell.CastDelay, miningSpell.ManaCost);
		}
		else if (ObjectManager.Instance.TryToFindWorldObject(Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), out WorldObject wo))
		{
			ObjectManager.Instance.HitObject(Player.LocalClientInstance.CurrentPlayerBiome.Value, wo, miningSpell.MiningPower);

			miningSpell.SpawnMiningVisuals();

			MiningCastCleanUp(miningSpell.CastDelay, miningSpell.ManaCost);
		}
	}
	
	private void MiningCastCleanUp(float castDelay, int manaCost)
	{
		_validMagicIndexes.Dequeue();
		_castDelayTimer = WandSO.BaseCastDelay + castDelay;
		CurrentMana -= manaCost;

		if (_validMagicIndexes.Count <= 0)
		{
			CurrentReload = 0;
			TotalReloadDuration = WandSO.ReloadDuration + _castDelayTimer;
		}
	}
	
	private void StartSpellCharge(SpellItemSO spellToCast, List<int> spellModsFound = null)
	{
		// Spawn the spell on the server
		_spellToCastId = IdGenerator.GenerateRandomId();
		_spellToCast = spellToCast;
		_spellToCast.LoadSpell(WandSO, spellModsFound, _spellToCastId);
		
		_castTimeTimer = new(_spellToCast.CastTime);
		_castTimeTimer.OnTimerEnd += ExecuteSpell;

		Debug.Log($"Charging Spell...");
	}

    private void ExecuteSpell(object sender, EventArgs e)
    {
		_spellToCast.ExecuteSpell(WandSO, _spellToCastId);

		_castTimeTimer.OnTimerEnd -= ExecuteSpell;
		Debug.Log($"Spell Executed");
	}
	
	private void CancelSpellCharge()
	{
		_spellToCast.CancelSpell(_spellToCastId);

		_spellToCastId = default;
		_spellToCast = null;

		_castTimeTimer.OnTimerEnd -= ExecuteSpell;
		_castTimeTimer = new Timer(0);
		Debug.Log($"Cast was interrupted. Reseting spellToCast values");
	}

    private void ResetValidMagicIndexes()
	{
		_validMagicIndexes.Clear();
	}

	private void TryToRefillValidMagicIndexes()
	{
		for (int i = 0; i < WandInvItem.MagicArray.Length; i++)
		{
			if(WandInvItem.MagicArray[i] != null)
			{
				_validMagicIndexes.Enqueue(i);
			}
		}
	}
	
	public void SetSelected(bool value)
	{
	    IsSelected = value;

		if (_castTimeTimer.RemainingSeconds > 0 && IsSelected)
		{
			CancelSpellCharge();
		}
	}
}
