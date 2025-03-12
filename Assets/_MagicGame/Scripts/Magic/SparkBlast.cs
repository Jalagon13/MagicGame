using System.Collections;
using UnityEngine;

public class SparkBlast : Spell
{
    [SerializeField] private float _blastRadius = 1.25f;
    [SerializeField] private ParticleSystem _detonateParticles;

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);

        var go = Instantiate(_detonateParticles.gameObject, transform.position, Quaternion.identity);
        go.GetComponent<ParticleSystem>().Play();

        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _blastRadius, CollisionMask);
        for (int i = 0; i < collisions.Length; i++)
        {
            int layerTest = 1 << collisions[i].gameObject.layer;
            if ((layerTest & CollisionMask) != 0)
            {
                if (collisions[i].gameObject.layer == NpcLayer)
                {
                    if (collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellDataNV.Value.SpawnBiome))
                    {
                        Npc npc = npcNet.gameObject.GetComponent<Npc>();
                        if (!HitTargets.Contains(npc))
                        {
                            npc.ApplyDamage(SpellDataNV.Value.Damage, transform.position, SpellDataNV.Value.Knockback);
                        }
                    }
                }
            }
        }
        
        if(_miningSpellMod != null)
        {
            _miningSpellMod.TryToHitTiles(_blastRadius, transform.position);
        }

        StartCoroutine(StopSparkBlast());
    }
    
    private IEnumerator StopSparkBlast()
    {
        _isDead = true;
        Debug.Log($"Stopping SnapBlast after {_detonateParticles.main.duration} seconds");
        yield return new WaitForSeconds(_detonateParticles.main.duration);
        TerminateSpell();
    }
}
