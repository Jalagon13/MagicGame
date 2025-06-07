using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

using UnityEngine;

public enum CharacterStateMachine
{
    BasicNpc,
    Player,
}

[RequireComponent(typeof(NetworkHealthState), typeof(NetworkLifeState))]
public class ServerCharacter : NetworkBehaviour
{
    [SerializeField]
    private CharacterStateMachine _aiType;
    public CharacterStateMachine AIType => _aiType;
    
    [SerializeField]
    private CharacterDataSO _characterData;
    public CharacterDataSO Data => _characterData;
    
    [SerializeField] 
    private ClientCharacter _clientCharacter;
    public ClientCharacter ClientCharacter => _clientCharacter;
    
    public NetworkHealthState NetHealthState { get; private set; }
    public int HitPoints
    {
        get => NetHealthState.HitPoints.Value;
        private set => NetHealthState.HitPoints.Value = value;
    }
    
    public NetworkLifeState NetLifeState { get; private set; }
    public LifeState LifeState
    {
        get => NetLifeState.LifeState.Value;
        private set => NetLifeState.LifeState.Value = value;
    }
    
    public NpcNetworkVisibility NpcVisibility { get; private set; }
    
    [SerializeField] 
    private DamageReceiver _damageReceiver;
    
    private StateMachine _stateMachine;
    public StateMachine StateMachine => _stateMachine;
    
    [SerializeField] 
    private ServerCharacterMovement _serverCharacterMovement;
    public ServerCharacterMovement Movement => _serverCharacterMovement;
    
    
    [SerializeField] 
    private ServerAnimationHandler _serverAnimationHandler;
    public ServerAnimationHandler AnimationHandler => _serverAnimationHandler;
    
    public NetworkVariable<MovementState> MovementState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<CardinalDirection> CardinalDirection { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<AIStateData> SuperAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<AIStateData> SubAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        NetHealthState = GetComponent<NetworkHealthState>();
        NetLifeState = GetComponent<NetworkLifeState>();
        
        if(_characterData.IsNpc)
        {
            NpcVisibility = GetComponent<NpcNetworkVisibility>();
            if(NpcVisibility == null) Debug.LogWarning($"ServerCharacter {gameObject.name} missing NpcVisibility.");
        }

        switch (_aiType)
        {
            case CharacterStateMachine.BasicNpc:
                _stateMachine = new BasicNpcStateMachine(this);
                break;
            case CharacterStateMachine.Player:
                _stateMachine = new PlayerStateMachine(this);
                break;
        }

        if (_stateMachine == null)
            Debug.LogWarning($"ServerCharacter {gameObject.name} missing _aiBrain.");
    }

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            NetLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
            _damageReceiver.DamagedReceived += ReceiveHP;
            
            HitPoints = _characterData.BaseHP;
            _stateMachine?.OwnerInitialization();
        }
    }

    protected override void OnNetworkPostSpawn()
    {
        if(IsOwner)
        {
            _stateMachine?.StartStateMachine();
        }
    }

    public override void OnNetworkDespawn()
    {
        if(IsOwner)
        {
            NetLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
            _damageReceiver.DamagedReceived -= ReceiveHP;
        }
    }
    
    public override void OnDestroy()
    {
        _stateMachine?.Dispose();
    }
    
    private void FixedUpdate()
    {
        if (IsOwner || (_characterData.IsNpc && IsServer))
        {
            _serverCharacterMovement.FixedUpdateMovement();
        }
    }
    
    private void Update()
    {
        if (IsOwner || (_characterData.IsNpc && IsServer))
        {
            if (_stateMachine != null && LifeState == LifeState.Alive)
            {
                _stateMachine.UpdateAI();
            }
        }
    }

    private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if(LifeState == LifeState.Dead)
        {
            // TODO: Death and IFrame functionality here...
            if (_characterData.IsNpc)
            {
                // Npc Death functionality here
                NpcVisibility.KillNpcServerRpc();
            }
            else
            {
                // Player Death functionality here
            }
        }
    }

    private void ReceiveHP(object sender, DamageReceiver.DamageReceivedEventArgs e)
    {
        ServerCharacter inflicter = e.Inflicter;
        int hp = e.HP;
        
        if(hp > 0)
        {
            // HP healing mod functionality here
            float healingMod = 1f;
            hp = (int)(hp * healingMod);
        }
        else
        {
            if(LifeState == LifeState.IFrame)
                return;
                
            // Damage reduction mod functionality here
            float damageReduction = 1f;
            hp = (int)(hp * damageReduction);
            
            _clientCharacter.PlayDamageNumbersRpc(hp);
            
            if (_characterData.CanBeKnockedBack && e.PlayKnockback)
            {
                _serverCharacterMovement.StartKnockback(inflicter.transform.position, e.KnockbackForce);
            }
        }
        
        HitPoints = Mathf.Clamp(HitPoints + hp, 0, _characterData.BaseHP);
        
        _stateMachine?.ReceiveHP(inflicter, hp);
        
        if(HitPoints > 0)
        {
            StartCoroutine(StartIFrameTimer());
        }
        else if(HitPoints <= 0)
        {
            LifeState = LifeState.Dead;
        }
    }

    private IEnumerator StartIFrameTimer()
    {
        LifeState = LifeState.IFrame;
        yield return new WaitForSeconds(_characterData.IFrameDuration);
        LifeState = LifeState.Alive;
    }
}
