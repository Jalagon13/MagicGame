using System;
using System.Collections.Generic;
using UnityEngine;

public class GameDataRegistry : MonoBehaviour
{
    public static GameDataRegistry Instance { get; private set; }
    
    [SerializeField] 
    private List<CharacterDataSO> _characterData;
    [Space(15)]
    [SerializeField] 
    private List<ResourceDataSO> _resourceData;

    private void Awake()
    {
        Instance = this;
    }

    public ushort GetUShortIdFromResourceData(ResourceDataSO resourceData)
    {
        if(resourceData == null)
        {
            Debug.LogError($"ResourceDataSO is null. Use this log to deduce where this came from");
        }
    
        for (int i = 0; i < _resourceData.Count; i++)
        {
            if (_resourceData[i].StringID == resourceData.StringID)
            {
                return (ushort)i;
            }
        }

        Debug.LogError($"ResourceDataSO '{resourceData}' not found!");
        return ushort.MaxValue;
    }

    public ResourceDataSO GetResourceDataFromUShortId(ushort resourceId)
    {
        if (resourceId >= _resourceData.Count)
        {
            Debug.LogError($"Invalid Resource ID: {resourceId}");
            return null;
        }

        return _resourceData[resourceId];
    }

    public ushort GetUShortIdFromCharacterData(CharacterDataSO characterData)
    {
        if (characterData == null)
        {
            Debug.LogError($"CharacterDataSO is null. Use this log to deduce where this came from");
        }

        for (int i = 0; i < _characterData.Count; i++)
        {
            if (_characterData[i].StringID == characterData.StringID)
            {
                return (ushort)i;
            }
        }
        
        Debug.LogError($"CharacterDataSO '{characterData}' not found!");
        return ushort.MaxValue;
    }

    public CharacterDataSO GetCharacterDataFromUShortId(ushort npcId)
    {
        if (npcId >= _characterData.Count)
        {
            Debug.LogError($"Invalid NPC ID: {npcId}");
            return null;
        }

        return _characterData[npcId];
    }
}
