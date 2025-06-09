using System.Collections.Generic;
using UnityEngine;

public class GameDB : MonoBehaviour
{
    public static GameDB Instance { get; private set; }
    
    [SerializeField] 
    private List<CharacterDataSO> _characterData;

    private void Awake()
    {
        Instance = this;
    }
}
