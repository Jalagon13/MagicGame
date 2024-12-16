using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe Database", menuName = "Crafting/New Recipe Database")]
public class RecipeDataBaseObject : ScriptableObject
{
    public List<RecipeSO> RecipeDatabase = new List<RecipeSO>();
}
