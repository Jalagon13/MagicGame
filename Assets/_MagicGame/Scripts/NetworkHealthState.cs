using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// MonoBehaviour containing only one NetworkVariable<int> which represents this object's health.
/// </summary>
public class NetworkHealthState : NetworkBehaviour
{
    public event EventHandler OnHitPointsDepleted; // Called on server side
    public event EventHandler OnHitPointsReplenished; // Called on server side
    public event EventHandler<HitPointsDamagedEventArgs> OnHitPointsDamaged; // Called on server side
    public class HitPointsDamagedEventArgs : EventArgs
    {
        public int DamageTaken;
        public Vector2 SourcePosition;
        public int KnockbackForce;
    }

    [field: SerializeField] public int BaseHealth { get; private set; } = 100;
    [field: SerializeField] public int BaseDefense { get; private set; } = 0;
    [field: SerializeField] public float IFrameDuration { get; private set; } = 0.1666667f;
    [field: SerializeField] public bool CanDie { get; private set; } = true;
    [field: SerializeField] public bool CanTakeDamage { get; private set; } = true;

    public NetworkVariable<int> HitPoints { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> CurrentDefense { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> MaxHealth { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector2> DamagerPosition { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> KnockbackForce { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool Invulnerable => _iFrameTimer.RemainingSeconds > 0;
    public bool IsDead => HitPoints.Value <= 0;

    private Timer _iFrameTimer;

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
            KnockbackForce.Value = 0;
            DamagerPosition.Value = Vector2.zero;
            CurrentDefense.Value = BaseDefense;
            HitPoints.Value = BaseHealth;
            MaxHealth.Value = BaseHealth;
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
        if (newValue <= 0)
        {
            OnHitPointsDepleted?.Invoke(this, EventArgs.Empty);
        }
        else if (newValue < previousValue)
        {
            int damageTaken = previousValue - newValue;
            Debug.Log($"Before sending damage event. Sourceposition: {DamagerPosition.Value}, Knockbackforce: {KnockbackForce.Value}");
            OnHitPointsDamaged?.Invoke(this, new HitPointsDamagedEventArgs
            {
                DamageTaken = damageTaken,
                SourcePosition = DamagerPosition.Value,
                KnockbackForce = KnockbackForce.Value
            });
        }
        else if (previousValue <= 0 && newValue > 0)
        {
            OnHitPointsReplenished?.Invoke(this, EventArgs.Empty);
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void TakeDamageRpc(int amount, Vector2 damagerPosition, int knockbackForce)
    {
        if(IsDead || Invulnerable || !CanTakeDamage) return;
        
        DamagerPosition.Value = damagerPosition;
        KnockbackForce.Value = knockbackForce;
        Debug.Log($"Taking {amount} damage from {DamagerPosition.Value} with force {KnockbackForce.Value}");
        
        int damageReduction = CurrentDefense.Value / 2;
        int finalDamage = Mathf.Max(1, amount - damageReduction);

        if (CanDie)
        {
            if (HitPoints.Value > 0)
            {
                HitPoints.Value = Math.Max(0, HitPoints.Value - finalDamage);
            }
        }
        else 
        {
            OnHitPointsDamaged?.Invoke(this, new HitPointsDamagedEventArgs
            {
                DamageTaken = finalDamage,
                SourcePosition = DamagerPosition.Value,
                KnockbackForce = KnockbackForce.Value
            });
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