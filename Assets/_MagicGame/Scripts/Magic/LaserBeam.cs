using System;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeam : Spell
{
    [SerializeField] private float _beamLength = 5f;
    [SerializeField] private float _beamWidth = 0.2f;
    [SerializeField] private float _laserTickDuration = 0.2f;
    [SerializeField] private float _movementSpeedModifier = 0.75f;
    [SerializeField] private LineRenderer _beamLine;
    
    private Vector2 _desiredDirection;
    private Vector2 _spellSpawnPosition;
    private Vector2 _endPoint;
    private Timer _laserTickTimer;
    private List<Npc> _hitTargets = new();

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);
        
        NetworkObject.ChangeOwnership(SpellDataNV.Value.OwnerPlayerId);
        NetworkObject.DontDestroyWithOwner = true;
        
        OnSpellEnd += LaserBeamEndHandle;

        _beamLine.startWidth = _beamWidth;
        _beamLine.endWidth = _beamWidth;
        
        _laserTickTimer = new Timer(_laserTickDuration);
        _laserTickTimer.OnTimerEnd += LaserTick;
        
        _hitTargets = new(SpellDataNV.Value.Pierces == 0 ? 1 : SpellDataNV.Value.Pierces);
    }

    private void LaserTick(object sender, EventArgs e)
    {
        if (_miningSpellMod != null)
        {
            Debug.Log("Trying to hit tiles");
            _miningSpellMod.TryToHitTiles(_beamWidth, _endPoint, false);
        }

        foreach (Npc npc in _hitTargets)
        {
            npc.ApplyDamage(SpellDataNV.Value.Damage, NetworkManager.ConnectedClients[SpellDataNV.Value.OwnerPlayerId].PlayerObject.transform.position, SpellDataNV.Value.Knockback);
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
        base.FixedUpdate();
        
        if(!Started) return;

        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(_movementSpeedModifier);

        _laserTickTimer.Tick(Time.fixedDeltaTime);

        _spellSpawnPosition = Player.LocalClientInstance.MainHand.SpellSpawnTransform.position;
        _desiredDirection = (ActionManager.MouseWorldPosition - _spellSpawnPosition).normalized;
        _desiredDirection.Normalize();
        
        _beamLine.SetPosition(0, _spellSpawnPosition);
        _hitTargets.Clear();

        RaycastHit2D[] hits = Physics2D.RaycastAll(_spellSpawnPosition, _desiredDirection, _beamLength, CollisionMask);
        foreach (var hit in hits)
        {
            int layerTest = 1 << hit.collider.gameObject.layer;
            
            if ((layerTest & CollisionMask) != 0)
            {
                if (hit.collider.gameObject.layer == WallMask)
                {
                    if (hit.collider.gameObject.TryGetComponent(out PathfindingWallTm pfWall))
                    {
                        if (pfWall.BiomeSameAs(SpellDataNV.Value.SpawnBiome))
                        {
                            _endPoint = hit.point;
                            _beamLine.SetPosition(1, hit.point);
                            return;
                        }
                    }
                }

                if (hit.collider.gameObject.layer == NpcLayer)
                {
                    if (hit.collider.TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellDataNV.Value.SpawnBiome))
                    {
                        _beamLine.SetPosition(1, hit.point);
                        
                        if(!_hitTargets.Contains(npcNet.GetComponent<Npc>()))
                        {
                            _hitTargets.Add(npcNet.GetComponent<Npc>());
                        }
                        return;
                    }
                }
            }
        }
        
        _endPoint = _spellSpawnPosition + (_desiredDirection * _beamLength);
        _beamLine.SetPosition(1, _endPoint);
    }
}
