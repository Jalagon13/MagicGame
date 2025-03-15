using System.Collections;
using NUnit.Framework.Internal;
using Unity.Netcode;
using UnityEngine;

public class SparkBlast : Spell
{
    [SerializeField] private float _blastRadius = 1.25f;
    [SerializeField] private ParticleSystem _detonateParticles;

    private GameObject _vfx;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _vfx = Instantiate(_detonateParticles.gameObject, _spellGameObject.transform);
        _vfx.transform.localPosition = Vector3.zero;
    }

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);
        
        SpawnBlastParticlesClientRpc(spawnPoint);

        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _blastRadius, CollisionMask);
        for (int i = 0; i < collisions.Length; i++)
        {
            int layerTest = 1 << collisions[i].gameObject.layer;
            if ((layerTest & CollisionMask) != 0)
            {
                if (collisions[i].gameObject.layer == NpcLayer)
                {
                    if (collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
                    {
                        Npc npc = npcNet.gameObject.GetComponent<Npc>();
                        if (!HitTargets.Contains(npc))
                        {
                            npc.ApplyDamage(SpellData.Value.Damage, transform.position, SpellData.Value.Knockback);
                        }
                    }
                }
            }
        }
        
        StartCoroutine(StopSparkBlast());
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnBlastParticlesClientRpc(Vector2 spawnPoint)
    {
        _vfx.transform.position = spawnPoint;
        _vfx.GetComponent<ParticleSystem>().Play();
    }
    
    private IEnumerator StopSparkBlast()
    {
        _isDead = true;
        Debug.Log($"Stopping SnapBlast after {_detonateParticles.main.duration} seconds");
        yield return new WaitForSeconds(_detonateParticles.main.duration);
        TerminateSpell();
    }
}
