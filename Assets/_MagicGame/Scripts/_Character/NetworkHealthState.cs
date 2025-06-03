using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkHealthState : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<int> HitPoints = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public event EventHandler HitPointsDepleted;
    public event EventHandler HitPointsReplenished;

    void OnEnable()
    {
        HitPoints.OnValueChanged += HitPointsChanged;
    }

    void OnDisable()
    {
        HitPoints.OnValueChanged -= HitPointsChanged;
    }

    void HitPointsChanged(int previousValue, int newValue)
    {
        if (previousValue > 0 && newValue <= 0)
        {
            // Newly reached 0 HP
            HitPointsDepleted?.Invoke(this, EventArgs.Empty);
        }
        else if (previousValue <= 0 && newValue > 0)
        {
            // Newly revived
            HitPointsReplenished?.Invoke(this, EventArgs.Empty);
        }
    }
}
