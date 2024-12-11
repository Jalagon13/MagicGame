using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu()]
public class ItemDataBaseSO : ScriptableObject
{
    public List<ItemSO> ItemSOList;
	
    public int GetItemIndexFromItemObject(ItemSO item)
    {
        if(item == null)
        {
            return -1;
        }
	
        int index = ItemSOList.IndexOf(item);
        if(index > 65535 || index < 0)
        {
            Debug.LogError($"Warning, {item.name} is returning an index value out of bounds of a ushort");
        }
		
        return (ushort)index;
    }
}
