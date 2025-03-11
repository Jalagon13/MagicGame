using UnityEngine;

public class LaserBeam : Spell
{
    [SerializeField] private float _beamLength = 5f;
    [SerializeField] private LineRenderer _beamLine;

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);
        
        
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        _beamLine.SetPosition(0, transform.position);
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, _finalDirection, _beamLength, CollisionMask);
        foreach (var hit in hits)
        {
            if(hit.collider.gameObject.layer == WallMask)
            {
                if (hit.collider.gameObject.TryGetComponent(out PathfindingWallTm pfWall))
                {
                    if (pfWall.BiomeSameAs(SpellDataNV.Value.SpawnBiome))
                    {
                        _beamLine.SetPosition(1, hit.point);
                        return;
                    }
                }
            }
        }

        _beamLine.SetPosition(1, (Vector2)transform.position + _finalDirection * _beamLength);
    }
}
