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

[RequireComponent(typeof(NetworkHealthState), typeof(NetworkLifeState), typeof(NpcNetworkVisibility))]
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
    private IAIBrain _stateMachine;
    
    [SerializeField] 
    private ServerCharacterMovement _serverCharacterMovement;
    public ServerCharacterMovement Movement => _serverCharacterMovement;
    
    private ServerActionPlayer _serverActionPlayer;
    public ServerActionPlayer ServerActionPlayer => _serverActionPlayer;
    
    [SerializeField] 
    private ServerAnimationHandler _serverAnimationHandler;
    public ServerAnimationHandler AnimationHandler => _serverAnimationHandler;
    
    public NetworkVariable<MovementState> MovementState = new NetworkVariable<MovementState>();

    private void Awake()
    {
        _serverActionPlayer = new ServerActionPlayer(this);

        NetHealthState = GetComponent<NetworkHealthState>();
        NetLifeState = GetComponent<NetworkLifeState>();
        NpcVisibility = GetComponent<NpcNetworkVisibility>();
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            NetLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
            _damageReceiver.DamagedReceived += ReceiveHP;
            
            HitPoints = _characterData.BaseHP;

            if (_characterData.IsNpc)
            {
                switch (_aiType)
                {
                    case CharacterStateMachine.BasicNpc:
                        _stateMachine = new BasicNpcStateMachine(this, _serverActionPlayer);
                        break;
                    case CharacterStateMachine.Player:
                        _stateMachine = new PlayerStateMachine(this, _serverActionPlayer);
                        break;
                }
            
                if(_aiType == CharacterStateMachine.BasicNpc)
                {
                    _stateMachine = new BasicNpcStateMachine(this, _serverActionPlayer);
                }
                
                if (_stateMachine == null)
                    Debug.LogWarning($"ServerCharacter {gameObject.name} missing _aiBrain.");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer)
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
        if (IsServer || (!_characterData.IsNpc && IsOwner))
        {
            _serverCharacterMovement.FixedUpdateMovement();
        }
    }
    
    private void Update()
    {
        if (IsServer || (!_characterData.IsNpc && IsOwner))
        {
            _serverActionPlayer.OnUpdateServerActions();
            if (_characterData.IsNpc && LifeState == LifeState.Alive && _stateMachine != null)
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
        }
        
        HitPoints = Mathf.Clamp(HitPoints + hp, 0, _characterData.BaseHP);
        
        if(_characterData.CanBeKnockedBack && e.PlayKnockback)
        {
            _serverCharacterMovement.StartKnockback(inflicter.transform.position, e.KnockbackForce);
        }
        
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
