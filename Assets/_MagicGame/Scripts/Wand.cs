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
	
	private Queue<int> _validMagicIndexes = new();
	private float _castTimer;

	public Wand(WandInventoryItem wandInventoryItem)
	{
		WandInvItem = wandInventoryItem;
		WandSO = WandInvItem.Item as WandItemSO;
		CurrentMana = WandSO.MaxMana;
		WandInvItem.OnWandContentsUpdated += OnWandContentsUpdated;
		
		ResetValidMagicIndexes();
	}

	private void OnWandContentsUpdated(object sender, EventArgs e)
	{
		Debug.Log($"Resseting valid magic idnexes");
	
		ResetValidMagicIndexes();
	}

	public void Tick(float deltaTime)
	{
		if(_castTimer > 0)
		{
			_castTimer -= deltaTime;
		}
		
		CurrentReload += deltaTime; // Regen recharge over time
		CurrentReload = Mathf.Min(CurrentReload, TotalReloadDuration); // Clamp to prevent overfilling
		
		CurrentMana += WandSO.ManaRegenSpeed * deltaTime; // Regenerate mana over time
		CurrentMana = Mathf.Min(CurrentMana, WandSO.MaxMana); // Clamp to prevent overfilling
	}
	
	public void CastSpell()
	{
		if(_castTimer > 0 || CurrentReload < TotalReloadDuration) return; // Cast Delay or recharge ongoing return

		if(_validMagicIndexes.Count == 0) // If validspells is empty, try to fill it up
		{
			TryToRefillValidMagicIndexes();
		}
		
		if(_validMagicIndexes.Count == 0) return; // If still empty after fill, return

		MagicItemSO magic = WandInvItem.MagicArray[_validMagicIndexes.Peek()];
		
		if(magic is MultiCastItemSO multiCast)
		{
			HandleMultiCast(multiCast);
		}
		else if(magic is SpellItemSO spellToCast)
		{
			HandleSingleSpellCast(spellToCast);
		}
		else if(magic is DestructionCataylstItemSO miningSpell)
		{
			HandleMiningCast(miningSpell);
		}
	}
	
	private void HandleMultiCast(MultiCastItemSO multiCast)
	{
		List<int> spellsShot = new();

		Debug.Log($"Found Multicast");
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

					potentialSpellToShoot.CastSpell(WandSO);
					numOfSpellsCast++;
					cumulativeCastDelay += potentialSpellToShoot.CastDelay;
					spellsShot.Add(validMagicIndex);
				}
			}

			// If the queue is empty but we still need more spells, try refilling it
			if (_validMagicIndexes.Count == 0 && numOfSpellsCast < multiCast.MultiCastAmount)
			{
				Debug.Log("Queue is empty but I still need more spells, refilling...");
				TryToRefillValidMagicIndexes();
			}
		}

		if (_validMagicIndexes.Count > 0)
		{
			_castTimer = WandSO.BaseCastDelay + cumulativeCastDelay;
		}
		else
		{
			CurrentReload = 0;
			TotalReloadDuration = WandSO.ReloadDuration + cumulativeCastDelay;
		}
	}

	private void HandleSingleSpellCast(SpellItemSO spellToCast)
	{
		if (spellToCast.ManaCost > CurrentMana) return;

		_validMagicIndexes.Dequeue();
		_castTimer = WandSO.BaseCastDelay + spellToCast.CastDelay;
		CurrentMana -= spellToCast.ManaCost;
		spellToCast.CastSpell(WandSO);

		if (_validMagicIndexes.Count <= 0)
		{
			CurrentReload = 0;
			TotalReloadDuration = WandSO.ReloadDuration + _castTimer;
		}
	}
	
	private void HandleMiningCast(DestructionCataylstItemSO miningSpell)
	{
		if (miningSpell.ManaCost > CurrentMana || !miningSpell.PlayerInRangeOfMouse()) return;

		if (Environment.Instance.WallTm.HasTile(Vector3Int.FloorToInt(ActionManager.MouseWorldPosition)))
		{
			Environment.Instance.HitWallTile(Player.LocalClientInstance.CurrentBiome.Value, Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), miningSpell.MiningPower);
			// SoundManager.Instance.PlayOneShot(FMODEvents.Instance.WandCast, Player.LocalClientInstance.transform.position);

			miningSpell.SpawnMiningVisuals();

			MiningCastCleanUp(miningSpell.CastDelay, miningSpell.ManaCost);
		}
		else if (ObjectManager.Instance.TryToFindWorldObject(Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), out WorldObject wo))
		{
			ObjectManager.Instance.HitObject(Player.LocalClientInstance.CurrentBiome.Value, wo, miningSpell.MiningPower);

			miningSpell.SpawnMiningVisuals();

			MiningCastCleanUp(miningSpell.CastDelay, miningSpell.ManaCost);
		}
	}
	
	private void MiningCastCleanUp(float castDelay, int manaCost)
	{
		_validMagicIndexes.Dequeue();
		_castTimer = WandSO.BaseCastDelay + castDelay;
		CurrentMana -= manaCost;

		if (_validMagicIndexes.Count <= 0)
		{
			CurrentReload = 0;
			TotalReloadDuration = WandSO.ReloadDuration + _castTimer;
		}
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
}
