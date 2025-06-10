using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkHealthState : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<int> HitPoints = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public event EventHandler<HitPointsChangedEventArgs> OnHitPointsChanged;
    public class HitPointsChangedEventArgs : EventArgs
    {
        public int MaxHitPoints { get; }
        public int CurrentHitPoints { get; }
        
        public HitPointsChangedEventArgs(int maxHitPoints, int currentHitPoints)
        {
            MaxHitPoints = maxHitPoints;
            CurrentHitPoints = currentHitPoints;
        }
    }
    
    private ServerCharacter _serverCharacter;

    private void Awake()
    {
        _serverCharacter = GetComponent<ServerCharacter>();
    }

    private void OnEnable()
    {
        HitPoints.OnValueChanged += HitPointsChanged;
    }

    private void OnDisable()
    {
        HitPoints.OnValueChanged -= HitPointsChanged;
    }

    private void HitPointsChanged(int previousValue, int newValue)
    {
        OnHitPointsChanged?.Invoke(this, new HitPointsChangedEventArgs(_serverCharacter.Stats.MaxHealth.AsIntValue, HitPoints.Value));
    }
    
    public bool IsFullHp()
    {
        return HitPoints.Value >= _serverCharacter.Stats.MaxHealth.AsIntValue;
    }
}
