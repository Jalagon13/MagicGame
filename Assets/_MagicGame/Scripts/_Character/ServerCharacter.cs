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
    private Timer _hpRegenTimer;

    [SerializeField]
    private CharacterStateMachine _aiType;
    public CharacterStateMachine AIType => _aiType;
    
    [SerializeField]
    private CharacterDataSO _characterData;
    public CharacterDataSO Data => _characterData;
    
    [SerializeField] 
    private ClientCharacter _clientCharacter;
    public ClientCharacter ClientCharacter => _clientCharacter;
    
    [SerializeField] 
    private ClientFeedbacks _clientFeedbacks;
    
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
            Debug.LogWarning($"ServerCharacter {gameObject.name} has not been assigned an AI state machine");
    }

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            NetLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
            _damageReceiver.HpReceived += ReceiveHP;
            
            HitPoints = _characterData.BaseHealth;
            _stateMachine?.OwnerInitialization();

            if(Data.CanRegenerateHealth)
            {
                _hpRegenTimer = new Timer(_characterData.BaseHealthRegenTimeInterval <= 0 ? 1f : _characterData.BaseHealthRegenTimeInterval);
                _hpRegenTimer.OnTimerEnd += OnHealthRegenTimerEnd;
            }
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
            if (_hpRegenTimer != null)
            {
                _hpRegenTimer.OnTimerEnd -= OnHealthRegenTimerEnd;
                _hpRegenTimer = null;
            }
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
            if (_stateMachine != null)
            {
                _characterStats.TickBuffs(Time.deltaTime);
                _stateMachine.UpdateAI();
            }
            
            if (_characterData.CanRegenerateHealth && _hpRegenTimer != null && LifeState != LifeState.Dead && !NetHealthState.IsFullHp())
            {
                _hpRegenTimer.Tick(Time.deltaTime);
            }
        }
    }

    // Probably delete this maybe later idk may be useful another time in this class
    private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if(previousValue == LifeState.Alive && newValue == LifeState.Dead)
        {
            // I already have a death state im not if im going to use this
        }
        else if(newValue == LifeState.IFrame)
        {
            // TODO: IFrame functionality for all servercharacters here... not sure what to do with this probably delete it
        }
    }

    private void ReceiveHP(object sender, DamageReceiver.DamageReceivedEventArgs e)
    {
        ServerCharacter inflicter = e.Inflicter;
        int hpReceived = e.HP;
        
        if(hpReceived > 0)
        {
            // HP healing mod functionality here
            float healingMod = 1f;
            hpReceived = (int)(hpReceived * healingMod);
        }
        else
        {
            if(LifeState == LifeState.IFrame)
                return;
            
            // Damage reduction mod functionality here
            float damageReduction = 1f;
            hpReceived = (int)(hpReceived * damageReduction);

            // If not dead after taking damage, play character damaged feedbacks
            if (HitPoints + hpReceived > 0 || !_characterData.CanDie)
                _clientFeedbacks.PlayDamageFeedbacksRpc(hpReceived);

            if (_characterData.CanBeKnockedBack && e.PlayKnockback)
                _serverCharacterMovement.StartKnockback(inflicter.transform.position, e.KnockbackForce);

            if (HitPoints + hpReceived > 0)
                StartCoroutine(StartIFrameTimer());
        }
        
        HitPoints = Mathf.Clamp(HitPoints + hpReceived, 0, _characterData.BaseHealth);
        _stateMachine?.ReceiveHP(inflicter, hpReceived);

        if (HitPoints <= 0 && _characterData.CanDie)
        {
            _clientFeedbacks.PlayDeathFeedbacksRpc(hpReceived);
            LifeState = LifeState.Dead;
        }
    }

    private IEnumerator StartIFrameTimer()
    {
        LifeState = LifeState.IFrame;
        yield return new WaitForSeconds(_characterData.IFrameDuration);
        LifeState = LifeState.Alive;
    }

    private void OnHealthRegenTimerEnd(object sender, EventArgs e)
    {
        if (LifeState != LifeState.Dead)
        {
            int healAmount = _characterData.BaseHealthRegenAmount <= 0 ? 1 : _characterData.BaseHealthRegenAmount;
            _damageReceiver.ReceiveHP(this, healAmount, false);
        }
        
        _hpRegenTimer.Reset();
    }
}
