using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using Unity.Netcode;
using UnityEditor.EditorTools;
using UnityEngine;

public class ShockStream : ServerSpell
{
    protected override void OnSpellExecute()
    {

    }

    protected override IEnumerator OnSpellEnd()
    {
        // This method is called when the spell ends, you can add any cleanup logic here if needed.
        yield return null;
    }

    // private bool _hadTargetLastFrame = false;

    // [field: SerializeField] public float Range { get; private set; }
    // [field: SerializeField] public float TimeBetweenDamage { get; private set; } = 0.25f;
    // [field: Tooltip("Lifetime of particle system per distance for the beam")]
    // [field: SerializeField] public float LifetimePerDistanceUnit { get; private set; } = 0.05f;
    // [field: SerializeField] public ParticleSystem LightningStream { get; private set; }
    // [field: SerializeField] public EventReference DamageSound { get; private set; }
    // [field: SerializeField] public EventReference SustainedElectricitySound { get; private set; }

    // public NetworkVariable<Vector2> BeamStart { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // public NetworkVariable<Vector2> BeamEnd { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // public NetworkVariable<bool> BeamOn { get; private set; } = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // private List<NetworkHealthState> _potentialTargetsToLockOnTo = new();
    // private Timer _damageTimer;
    // private EventInstance _sustainedElectricitySoundEventInstance;

    // protected override void OnSpellSpawned()
    // {
    //     // Optional spawn-time logic
    // }

    // protected override void OnExecuteSpellStart()
    // {
    //     _damageTimer = new Timer(0.1f);
    //     BeamOn.OnValueChanged += BeamOnChanged;

    //     _sustainedElectricitySoundEventInstance = SoundManager.Instance.CreateInstance(SustainedElectricitySound);
    // }

    // protected override void OnSpellEnd()
    // {
    //     _sustainedElectricitySoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    //     LightningStream.Stop();
    // }

    // protected override void OnSpellCanceled()
    // {
    //     // Optional cancel logic
    // }
    
    // protected override void Update()
    // {
    //     base.Update();

    //     if (IsOwner && IsStarted.Value)
    //     {
    //         // if(Player.LocalClientInstance.PlayerStats.CurrentMana < SpellData.Value.ManaCost)
    //         // {
    //         //     OnSpellEnd();
    //         //     return;
    //         // }
        
    //         Vector2 wandPos = Player.LocalClientInstance.PlayerHand.SpellSpawnTransform.position;
    //         transform.position = wandPos;
            
    //         _damageTimer.Tick(Time.deltaTime);
    //         _potentialTargetsToLockOnTo.Clear();

    //         Collider2D[] collisions = Physics2D.OverlapCircleAll(wandPos, Range, CollisionMask);

    //         for (int i = 0; i < collisions.Length; i++)
    //         {
    //             int layerTest = 1 << collisions[i].gameObject.layer;
    //             if ((layerTest & CollisionMask) != 0)
    //             {
    //                 if (collisions[i].gameObject.layer == NpcLayer)
    //                 {
    //                     if (collisions[i].TryGetComponent(out NpcNetworkVisibility npcNet) && npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
    //                     {
    //                         NetworkHealthState npc = npcNet.GetComponent<NetworkHealthState>();
    //                         if (!_potentialTargetsToLockOnTo.Contains(npc))
    //                         {
    //                             _potentialTargetsToLockOnTo.Add(npc);
    //                         }
    //                     }
    //                 }
    //             }
    //         }

    //         NetworkHealthState closestTarget = null;

    //         if (_potentialTargetsToLockOnTo.Count > 0)
    //         {
    //             // Pick the closest target to home to
    //             float closestDistance = float.MaxValue;

    //             foreach (NetworkHealthState target in _potentialTargetsToLockOnTo)
    //             {
    //                 float distance = Vector2.Distance(wandPos, target.transform.position);
    //                 if (distance < closestDistance)
    //                 {
    //                     closestDistance = distance;
    //                     closestTarget = target;
    //                 }
    //             }

    //             if (closestTarget == null) return;

    //             if (_damageTimer.RemainingSeconds <= 0)
    //             {
    //                 _damageTimer.RemainingSeconds = TimeBetweenDamage;

    //                 // PlayerStats.Instance.SubtractMana(SpellData.Value.ManaCost);
    //                 // closestTarget.TakeDamageRpc(SpellData.Value.Damage, NetworkManager.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.transform.position, SpellData.Value.Knockback);
    //                 SoundManager.Instance.PlayOneShot(DamageSound, transform.position);
    //             }

    //             BeamStart.Value = wandPos;
    //             BeamEnd.Value = closestTarget.GetComponent<Collider2D>().bounds.center;
    //         }
    //         else
    //         {
    //             BeamStart.Value = wandPos;
    //             BeamEnd.Value = wandPos;
    //         }

    //         bool hasTargetNow = closestTarget != null;

    //         if (hasTargetNow && !_hadTargetLastFrame)
    //         {
    //             _sustainedElectricitySoundEventInstance.start();
    //             LightningStream.Play();
    //             BeamOn.Value = true;
    //         }
    //         else if (!hasTargetNow && _hadTargetLastFrame)
    //         {
    //             _sustainedElectricitySoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    //             LightningStream.Stop();
    //             BeamOn.Value = false;
    //         }

    //         _hadTargetLastFrame = hasTargetNow;
    //     }

    //     if (IsClient && IsStarted.Value)
    //     {
    //         // Calculate direction and distance
    //         if(BeamOn.Value)
    //         {
    //             Vector2 direction = BeamEnd.Value - BeamStart.Value;
    //             float distance = direction.magnitude;

    //             // Set lifetime based on distance
    //             var main = LightningStream.main;
    //             main.startLifetime = new ParticleSystem.MinMaxCurve(distance * LifetimePerDistanceUnit);

    //             // Rotate the particle system to face the direction of the beam
    //             if (direction != Vector2.zero)
    //             {
    //                 float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    //                 LightningStream.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    //                 LightningStream.transform.position = BeamStart.Value;
    //             }
    //         }
    //         else
    //         {
    //             var main = LightningStream.main;
    //             main.startLifetime = new ParticleSystem.MinMaxCurve(0.01f);
    //             LightningStream.transform.position = BeamStart.Value;
    //         }
    //     }
    // }

    // private void BeamOnChanged(bool previousValue, bool newValue)
    // {
    //     if (newValue)
    //     {
    //         LightningStream.Play();
    //     }
    //     else
    //     {
    //         LightningStream.Stop();
    //     }
    // }
}
