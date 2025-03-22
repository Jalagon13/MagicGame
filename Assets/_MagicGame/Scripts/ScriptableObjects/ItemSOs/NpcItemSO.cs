using System;
using System.Collections.Generic;
using System.Text;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Npc Item", menuName = "Create Item/New NPC Item")]
public class NpcItemSO : ItemSO
{
    [field: SerializeField] public NpcSO NpcToSpawn { get; private set; }
    [Tooltip("For debugging purposes")]
    [field: SerializeField] public bool IgnoreNpcHousingCheck { get; private set; } = false;

    private static readonly int _maxFloorTiles = 81;
    private static readonly int _minFloorTiles = 9;

    // NTFS: Make it so this item can spawn any NPC you choose it to spawn in the inspector
    public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
    {
        Vector2 pos = ActionManager.MouseWorldPosition;

        if (IsClear(pos) && PlayerInRangeOfMouse() && NpcHousingCheck())
        {
            Vector2 spawnPosition = new(Mathf.FloorToInt(pos.x) + 0.5f, Mathf.FloorToInt(pos.y) + 0.5f);
            NpcManager.Instance.SpawnNpc(spawnPosition, NpcToSpawn); // NTFS: There is no check for npc slot limit, right now it is assumed this will spawn 0 npc slot NPCs like mercahnts and training dummies
            InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
        }

        return _baseActionCooldown;
    }

    private bool NpcHousingCheck()
    {
        if (IgnoreNpcHousingCheck) return true;
    
        // Initialize tilemaps and variables
        Tilemap wallTilemap = Environment.Instance.WallTm;
        Tilemap floorTilemap = Environment.Instance.FloorTm;
        List<Vector3Int> floorTilePositions = new();
        List<Vector3Int> wallTilePositions = new();
        List<Vector3Int> doorPositions = new();
        Stack<Vector3Int> tilesToCheck = new();
        
        // Check if the player is in the correct biome
        if(Player.LocalClientInstance.CurrentPlayerBiome.Value != BiomeType.Forest)
        {
            Debug.LogWarning("Npc can only be spawned in the forest biome");
            return false;
        }

        // Start flood-fill algorithm from the current position
        var checkPos = Vector3Int.FloorToInt(ActionManager.MouseWorldPosition);
        tilesToCheck.Push(checkPos);

        int iterationLimit = _maxFloorTiles * 3; // Max * 3 for iteration limit
        int iterations = 0;

        while (tilesToCheck.Count > 0)
        {
            if (iterations >= iterationLimit)
            {
                Debug.LogWarning("Tile check exceeded iteration limit.");
                break;
            }

            var p = tilesToCheck.Pop();
            iterations++;

            // Check for valid floor and wall tiles
            if (!floorTilemap.HasTile(p) && !wallTilemap.HasTile(p) && !HasDoor(p))
            {
                Debug.LogWarning("Room needs to be surrounded by floor and wall tiles.");
                return false;
            }

            // Collect wall tiles
            if (wallTilemap.HasTile(p) && !wallTilePositions.Contains(p))
            {
                wallTilePositions.Add(p);
                continue;
            }

            // Check for doors
            if (HasDoor(p))
            {
                doorPositions.Add(p);
                continue;
            }

            // Collect floor tiles
            if (floorTilemap.HasTile(p) && !floorTilePositions.Contains(p))
            {
                floorTilePositions.Add(p);

                PushAdjacentTiles(p, tilesToCheck);
            }
        }

        // Validate the number of floor tiles
        if (floorTilePositions.Count < _minFloorTiles)
        {
            Debug.LogWarning("House is too small");
            return false;
        }
        
        // Validate the number of floor tiles
        if(floorTilePositions.Count > _maxFloorTiles)
        {
            Debug.LogWarning("House is too large");
            return false;
        }

        // Check for at least one door
        if (doorPositions.Count == 0)
        {
            Debug.LogWarning("No door found!");
            return false;
        }

        // Check if doors are flanked by walls and one side is on the floor
        bool validDoorFound = false;
        foreach (var doorPos in doorPositions)
        {
            bool northWall = wallTilePositions.Contains(new Vector3Int(doorPos.x, doorPos.y + 1));
            bool southWall = wallTilePositions.Contains(new Vector3Int(doorPos.x, doorPos.y - 1));
            bool eastWall = wallTilePositions.Contains(new Vector3Int(doorPos.x + 1, doorPos.y));
            bool westWall = wallTilePositions.Contains(new Vector3Int(doorPos.x - 1, doorPos.y));

            if ((northWall && southWall) || (eastWall && westWall))
            {
                validDoorFound = true;
                break;
            }
        }

        if (!validDoorFound)
        {
            Debug.LogWarning("Doors needs to be flanked by 2 walls and lead outside.");
            return false;
        }

        // Check for torches in the enclosed area
        if (!HasTorch(floorTilePositions))
        {
            Debug.LogWarning("Room needs a light source");
            return false;
        }

        // If all checks pass
        return true;
    }

    private void PushAdjacentTiles(Vector3Int position, Stack<Vector3Int> tilesToCheck)
    {
        Vector3Int[] adjacentPositions = {
            new (position.x - 1, position.y),
            new (position.x + 1, position.y),
            new (position.x, position.y - 1),
            new (position.x, position.y + 1)
        };

        foreach (var adjPos in adjacentPositions)
        {
            if (!tilesToCheck.Contains(adjPos))
            {
                tilesToCheck.Push(adjPos);
            }
        }
    }

    private bool HasTorch(List<Vector3Int> floorTilePositions)
    {
        foreach (var pos in floorTilePositions)
        {
            var centerPos = new Vector2(pos.x + 0.5f, pos.y + 0.5f);
            var colliders = Physics2D.OverlapCircleAll(centerPos, 0.1f);

            foreach (Collider2D col in colliders)
            {
                if (col.CompareTag("LightSource"))
                    return true;
            }
        }
        return false;
    }

    private bool HasDoor(Vector3Int position)
    {
        var centerPos = new Vector2(position.x + 0.5f, position.y + 0.5f);
        var colliders = Physics2D.OverlapCircleAll(centerPos, 0.1f);

        foreach (Collider2D col in colliders)
        {
            if (col.TryGetComponent(out DoorObject door))
                return true;
        }
        return false;
    }

    public override InventoryItem CreateInventoryItem(int quantity)
    {
        return new InventoryItem(this, quantity);
    }

    public override string GetDescription()
    {
        StringBuilder description = new();
        description.Append($"Places an NPC<br>");
        description.Append($"{GetDescriptionBreak()}");

        return description.ToString();
    }

    private bool PlayerInRangeOfMouse()
    {
        return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= 3;
    }

    private bool IsClear(Vector2 position)
    {
        Vector2 positionCheck = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        var colliders = Physics2D.OverlapBoxAll(positionCheck + new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0);

        foreach (Collider2D col in colliders)
        {
            if (col.TryGetComponent(out WorldObject clickable) || col.TryGetComponent(out Npc npc))
                return false;
        }

        return true;
    }
}
