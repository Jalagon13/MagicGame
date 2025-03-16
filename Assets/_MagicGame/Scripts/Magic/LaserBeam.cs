using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LaserBeam : Spell
{
    [SerializeField] private float _beamLength = 5f;
    [SerializeField] private float _beamWidth = 0.2f;
    [SerializeField] private float _laserTickDuration = 0.2f;
    [SerializeField] private float _movementSpeedModifier = 0.75f;
    [SerializeField] private float _reflectionMinimumLength = 0.25f;
    [SerializeField] private LineRenderer _beamLine;
    
    private Vector2 _desiredDirection;
    private Vector2 _spellSpawnPosition;
    private Timer _laserTickTimer;
    private HashSet<Vector2> _contactPoints = new();
    private List<Vector2> _laserPoints = new();
    private float _distanceTraveled;

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);
        
        NetworkObject.ChangeOwnership(SpellData.Value.OwnerPlayerId);
        NetworkObject.DontDestroyWithOwner = true;
        OnSpellEnd += LaserBeamEndHandle;

        _laserPoints = new(SpellData.Value.Bounces + 2);
        _contactPoints = new(SpellData.Value.Bounces);

        _laserTickTimer = new Timer(_laserTickDuration);
        _laserTickTimer.OnTimerEnd += LaserTick;
    }

    private void LaserTick(object sender, EventArgs e)
    {
        foreach (NetworkHealthState npc in HitTargets)
        {
            npc.TakeDamageRpc(SpellData.Value.Damage, NetworkManager.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.transform.position, SpellData.Value.Knockback);
        }
        
        _laserTickTimer.RemainingSeconds = _laserTickDuration;
    }

    private void LaserBeamEndHandle(object sender, EventArgs e)
    {
        OnSpellEnd -= LaserBeamEndHandle;
        _laserTickTimer.OnTimerEnd -= LaserTick;
        
        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1);
    }

    protected override void FixedUpdate()
    {
        Debug.Log($"");
        base.FixedUpdate();
        
        if(!Started.Value) return;
        
        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(_movementSpeedModifier);
        
        _distanceTraveled = 0;
        _bounces = 0;
        _beamLine.positionCount = 0;
        _beamLine.startWidth = _beamWidth;
        _beamLine.endWidth = _beamWidth;
        _beamLine.alignment = LineAlignment.View;

        _spellSpawnPosition = Player.LocalClientInstance.MainHand.SpellSpawnTransform.position;
        _desiredDirection = (ActionManager.MouseWorldPosition - _spellSpawnPosition).normalized;
        _desiredDirection.Normalize();

        _laserTickTimer.Tick(Time.fixedDeltaTime);

        HitTargets.Clear();
        _contactPoints.Clear();
        _laserPoints.Clear();
        _laserPoints.Add(_spellSpawnPosition);

        RaycastHit2D[] hits = Physics2D.RaycastAll(_spellSpawnPosition, _desiredDirection, _beamLength, CollisionMask);
        foreach (var hit in hits)
        {
            int layerTest = 1 << hit.collider.gameObject.layer;
            if ((layerTest & CollisionMask) != 0)
            {
                if (hit.collider.gameObject.layer == NpcLayer)
                {
                    if (hit.collider.TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
                    {
                        if (!HitTargets.Contains(npcNet.GetComponent<NetworkHealthState>()))
                        {
                            HitTargets.Add(npcNet.GetComponent<NetworkHealthState>());
                        }

                        if (HitTargets.Count == SpellData.Value.Pierces + 1)
                        {
                            _laserPoints.Add(hit.point);
                            ConstructLaser();
                            return;
                        }
                    }
                }
            }
        }

        foreach (var hit in hits)
        {
            int layerTest = 1 << hit.collider.gameObject.layer;
            if ((layerTest & CollisionMask) != 0)
            {
                if (hit.collider.gameObject.layer == WallMask)
                {
                    if (hit.collider.gameObject.TryGetComponent(out PathfindingWallTm pfWall))
                    {
                        if (pfWall.BiomeSameAs(SpellData.Value.SpawnBiome))
                        {
                            _bounces++;
                            _distanceTraveled += Vector2.Distance(_spellSpawnPosition, hit.point);
                            if (_bounces >= SpellData.Value.Bounces + 1)
                            {
                                _laserPoints.Add(hit.point);
                                _contactPoints.Add(hit.point);
                                ConstructLaser();
                                return;
                            }
                            else
                            {
                                ReflectLaser(hit, _desiredDirection);
                                return;
                            }
                        }
                    }
                }
            }
        }

        _laserPoints.Add(_spellSpawnPosition + (_desiredDirection * _beamLength));
        ConstructLaser();
    }

    private void ReflectLaser(RaycastHit2D incomingHit, Vector2 incomingDirection)
    {
        Vector2 reflectedDirection = Vector2.Reflect(incomingDirection.normalized, incomingHit.normal);
        reflectedDirection.Normalize();
        
        _laserPoints.Add(incomingHit.point);
        _contactPoints.Add(incomingHit.point);
        
        float remainingDistance = _beamLength - _distanceTraveled;
        if (remainingDistance < _reflectionMinimumLength)
        {
            ConstructLaser();
            return;
        }

        Vector2 rayOffset = incomingHit.point + (incomingHit.normal * 0.01f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOffset, reflectedDirection, remainingDistance, CollisionMask);
        foreach (var hit in hits)
        {
            int layerTest = 1 << hit.collider.gameObject.layer;
            if ((layerTest & CollisionMask) != 0)
            {
                if (hit.collider.gameObject.layer == NpcLayer)
                {
                    if (hit.collider.TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
                    {
                        if (!HitTargets.Contains(npcNet.GetComponent<NetworkHealthState>()))
                        {
                            HitTargets.Add(npcNet.GetComponent<NetworkHealthState>());
                        }

                        if (HitTargets.Count == SpellData.Value.Pierces + 1)
                        {
                            _laserPoints.Add(hit.point);
                            ConstructLaser();
                            return;
                        }
                    }
                }
            }
        }

        foreach (var hit in hits)
        {
            int layerTest = 1 << hit.collider.gameObject.layer;
            if ((layerTest & CollisionMask) != 0)
            {
                if (hit.collider.gameObject.layer == WallMask)
                {
                    if (hit.collider.gameObject.TryGetComponent(out PathfindingWallTm pfWall))
                    {
                        if (pfWall.BiomeSameAs(SpellData.Value.SpawnBiome))
                        {
                            if(_contactPoints.Contains(hit.point)) continue; // If processing the incomingHit, continue

                            _bounces++;
                            _distanceTraveled += Vector2.Distance(rayOffset, hit.point);

                            if (_bounces >= SpellData.Value.Bounces + 1)
                            {
                                _laserPoints.Add(hit.point);
                                ConstructLaser();
                                return;
                            }
                            else
                            {
                                ReflectLaser(hit, reflectedDirection);
                                return;
                            }
                        }
                    }
                }
            }
        }

        _laserPoints.Add(incomingHit.point + (reflectedDirection * remainingDistance));
        ConstructLaser();
    }
    
    private void ConstructLaser()
    {
        _beamLine.positionCount = _laserPoints.Count;
        _beamLine.startWidth = _beamWidth;
        _beamLine.endWidth = _beamWidth;
        
        for (int i = 0; i < _laserPoints.Count; i++)
        {
           _beamLine.SetPosition(i, _laserPoints[i]);
        }
    }
}
