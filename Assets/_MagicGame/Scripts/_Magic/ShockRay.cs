using System.Collections.Generic;
using UnityEngine;

public class ShockRay : Spell
{
    [field: SerializeField] public float Range { get; private set; }
    [field: SerializeField] public LineRenderer BeamRenderer { get; private set; }

    private List<GameObject> _potentialTargetsToLockOnTo = new();

    protected override void OnOwnerSpellSpawned()
    {
        Debug.Log($"Spawned Shockray Spell. Client owner: {OwnerClientId}");
        
        
    }

    protected override void OnOwnerExecuteSpellStart()
    {
        Debug.Log($"Executing Shockray Spell. Owner: {NetworkObject.OwnerClientId}");

        BeamRenderer.useWorldSpace = true;
    }

    protected override void OnOwnerSpellEnd()
    {
        Debug.Log($"Ending Shockray Spell");

        base.OnOwnerSpellEnd();
    }

    public override void OnOwnerSpellCanceled()
    {
        Debug.Log($"Cancelling Shockray Spell");

        base.OnOwnerSpellCanceled();
    }

    protected override void Update()
    {
        base.Update();

        if(!IsOwner || !IsStarted.Value) return;

        _potentialTargetsToLockOnTo.Clear();
        BeamRenderer.positionCount = 0;
        BeamRenderer.enabled = false;
        
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
                        if(!_potentialTargetsToLockOnTo.Contains(collisions[i].gameObject))
                        {
                            _potentialTargetsToLockOnTo.Add(collisions[i].gameObject);
                        }
                    }
                }
            }
        }

        if (_potentialTargetsToLockOnTo.Count > 0)
        {
            // Pick the closest target to home to
            float closestDistance = float.MaxValue;
            GameObject closestTarget = null;
            foreach (GameObject target in _potentialTargetsToLockOnTo)
            {
                float distance = Vector2.Distance(transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }
            
            if(closestTarget == null) return;
            Debug.Log($"Locking on to {closestTarget.name}");
            BeamRenderer.enabled = true;
            BeamRenderer.positionCount = 2;
            BeamRenderer.SetPosition(0, wandPos);
            BeamRenderer.SetPosition(1, closestTarget.transform.position);
        }
    }
}
