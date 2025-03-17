using System.Text;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Assertions;

[CreateAssetMenu(fileName = "New Npc Item", menuName = "Create Item/New NPC Item")]
public class NpcItemSO : ItemSO
{
    // NTFS: Make it so this item can spawn any NPC you choose it to spawn in the inspector
    public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
    {
        Vector2 pos = ActionManager.MouseWorldPosition;

        if (IsClear(pos) && PlayerInRangeOfMouse())
        {
            Vector2 spawnPosition = new(Mathf.FloorToInt(pos.x) + 0.5f, Mathf.FloorToInt(pos.y) + 0.5f);
            NpcManager.Instance.SpawnTargetDummyServerRpc(spawnPosition);
            InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
        }

        return _baseActionCooldown;
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
