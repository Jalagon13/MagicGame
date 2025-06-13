using System;
using Unity.Netcode;

public struct SyncSpellData : IEquatable<SyncSpellData>, INetworkSerializable
{
    public int SpellItemId;
    public int ManaCost;
    public int Damage;
    public int Knockback;
    public float Speed;
    public float Lifetime;
    public float HasteMultiplier;
    public ulong CasterNetworkObjectId;
    public bool IsContinuousCast;
    public BiomeType SpawnBiome;

    public SyncSpellData(int spellItemId, int manaCost, int damage, int knockback, float speed, float lifetime, float hasteMultiplier, ulong casterNetworkObjectId, bool isContinuousCast, BiomeType spawnBiome)
    {
        SpellItemId = spellItemId;
        ManaCost = manaCost;
        Damage = damage;
        Knockback = knockback;
        Speed = speed;
        Lifetime = lifetime;
        HasteMultiplier = hasteMultiplier;
        CasterNetworkObjectId = casterNetworkObjectId;
        IsContinuousCast = isContinuousCast;
        SpawnBiome = spawnBiome;
    }

    public bool Equals(SyncSpellData other)
    {
        // Check if all primitive properties match
        if (SpellItemId != other.SpellItemId ||
            ManaCost != other.ManaCost ||
            Damage != other.Damage ||
            Knockback != other.Knockback ||
            Speed != other.Speed ||
            Lifetime != other.Lifetime ||
            HasteMultiplier != other.HasteMultiplier ||
            CasterNetworkObjectId != other.CasterNetworkObjectId ||
            IsContinuousCast != other.IsContinuousCast ||
            SpawnBiome != other.SpawnBiome)
        {
            return false;
        }

        return true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SpellItemId);
        serializer.SerializeValue(ref ManaCost);
        serializer.SerializeValue(ref Damage);
        serializer.SerializeValue(ref Knockback);
        serializer.SerializeValue(ref Speed);
        serializer.SerializeValue(ref Lifetime);
        serializer.SerializeValue(ref HasteMultiplier);
        serializer.SerializeValue(ref CasterNetworkObjectId);
        serializer.SerializeValue(ref IsContinuousCast);
        serializer.SerializeValue(ref SpawnBiome);
    }
}