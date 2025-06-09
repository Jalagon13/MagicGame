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
    
    public BiomeType CurrentBiome 
    { 
        get 
        { 
            if(_characterData.IsNpc && TryGetComponent(out NpcNetworkVisibility npcVisibility))
            {
                return npcVisibility.NpcBiomeType;
            }
            else if(TryGetComponent(out Player player))
            {
                return player.CurrentBiome.Value;
            }
            
            Debug.LogError($"No Player or NpcNetworkVisibility script found");
            return BiomeType.Forest;
        } 
    }
    
    private DamageReceiver _damageReceiver;
    
    private StateMachine _stateMachine;
    public StateMachine StateMachine => _stateMachine;
    
    [SerializeField] 
    private ServerCharacterMovement _serverCharacterMovement;
    public ServerCharacterMovement Movement => _serverCharacterMovement;
    
    [SerializeField] 
    private ServerAnimationHandler _serverAnimationHandler;
    public ServerAnimationHandler AnimationHandler => _serverAnimationHandler;
    
    private CharacterStats _characterStats;
    public CharacterStats Stats => _characterStats;
    
    public NetworkVariable<MovementState> MovementState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<CardinalDirection> CardinalDirection { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<AIStateData> SuperAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<AIStateData> SubAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        _damageReceiver = GetComponent<DamageReceiver>();
        _characterStats = new CharacterStats(_characterData);
        
        NetHealthState = GetComponent<NetworkHealthState>();
        NetLifeState = GetComponent<NetworkLifeState>();

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
            _damageReceiver.HpReceived += ReceiveHP;
            
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
            _damageReceiver.HpReceived -= ReceiveHP;
        }
    }
    
    public override void OnDestroy()
    {
        _stateMachine?.Dispose();
    }
    
    private void FixedUpdate()
    {
        if ((IsOwner || (_characterData.IsNpc && IsServer)) && _characterData.CanMove)
        {
            _serverCharacterMovement.FixedUpdateMovement();
        }
    }
    
    private void Update()
    {
        if (IsOwner || (_characterData.IsNpc && IsServer))
        {
            if (_stateMachine != null)
            {
                _characterStats.TickBuffs(Time.deltaTime);
                _stateMachine.UpdateAI();
            }
        }
    }

    private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if(LifeState == LifeState.Dead)
        {
            if (_characterData.IsNpc)
            {
                // Npc Death functionality here
                if(TryGetComponent(out NpcNetworkVisibility npcVisibility))
                {
                    npcVisibility.KillNpcServerRpc();
                }
                else
                {
                    Debug.LogError($"ServerCharacter {gameObject.name} missing NpcVisibility.");
                }
            }
        }
        else if(LifeState == LifeState.IFrame)
        {
            // TODO: IFrame functionality for all servercharacters here...
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
            
            _clientCharacter.PlayGameFeelRpc(hp);
            
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
        else if(HitPoints <= 0 && _characterData.CanDie)
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
