using System.Collections.Generic;
using UnityEngine;

public class Wand
{
	public WandInventoryItem WandInvItem { get; private set; }
	public float CurrentMana { get; private set; }
	public float CurrentRecharge { get; private set; }
	public float TotalRechargeDuration { get; private set; }
	public WandItemSO WandSO { get; private set; }
	
	private Queue<int> _validSpellIndexes = new();
	private float _castTimer;
	private int _spellIndex;

	public Wand(WandInventoryItem wandInventoryItem)
	{
		WandInvItem = wandInventoryItem;
		WandSO = WandInvItem.Item as WandItemSO;
		CurrentMana = WandSO.MaxMana;
		_spellIndex = -1;
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

		if(_validSpellIndexes.Count == 0) // If validspells is empty, try to fill it up
		{
			for (int i = 0; i < WandInvItem.SpellArray.Length; i++)
			{
				if(WandInvItem.SpellArray[i] != null)
				{
					_validSpellIndexes.Enqueue(i);
				}
			}
		}
		
		if(_validSpellIndexes.Count == 0) return; // If still empty after fill, return

		if(WandInvItem.SpellArray[_validSpellIndexes.Peek()].ManaCost > CurrentMana)
		{
			CurrentRecharge = 0;
			return;
		}
		
		_spellIndex = _validSpellIndexes.Dequeue(); 
		
		var spell = WandInvItem.SpellArray[_spellIndex];
		
		spell.CastSpell(WandSO); // Grab the next spell in line and cast it
		
		CurrentMana -= spell.ManaCost;
		CurrentMana = Mathf.Max(0, CurrentMana);
		
		if(_validSpellIndexes.Count > 0)
		{
			// valid spells exists after cast
			_castTimer = WandSO.BaseCastDelay + spell.CastDelay;
			Debug.Log($"doing cast timer for {WandSO.BaseCastDelay + spell.CastDelay}");
		}
		else
		{
			// Casted the last spell in the sequence
			CurrentRecharge = 0;
			TotalRechargeDuration = WandSO.MaxRechargeDuration + spell.CastDelay;
			Debug.Log($"Total recharge {TotalRechargeDuration}");
		}
	}
}
