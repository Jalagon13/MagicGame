using System;
using System.Collections.Generic;
using UnityEngine;

public class GameDataRegistry : MonoBehaviour
{
    public static GameDataRegistry Instance { get; private set; }
    
    [SerializeField] 
    private List<CharacterDataSO> _characterData;

    private void Awake()
    {
        Instance = this;
    }
    
    public short GetShortIdFromCharacterData(CharacterDataSO characterData)
    {
        for (int i = 0; i < _characterData.Count; i++)
        {
            if (_characterData[i].StringID == characterData.StringID)
            {
                return (short)i;
            }
        }
        
        Debug.LogError($"CharacterDataSO '{characterData}' not found!");
        return -1;
    }

    public CharacterDataSO GetCharacterDataFromShortId(short npcId)
    {
        if (npcId < 0 || npcId >= _characterData.Count)
        {
            Debug.LogError($"Invalid NPC ID: {npcId}");
            return null;
        }

        return _characterData[npcId];
    }
}
