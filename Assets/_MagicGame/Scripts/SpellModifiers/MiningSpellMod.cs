using System.Collections.Generic;
using UnityEngine;

public class MiningSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private MiningFocusItemSO _miningFocusItemSO;
    
    private List<Vector2> _tilesHit = new List<Vector2>();

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
        Debug.Log($"Mining Spell Mod");
        return spellData;
    }

    public void TryToHitTiles(float radius)
    {
        Vector2 worldPos = transform.position;
        Vector2Int centerTile = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

        int tileRadius = Mathf.CeilToInt(radius);

        for (int x = -tileRadius; x <= tileRadius; x++)
        {
            for (int y = -tileRadius; y <= tileRadius; y++)
            {
                Vector2Int tilePos = new Vector2Int(centerTile.x + x, centerTile.y + y);
                Vector2 tileCenter = new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);

                if(_tilesHit.Contains(tileCenter))
                {
                    continue;
                }

                if (Vector2.Distance(tileCenter, new Vector2(centerTile.x + 0.5f, centerTile.y + 0.5f)) <= tileRadius)
                {
                    // Process the tile here
                    if (Environment.Instance.WallTm.HasTile(Vector3Int.FloorToInt(tileCenter)))
                    {
                        Environment.Instance.HitWallTile(Player.LocalClientInstance.CurrentPlayerBiome.Value, Vector2Int.FloorToInt(tileCenter), _miningFocusItemSO.MiningPower);
                        _miningFocusItemSO.SpawnMiningVisuals(tileCenter);
                        _tilesHit.Add(tileCenter);
                    }
                }
            }
        }
    }
}
