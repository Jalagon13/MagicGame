using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class FlameBreath : ServerSpell
{
    [SerializeField] 
    private EventReference _sustainedFireSound;
    
    [SerializeField] 
    private ParticleSystem _flameBreathParticles;
    
    [SerializeField] 
    private int _maxNpcsThatCanBeDamaged = 5;

    [SerializeField]
    private float _timeBetweenDamage = 0.25f;

    [SerializeField]
    private float _cooldownBetweenPasses = 0.175f;

    private EventInstance _sustainedFireSoundEventInstance;
    private List<DamageReceiver> _queuedTargetsToDamage = new();
    private Coroutine _damageSequenceCoroutine;
    private List<FoliageCollider> _queuedFoliageToDestroy = new();
    private Coroutine _foliageSequenceCoroutine;
    
    private NetworkVariable<Vector2> _direction = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> Direction => _direction;

    protected override void OnSpellExecute()
    {

    }

    protected override void OnUpdateSpell()
    {
        Vector2 wandPos = Player.LocalClientInstance.PlayerHand.SpellSpawnTransform.position; // NTFS: Change this for general servercharacter usability later
        transform.position = wandPos;

        Vector2 mousePosition = ActionManager.MouseWorldPosition; // NTFS: Change this for general servercharacter usability later
        _direction.Value = mousePosition - wandPos;
        
        float angle = Mathf.Atan2(_direction.Value.y, _direction.Value.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (_queuedTargetsToDamage.Count == 0 && _damageSequenceCoroutine == null)
        {
            HashSet<DamageReceiver> uniqueTargets = new();
            var particles = new ParticleSystem.Particle[_flameBreathParticles.particleCount];
            _flameBreathParticles.GetParticles(particles);

            foreach (var particle in particles)
            {
                var colliders = Physics2D.OverlapPointAll(particle.position);
                foreach (var collider in colliders)
                {
                    if (IsValidNpcHit(collider, out DamageReceiver damageReciever)) // Detect NPCs
                    {
                        uniqueTargets.Add(damageReciever);
                        if (uniqueTargets.Count >= _maxNpcsThatCanBeDamaged)
                            break;
                    }

                    if (collider.gameObject.layer == FoliageLayer) // Detect Foliage
                    {
                        var foliage = collider.GetComponent<FoliageCollider>();
                        if (foliage != null && !_queuedFoliageToDestroy.Contains(foliage))
                        {
                            _queuedFoliageToDestroy.Add(foliage);
                        }
                    }
                }

                if (uniqueTargets.Count >= _maxNpcsThatCanBeDamaged)
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

    private IEnumerator DamageTargetsInSequence()
    {
        foreach (DamageReceiver target in _queuedTargetsToDamage)
        {
            if (SpellCasterNetworkObject.TryGetComponent(out ServerCharacter inflicter))
            {
                target.ReceiveHP(inflicter, -SpellData.Value.Damage, true, SpellData.Value.Knockback);
            }

            yield return new WaitForSeconds(_timeBetweenDamage);
        }

        yield return new WaitForSeconds(_cooldownBetweenPasses);
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

            TileManager.Instance.DestroyTileServerRpc(tilePos, tileId, Player.LocalClientInstance.CurrentBiome.Value);

            // Final null check before calling method


            yield return new WaitForSeconds(_timeBetweenDamage);
        }

        _queuedFoliageToDestroy.Clear();
        _foliageSequenceCoroutine = null;
    }

    protected override IEnumerator OnSpellEnd()
    {
        if (_damageSequenceCoroutine != null)
        {
            StopCoroutine(_damageSequenceCoroutine);
            _damageSequenceCoroutine = null;
        }
        if (_foliageSequenceCoroutine != null)
        {
            StopCoroutine(_foliageSequenceCoroutine);
            _foliageSequenceCoroutine = null;
        }

        // Find the longest particle system duration under the visualization GameObject
        float maxDuration = 0f;
        if (ClientSpell != null && ClientSpell.Visualization != null)
        {
            var particleSystems = ClientSpell.Visualization.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                // Duration = startLifetime (can be a curve, so take the max) + duration (looping particles may need special handling)
                float lifetime = ps.main.startLifetime.constantMax;
                float duration = ps.main.duration;
                float totalDuration = ps.main.loop ? lifetime : duration + lifetime;
                if (totalDuration > maxDuration)
                    maxDuration = totalDuration;
            }
        }

        if (maxDuration > 0f)
            yield return new WaitForSeconds(maxDuration);
        else
            yield return null;
    }

    public override void ClientSpellStart(ClientSpell clientSpell)
    {
        clientSpell.Visualization.SetActive(true);
    
        _sustainedFireSoundEventInstance = SoundManager.Instance.CreateInstance(_sustainedFireSound);
        _sustainedFireSoundEventInstance.start();

        SpellItemSO spellItemSO = GameManager.Instance.GetItemSOFromItemId(SpellData.Value.SpellItemId) as SpellItemSO;
        SoundManager.Instance.PlayOneShot(spellItemSO.SpellCastSound, transform.position);
    }
    
    public override void ClientSpellUpdate(ClientSpell clientSpell)
    {
        // Update the position and rotation of the flame breath visualization

        // float angle = Mathf.Atan2(Direction.Value.y, Direction.Value.x) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public override void ClientSpellStop(ClientSpell clientSpell)
    {
        _sustainedFireSoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        StopAllParticleSystems(clientSpell.Visualization.transform);
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
}