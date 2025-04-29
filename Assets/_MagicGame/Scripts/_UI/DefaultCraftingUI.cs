using UnityEngine;

public class DefaultCraftingUI : MonoBehaviour
{
    [field: SerializeField] public GameObject DefaultCraftingSlotsHolder { get; private set; }
    [field: SerializeField] public CraftNodeUI CraftingNodeUIPrefab { get; private set; }
    [field: SerializeField] public RecipeDataBaseObject DefaultRecipeDatabase { get; private set; }
    
    private void Start()
    {
        foreach (RecipeSO recipeSO in DefaultRecipeDatabase.RecipeDatabase)
        {
            CraftNodeUI craftNodeUI = Instantiate(CraftingNodeUIPrefab, DefaultCraftingSlotsHolder.transform);
            craftNodeUI.Initialize(recipeSO);
        }
    }
}
