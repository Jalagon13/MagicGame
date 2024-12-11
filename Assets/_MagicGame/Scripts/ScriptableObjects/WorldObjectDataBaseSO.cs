using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu()]
public class WorldObjectDataBaseSO : ScriptableObject
{
    public List<WorldObject> WorldObjectList;

    public byte GetByteIDFromWorldAsset(WorldObject worldAsset)
    {
        foreach (WorldObject asset in WorldObjectList)
        {
            if(asset.GetWorldAssetName() == worldAsset.GetWorldAssetName())
            {
                return (byte)WorldObjectList.IndexOf(asset);
            }
        }
		
        Debug.LogError($"Cannot find {worldAsset} in WorldAssetList. Warning returning 0");
        return 0;
    }
}
