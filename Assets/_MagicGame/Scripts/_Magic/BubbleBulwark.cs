using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BubbleBulwark : Spell
{
    [field: SerializeField] public float BlastRadius;
    [field: SerializeField] public ParticleSystem PopParticles;

    private GameObject _vfx;

    protected override void OnOwnerSpellSpawned() 
    { 
        Debug.Log($"Loaded Bubble Spell");
    }

    protected override void OnOwnerExecuteSpellStart()
    {
        Debug.Log($"Executing Bubble Spell");
        
    }
    
    public override void OnOwnerSpellEnd()
    {
        Debug.Log($"Ending Bubble Spell");
        DetonateBubble();
        
        base.OnOwnerSpellEnd();
    }

    public override void OnOwnerSpellCanceled()
    {
        Debug.Log($"Cancelling Bubble Spell");
        
        base.OnOwnerSpellCanceled();
    }

    private void FixedUpdate()
    {
        transform.position = Player.LocalClientInstance.transform.position + new Vector3(0, 0.5f, 0);
    }
    
    private void DetonateBubble()
    {
        SpawnDetonateParticlesClientRpc(transform.position);
        DetonateSheild();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnDetonateParticlesClientRpc(Vector2 spawnPoint)
    {
        Debug.Log("Spawning Detonate Particles");
        _vfx = Instantiate(PopParticles.gameObject, _spellGameObject.transform.position, Quaternion.identity);
        _vfx.GetComponent<ParticleSystem>().Play();
    }

    private void DetonateSheild()
    {
        Debug.Log("Detonating Sheild");
        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, BlastRadius, CollisionMask);
        for (int i = 0; i < collisions.Length; i++)
        {
            int layerTest = 1 << collisions[i].gameObject.layer;
            if ((layerTest & CollisionMask) != 0)
            {
                if (collisions[i].gameObject.layer == NpcLayer)
                {
                    if (collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
                    {
                        NetworkHealthState npc = npcNet.gameObject.GetComponent<NetworkHealthState>();
                        if (!HitTargets.Contains(npc))
                        {
                            npc.TakeDamageRpc(SpellData.Value.Damage, transform.position, SpellData.Value.Knockback);
                        }
                    }
                }
            }
        }
    }
}
