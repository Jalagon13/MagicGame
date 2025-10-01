using System;
using System.Collections;
using System.Collections.Generic;
using DigitalRuby.LightningBolt;
using FMOD.Studio;
using FMODUnity;
using Unity.Netcode;
using UnityEditor.EditorTools;
using UnityEngine;

public class ShockStream : ServerSpell
{
    [field: Header("Lingering Lightning")]
    [field: SerializeField] 
    public float Range { get; private set; }
    [field: SerializeField] 
    public float TimeBetweenDamage { get; private set; } = 0.25f;
    
    [field: Tooltip("Lifetime of particle system per distance for the beam")]
    [field: SerializeField] 
    public float LifetimePerDistanceUnit { get; private set; } = 0.05f;
    
    [SerializeField] 
    private LightningBoltScript _lightningStream;
    
    [field: SerializeField] 
    public EventReference DamageSound { get; private set; }
    [field: SerializeField] 
    public EventReference SustainedElectricitySound { get; private set; }
    
    public NetworkVariable<Vector2> BeamStart { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> BeamEnd { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> BeamOn { get; private set; } = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private List<DamageReceiver> _potentialTargetsToLockOnTo = new();
    private Timer _damageTimer;
    private EventInstance _sustainedElectricitySoundEventInstance;
    private SpellCaster _spellCaster;

    private bool _hadTargetLastFrame = false;

    protected override void OnSpellExecute()
    {
        _spellCaster = SpellCasterNetworkObject.GetComponent<SpellCaster>();
        _lightningStream.gameObject.SetActive(false);
        _damageTimer = new Timer(0.1f);
    }

    protected override IEnumerator OnSpellEnd()
    {
        // This method is called when the spell ends, you can add any cleanup logic here if needed.
        yield return null;
    }

    protected override void OnUpdateSpell()
    {
        if (IsOwner && SpellStateNV.Value == SpellState.Casting)
        {
            _damageTimer.Tick(Time.deltaTime);
            _potentialTargetsToLockOnTo.Clear();

            transform.position = _spellCaster.SpellSpawnTransform.position;
            Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, Range, CollisionMask);

            foreach (var col in collisions)
            {
                if(IsValidNpcHit(col, out DamageReceiver damageReceiver))
                {
                    if (!_potentialTargetsToLockOnTo.Contains(damageReceiver))
                    {
                        _potentialTargetsToLockOnTo.Add(damageReceiver);
                    }
                }
            }

            DamageReceiver closestTarget = null;

            if (_potentialTargetsToLockOnTo.Count > 0)
            {
                // Pick the closest target to home to
                float closestDistance = float.MaxValue;

                foreach (DamageReceiver target in _potentialTargetsToLockOnTo)
                {
                    float distance = Vector2.Distance(transform.position, target.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = target;
                    }
                }

                if (closestTarget == null) return;

                if (_damageTimer.RemainingSeconds <= 0)
                {
                    _damageTimer.RemainingSeconds = TimeBetweenDamage;

                    if (SpellCasterNetworkObject.TryGetComponent(out ServerCharacter inflicter))
                    {
                        closestTarget.ReceiveHP(inflicter, -SpellData.Value.Damage, true, SpellData.Value.Knockback);
                    }
                    SoundManager.Instance.PlayOneShot(DamageSound, transform.position);
                }

                BeamStart.Value = transform.position;
                BeamEnd.Value = closestTarget.GetComponent<Collider2D>().bounds.center;
            }
            else
            {
                BeamStart.Value = transform.position;
                BeamEnd.Value = transform.position;
            }

            bool hasTargetNow = closestTarget != null;

            if (hasTargetNow && !_hadTargetLastFrame)
            {
                _sustainedElectricitySoundEventInstance.start();
                _lightningStream.gameObject.SetActive(true);
                BeamOn.Value = true;
            }
            else if (!hasTargetNow && _hadTargetLastFrame)
            {
                _sustainedElectricitySoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _lightningStream.gameObject.SetActive(false);
                BeamOn.Value = false;
            }

            _hadTargetLastFrame = hasTargetNow;
        }
    }

    public override void OnClientSpellStart(ClientSpell clientSpell)
    {
        BeamOn.OnValueChanged += BeamOnChanged;

        // _zoneSpriteRenderer.transform.localScale = new(Range * 2, Range * 2, 1);
        _sustainedElectricitySoundEventInstance = SoundManager.Instance.CreateInstance(SustainedElectricitySound);
    }

    public override void OnClientSpellUpdate(ClientSpell clientSpell)
    {
        // Calculate direction and distance
        if (BeamOn.Value)
        {
            _lightningStream.SetPositions(BeamStart.Value, BeamEnd.Value);
        }
    }

    public override void OnClientSpellStop(ClientSpell clientSpell)
    {
        BeamOn.OnValueChanged -= BeamOnChanged;    
        
        _sustainedElectricitySoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _lightningStream.gameObject.SetActive(false);
    }

    private void BeamOnChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            _lightningStream.gameObject.SetActive(true);
        }
        else
        {
            _lightningStream.gameObject.SetActive(false);
        }
    }
}
