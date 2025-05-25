using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
using System.Collections;

public class FlameBreath : Spell
{
    [field: SerializeField] public float TimeBetweenDamage { get; private set; } = 0.25f;
    [field: SerializeField] public float CooldownBetweenPasses { get; private set; } = 0.5f;
    [field: SerializeField] public int MaxSimultaneousDamagedNPCs { get; private set; } = 5;
    [field: SerializeField] public float TimeBetweenFoliageHits { get; private set; } = 0.2f;
    [field: SerializeField] public ParticleSystem FlameBreathParticles { get; private set; }
    [field: SerializeField] public EventReference SustainedFireSound { get; private set; }

    private EventInstance _sustainedFireSoundEventInstance;
    private Timer _damageTimer;
    private List<NetworkHealthState> _queuedTargetsToDamage = new();
    private Coroutine _damageSequenceCoroutine;

    private List<FoliageCollider> _queuedFoliageToDestroy = new();
    private Coroutine _foliageSequenceCoroutine;

    protected override void OnSpellSpawned()
    {
        
    }

    protected override void OnExecuteSpellStart()
    {
        _sustainedFireSoundEventInstance = SoundManager.Instance.CreateInstance(SustainedFireSound);
        _sustainedFireSoundEventInstance.start();
        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(SpellData.Value.HasteMultiplier);
    }

    protected override void OnSpellEnd()
    {
        _sustainedFireSoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        StopAllParticleSystems(Visualization.transform);
        Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1);
    }

    protected override void OnSpellCanceled()
    {
        
    }

    private void StopAllParticleSystems(Transform parent)
    {
        ParticleSystem ps = parent.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        foreach (Transform child in parent)
        {
            StopAllParticleSystems(child);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (IsOwner && IsStarted.Value)
        {
            Vector2 wandPos = Player.LocalClientInstance.MainHand.SpellSpawnTransform.position;
            transform.position = wandPos;

            Vector2 mousePosition = ActionManager.MouseWorldPosition;
            Vector2 direction = mousePosition - wandPos;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (_queuedTargetsToDamage.Count == 0 && _damageSequenceCoroutine == null)
            {
                HashSet<NetworkHealthState> uniqueTargets = new();
                var particles = new ParticleSystem.Particle[FlameBreathParticles.particleCount];
                FlameBreathParticles.GetParticles(particles);

                foreach (var particle in particles)
                {
                    var colliders = Physics2D.OverlapPointAll(particle.position);
                    foreach (var collider in colliders)
                    {
                        if (IsValidNpcHit(collider, out NetworkHealthState npcHealth)) // Detect NPCs
                        {
                            uniqueTargets.Add(npcHealth);
                            if (uniqueTargets.Count >= MaxSimultaneousDamagedNPCs)
                                break;
                        }

                        if (collider.gameObject.layer == 16) // Detect Foliage
                        {
                            var foliage = collider.GetComponent<FoliageCollider>();
                            if (foliage != null && !_queuedFoliageToDestroy.Contains(foliage))
                            {
                                _queuedFoliageToDestroy.Add(foliage);
                            }
                        }
                    }

                    if (uniqueTargets.Count >= MaxSimultaneousDamagedNPCs)
                        break;
                }

                _queuedTargetsToDamage.AddRange(uniqueTargets);

                if (_queuedTargetsToDamage.Count > 0)
                {
                    _damageSequenceCoroutine = StartCoroutine(DamageTargetsInSequence());
                }

                if (_queuedFoliageToDestroy.Count > 0 && _foliageSequenceCoroutine == null)
                {
                    var foliageToProcess = new List<FoliageCollider>(_queuedFoliageToDestroy);
                    _queuedFoliageToDestroy.Clear();

                    _foliageSequenceCoroutine = StartCoroutine(DestroyFoliageSequence(foliageToProcess));
                }
            }
        }
    }

    private IEnumerator DamageTargetsInSequence()
    {
        foreach (var target in _queuedTargetsToDamage)
        {
            target.TakeDamageRpc(
                SpellData.Value.Damage,
                NetworkManager.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.transform.position,
                SpellData.Value.Knockback
            );

            yield return new WaitForSeconds(TimeBetweenDamage);
        }

        yield return new WaitForSeconds(CooldownBetweenPasses);
        _queuedTargetsToDamage.Clear();
        _damageSequenceCoroutine = null;
    }

    private IEnumerator DestroyFoliageSequence(List<FoliageCollider> foliageList)
    {
        foreach (var foliage in foliageList)
        {
            if (foliage == null || foliage.gameObject == null) continue;

            Vector3 pos = foliage.transform.position;
            Vector2Int tilePos = new Vector2Int((int)pos.x, (int)pos.y);
            int tileId = GameManager.Instance.GetTileIDFromTilemapTilePosition(
                TileManager.Instance.FoliageTm, (Vector3Int)tilePos
            );

            if (foliage != null && foliage.gameObject != null)
            {
                foliage.DestroyFoliage();
            }
            
            TileManager.Instance.DestroyTileServerRpc(tilePos, tileId, Player.LocalClientInstance.CurrentPlayerBiome.Value);

            // Final null check before calling method
            

            yield return new WaitForSeconds(TimeBetweenFoliageHits);
        }

        _queuedFoliageToDestroy.Clear();
        _foliageSequenceCoroutine = null;
    }
}
