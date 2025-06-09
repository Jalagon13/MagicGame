using System;
using UnityEngine;

public class DeathPanelUI : MonoBehaviour
{
    private void Awake()
    {
        Player.OnAnyPlayerSpawned += RegisterDeathPanelLogic;
        Hide(); 
    }
    
    private void OnDestroy()
    {
        Player.OnAnyPlayerSpawned -= RegisterDeathPanelLogic;
        if (Player.LocalClientInstance != null)
        {
            Player.LocalClientInstance.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
        }
    }

    private void RegisterDeathPanelLogic(object sender, Player.PlayerIdEventArgs e)
    {
        if (Player.LocalClientInstance != null)
        {
            Player.LocalClientInstance.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
        }
    }

    private void OnPlayerLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if(previousValue == LifeState.Alive && newValue == LifeState.Dead)
        {
            Show();
        }
        else if(previousValue == LifeState.Dead && newValue == LifeState.Alive)
        {
            Hide();
        }
    }
    
    private void Show()
    {
        gameObject.SetActive(true);
    }
    
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
