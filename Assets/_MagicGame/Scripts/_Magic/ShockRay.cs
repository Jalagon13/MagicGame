using System.Collections.Generic;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class ShockRay : Spell
{
    [field: SerializeField] public float Range { get; private set; }
    [field: SerializeField] public float TimeBetweenDamage { get; private set; } = 0.25f;
    [field: SerializeField] public LineRenderer BeamRenderer { get; private set; }
    [field: SerializeField] public EventReference DamageSound { get; private set; }

    public NetworkVariable<bool> BeamVisible { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> BeamStart { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> BeamEnd { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private List<NetworkHealthState> _potentialTargetsToLockOnTo = new();
    private Timer _damageTimer;

    protected override void OnOwnerSpellSpawned()
    {
        
    }

    protected override void OnOwnerExecuteSpellStart()
    {
        BeamRenderer.useWorldSpace = true;
        _damageTimer = new Timer(0.1f);
    }

    public override void OnOwnerSpellEnd()
    {
        // Any local cleanup goes here

        base.OnOwnerSpellEnd();
    }

    protected override void Update()
    {
        base.Update();

        if(IsOwner && IsStarted.Value)
        {
            _damageTimer.Tick(Time.deltaTime);
            _potentialTargetsToLockOnTo.Clear();
            BeamVisible.Value = false;

            Vector2 wandPos = Player.LocalClientInstance.MainHand.SpellSpawnTransform.position;
            Collider2D[] collisions = Physics2D.OverlapCircleAll(wandPos, Range, CollisionMask);

            for (int i = 0; i < collisions.Length; i++)
            {
                int layerTest = 1 << collisions[i].gameObject.layer;
                if ((layerTest & CollisionMask) != 0)
                {
                    if (collisions[i].gameObject.layer == NpcLayer)
                    {
                        if (collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
                        {
                            NetworkHealthState npc = npcNet.GetComponent<NetworkHealthState>();
                            if (!_potentialTargetsToLockOnTo.Contains(npc))
                            {
                                _potentialTargetsToLockOnTo.Add(npc);
                            }
                        }
                    }
                }
            }

            NetworkHealthState closestTarget = null;

            if (_potentialTargetsToLockOnTo.Count > 0)
            {
                // Pick the closest target to home to
                float closestDistance = float.MaxValue;

                foreach (NetworkHealthState target in _potentialTargetsToLockOnTo)
                {
                    float distance = Vector2.Distance(wandPos, target.transform.position);
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

                    closestTarget.TakeDamageRpc(SpellData.Value.Damage, NetworkManager.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.transform.position, SpellData.Value.Knockback);
                    SoundManager.Instance.PlayOneShot(DamageSound, transform.position);
                }

                BeamVisible.Value = true;
                BeamStart.Value = wandPos;
                BeamEnd.Value = closestTarget.transform.position;
            }
        }

        if(IsClient && IsStarted.Value)
        {
            BeamRenderer.enabled = BeamVisible.Value;
            BeamRenderer.SetPosition(0, BeamStart.Value);
            BeamRenderer.SetPosition(1, BeamEnd.Value);
        }
    }
}
