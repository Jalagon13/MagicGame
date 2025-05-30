using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

using UnityEngine;

public enum AIType
{
    ChaseAI
}

[RequireComponent(typeof(NetworkHealthState), typeof(NetworkLifeState), typeof(NpcNetworkVisibility))]
public class ServerCharacter : NetworkBehaviour
{
    [SerializeField]
    private AIType _aiType;
    public AIType AIType => _aiType;
    
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
    private IAIBrain _aiBrain;
    
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
        if (!NetHealthState || !NetLifeState || !NpcVisibility)
        {
            Debug.LogError("ServerCharacter missing required components.");
        }
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
                if(_aiType == AIType.ChaseAI)
                {
                    _aiBrain = new ChaseAIStateMachine(this, _serverActionPlayer);
                }
                
                if (_aiBrain == null)
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
    
    private void FixedUpdate()
    {
        if (!IsServer) return;
        
        _serverCharacterMovement.FixedUpdateMovement();
    }
    
    private void Update()
    {
        if (!IsServer) return;

        _serverActionPlayer.OnUpdateServerActions();
        if (_characterData.IsNpc && LifeState == LifeState.Alive && _aiBrain != null)
        {
            _aiBrain.UpdateAI();
        }
    }

    private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if(LifeState != LifeState.Alive)
        {
            // TODO: Death and IFrame functionality here...
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
        }
        
        HitPoints = Mathf.Clamp(HitPoints + hp, 0, _characterData.BaseHP);
        
        if(_characterData.CanBeKnockedBack && e.PlayKnockback)
        {
            _serverCharacterMovement.StartKnockback(inflicter.transform.position, e.KnockbackForce);
        }
        
        _aiBrain?.ReceiveHP(inflicter, hp);
        
        if(HitPoints > 0)
        {
            StartCoroutine(StartIFrameTimer());
        }
        else if(HitPoints <= 0)
        {
            if(_characterData.IsNpc)
            {
                // Npc Death functionality here
            }
            else
            {
                // Player Death functionality here
            }
        
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
