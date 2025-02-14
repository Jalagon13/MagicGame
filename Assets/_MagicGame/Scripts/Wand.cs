using System;
using System.Collections.Generic;
using UnityEngine;

public class Wand
{
	public WandInventoryItem WandInvItem { get; private set; }
	public float CurrentMana { get; private set; }
	public float CurrentRecharge { get; private set; }
	public float TotalRechargeDuration { get; private set; }
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
		
		CurrentRecharge += deltaTime; // Regen recharge over time
		CurrentRecharge = Mathf.Min(CurrentRecharge, TotalRechargeDuration); // Clamp to prevent overfilling
		
		CurrentMana += WandSO.ManaChargeSpeed * deltaTime; // Regenerate mana over time
		CurrentMana = Mathf.Min(CurrentMana, WandSO.MaxMana); // Clamp to prevent overfilling
	}
	
	public void CastSpell()
	{
		if(_castTimer > 0 || CurrentRecharge < TotalRechargeDuration) return; // Cast Delay or recharge ongoing return

		if(_validMagicIndexes.Count == 0) // If validspells is empty, try to fill it up
		{
			TryToRefillValidMagicIndexes();
		}
		
		if(_validMagicIndexes.Count == 0) return; // If still empty after fill, return

		MagicItemSO magic = WandInvItem.MagicArray[_validMagicIndexes.Peek()];
		
		if(magic is MultiCastItemSO multiCast)
		{
			List<int> spellsShot = new();
		
			Debug.Log($"Found Multicast");
			_validMagicIndexes.Dequeue();
			
			if(_validMagicIndexes.Count == 0)
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
					if(spellsShot.Contains(validMagicIndex)) continue; // If spell has been shot already, skip it
				
					Debug.Log($"Found spell for multicast: {potentialSpellToShoot.Name}");

					if (potentialSpellToShoot.ManaCost <= CurrentMana)
					{
						CurrentMana -= potentialSpellToShoot.ManaCost;
					
						Debug.Log($"Casting {potentialSpellToShoot.Name}");
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
			
			if(_validMagicIndexes.Count > 0)
			{
				_castTimer = WandSO.BaseCastDelay + cumulativeCastDelay;
			}
			else
			{
				CurrentRecharge = 0;
				TotalRechargeDuration = WandSO.MaxRechargeDuration + cumulativeCastDelay;
			}
		}
		else if(magic is SpellItemSO spellToCast)
		{
			_validMagicIndexes.Dequeue(); 
			_castTimer = WandSO.BaseCastDelay + spellToCast.CastDelay;
			CurrentMana -= spellToCast.ManaCost;
			spellToCast.CastSpell(WandSO);
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
