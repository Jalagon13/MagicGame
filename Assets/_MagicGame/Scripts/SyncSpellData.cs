using System;
using Unity.Netcode;

public struct SyncSpellData : IEquatable<SyncSpellData>, INetworkSerializable
{
    public int SpellIndex;
    public int ManaCost;
    public int Damage;
    public int Knockback;
    public int WandSlotIndex;
    public float Speed;
    public float Lifetime;
    public float HasteMultiplier;
    public ulong SpellId;
    public ulong CasterNetworkObjectId;
    public ulong InventorySlotId;
    public bool DespawnIfFocusSlotChanged;
    public bool IsContinuousCast;
    public BiomeType SpawnBiome;

    public SyncSpellData(int spellIndex, int manaCost, int damage, int knockback, int wandSlotIndex, float speed, float lifetime, float hasteMultiplier, ulong spellId, ulong casterNetworkObjectId, ulong inventorySlotId, bool despawnIfFocusSlotChanged, bool isContinuousCast, BiomeType spawnBiome)
    {
        SpellIndex = spellIndex;
        ManaCost = manaCost;
        Damage = damage;
        Knockback = knockback;
        WandSlotIndex = wandSlotIndex;
        Speed = speed;
        Lifetime = lifetime;
        HasteMultiplier = hasteMultiplier;
        SpellId = spellId;
        CasterNetworkObjectId = casterNetworkObjectId;
        InventorySlotId = inventorySlotId;
        DespawnIfFocusSlotChanged = despawnIfFocusSlotChanged;
        IsContinuousCast = isContinuousCast;
        SpawnBiome = spawnBiome;
    }

    public bool Equals(SyncSpellData other)
    {
        // Check if all primitive properties match
        if (SpellIndex != other.SpellIndex ||
            ManaCost != other.ManaCost ||
            Damage != other.Damage ||
            Knockback != other.Knockback ||
            WandSlotIndex != other.WandSlotIndex ||
            Speed != other.Speed ||
            Lifetime != other.Lifetime ||
            HasteMultiplier != other.HasteMultiplier ||
            SpellId != other.SpellId ||
            CasterNetworkObjectId != other.CasterNetworkObjectId ||
            InventorySlotId != other.InventorySlotId ||
            DespawnIfFocusSlotChanged != other.DespawnIfFocusSlotChanged ||
            IsContinuousCast != other.IsContinuousCast ||
            SpawnBiome != other.SpawnBiome)
        {
            return false;
        }

        return true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SpellIndex);
        serializer.SerializeValue(ref ManaCost);
        serializer.SerializeValue(ref Damage);
        serializer.SerializeValue(ref Knockback);
        serializer.SerializeValue(ref WandSlotIndex);
        serializer.SerializeValue(ref Speed);
        serializer.SerializeValue(ref Lifetime);
        serializer.SerializeValue(ref HasteMultiplier);
        serializer.SerializeValue(ref SpellId);
        serializer.SerializeValue(ref CasterNetworkObjectId);
        serializer.SerializeValue(ref InventorySlotId);
        serializer.SerializeValue(ref DespawnIfFocusSlotChanged);
        serializer.SerializeValue(ref IsContinuousCast);
        serializer.SerializeValue(ref SpawnBiome);
    }
}