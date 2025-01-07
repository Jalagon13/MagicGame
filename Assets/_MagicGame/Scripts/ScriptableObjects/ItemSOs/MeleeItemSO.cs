using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "New Melee Item", menuName = "Create Item/New Melee Item")]
public class MeleeItemSO : ItemSO
{
	public override void ExecutePrimaryAction() { }

	public override void ExecuteSecondaryAction() { }

	public override string GetDescription()
	{
		StringBuilder description = new();
		
		foreach (var item in DefaultParameterList)
		{
			switch (item.Parameter.ParameterName)
			{
				case "Damage":
					float damage = item.Value;
					description.Append($"<color=yellow>{damage} Damage</color=yellow><br>");
					break;
				case "SwingSpeed":
					float swingSpeed = item.Value;
					description.Append($"<color=yellow>{swingSpeed} Swing Speed</color=yellow><br>");
					break;
			}
		}
		
		description.Append(GetDescriptionBreak());
	
		return description.ToString();
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}
