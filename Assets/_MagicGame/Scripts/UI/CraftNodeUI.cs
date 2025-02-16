using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftNodeUI : MonoBehaviour
{
    [SerializeField] private Image _outputImage;
    [SerializeField] private TextMeshProUGUI _outputAmountText;
    [SerializeField] private Button _craftButton;
	
    private RecipeSO _recipeSO;
    private bool _canCraft, _hovered;
	
    private void OnDisable()
    {
        if(_hovered)
        {
            TooltipManager.Instance.Hide();
        }
    }
	
    public void Initialize(RecipeSO recipe)
    {
        _recipeSO = recipe;
        _outputImage.sprite = _recipeSO.OutputItem.UiDisplay;
        _outputAmountText.text = _recipeSO.OutputAmount == 1 ? string.Empty : _recipeSO.OutputAmount.ToString();
        UpdateCraftStatus();
    }
	
    // Attached to the InvItem button component
    public void OnCraft()
    {
        InventoryManager.Instance.TryToCraft(_recipeSO);
    }
	
    // Connected to Event Trigger component on _outputImage
    public void OnPointerEnter()
    {
        StringBuilder description = new();
        description.Append($"{_recipeSO.OutputItem.GetDescription()}");
        description.Append("<br>Recipe:<br>");
		
        //for each ingredient in the recipe resource list
        foreach (var ingredient in _recipeSO.ResourceList)
        {
            description.Append(GetIndredientText(ingredient));
        }
		
        TooltipManager.Instance.Show(description.ToString(), $"{_recipeSO.OutputItem.Name}");
        _hovered = true;
    }
	
    // Connected to Event Trigger component on _outputImage
    public void OnPointerExit()
    {
        TooltipManager.Instance.Hide();
        _hovered = false;
    }
	
    private void UpdateCraftStatus()
    {
        if(_recipeSO == null) return;

        _canCraft = InventoryManager.Instance.HasAllIngredients(_recipeSO.ResourceList);
		
        // Edit CraftNodeView visuals depending on _canCraft
        _craftButton.interactable = _canCraft;
        // ...
    }
	
    private string GetIndredientText(InventoryItem ingredient)
    {
        int currentIngredientCounter = InventoryManager.Instance.GetInventoryModel().GetAmount(ingredient.Item);
        string spriteImage = $"<voffset=-2><size=25><sprite name=\"{ingredient.Item.UiDisplay.name}\"></size></voffset>";
		
        return currentIngredientCounter >= ingredient.Quantity ? 
            $"{spriteImage}<voffset=4>{ingredient.Item.Name} ({ingredient.Quantity})</voffset><br>" : 
            $"{spriteImage}<voffset=4><color=red>{ingredient.Item.Name} ({ingredient.Quantity})</color></voffset><br>";
    }
}
