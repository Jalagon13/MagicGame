using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe Database", menuName = "Crafting/New Recipe Database")]
public class RecipeDataBaseObject : ScriptableObject
{
    [field: SerializeField] public string DatabaseName { get; private set; }
    public List<RecipeSO> RecipeDatabase = new List<RecipeSO>();
}
