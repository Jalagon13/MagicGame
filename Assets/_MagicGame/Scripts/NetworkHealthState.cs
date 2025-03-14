using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// MonoBehaviour containing only one NetworkVariable<int> which represents this object's health.
/// </summary>
public class NetworkHealthState : NetworkBehaviour
{
    [field: SerializeField] public int BaseHealth { get; private set; } = 100;
    [field: SerializeField] public int BaseDefense { get; private set; } = 0;
    [field: SerializeField] public float IFrameDuration { get; private set; } = 0.67f;

    public NetworkVariable<int> HitPoints { get; set; } = new NetworkVariable<int>();
    public NetworkVariable<int> CurrentDefense { get; set; } = new NetworkVariable<int>();
    public bool Invulnerable => _iFrameTimer.RemainingSeconds > 0;
    public bool IsDead => HitPoints.Value <= 0;

    public event EventHandler HitPointsDepleted;
    public event EventHandler HitPointsReplenished;
    public event EventHandler<HitPointsDamagedEventArgs> HitPointsDamaged;
    public class HitPointsDamagedEventArgs : EventArgs
    {
        public int DamageTaken;
        public Vector2 SourcePosition;
        public int KnockbackForce;
    }
    
    private Timer _iFrameTimer;
    private Vector2 _damagerPosition;
    private int _knockbackForce;

    private void OnEnable()
    {
        HitPoints.OnValueChanged += HitPointsChanged;
    }
    private void OnDisable() 
    {
        HitPoints.OnValueChanged -= HitPointsChanged;
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            HitPoints.Value = BaseHealth;
            _iFrameTimer = new Timer(IFrameDuration);
        }
    }

    private void Update()
    {
        if(!IsServer || !IsSpawned) return;
    
        _iFrameTimer?.Tick(Time.deltaTime);
    }

    private void HitPointsChanged(int previousValue, int newValue)
    {
        if (previousValue > 0 && newValue <= 0)
        {
            HitPointsDepleted?.Invoke(this, EventArgs.Empty);
        }
        else if (previousValue <= 0 && newValue > 0)
        {
            HitPointsReplenished?.Invoke(this, EventArgs.Empty);
        }
        else if (newValue < previousValue)
        {
            int damageTaken = previousValue - newValue;
            HitPointsDamaged?.Invoke(this, new HitPointsDamagedEventArgs
            {
                DamageTaken = damageTaken,
                SourcePosition = _damagerPosition,
                KnockbackForce = _knockbackForce
            });
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void TakeDamageRpc(int amount, Vector2 damagerPosition, int knockbackForce)
    {
        if(IsDead || Invulnerable) return;
    
        _damagerPosition = damagerPosition;
        _knockbackForce = knockbackForce;
        
        int damageReduction = CurrentDefense.Value / 2;
        int finalDamage = Mathf.Max(1, amount - damageReduction);

        if (HitPoints.Value > 0)
        {
            HitPoints.Value = Math.Max(0, HitPoints.Value - finalDamage);
        }

        _iFrameTimer.RemainingSeconds = IFrameDuration;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void HealRpc(int amount)
    {
        if (HitPoints.Value > 0)
        {
            if (HitPoints.Value + amount > BaseHealth)
            {
                amount = BaseHealth - HitPoints.Value;
            }
            HitPoints.Value += amount; // You may want to cap it at max HP
        }
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void HealToFullRpc()
    {
        HitPoints.Value = BaseHealth;
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SetCurrentDefenseRpc(int newCurentDefense)
    {
        CurrentDefense.Value = newCurentDefense;
        Debug.Log($"Player {OwnerClientId} Defense: {CurrentDefense.Value}");
    }
}