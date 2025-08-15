using System;
using System.Collections.Generic;
using Unity.Netcode;

public struct SyncSpellData : IEquatable<SyncSpellData>, INetworkSerializable
{
    public int SpellItemId;
    public int Damage;
    public int Knockback;
    public int BounceCount;
    public int PierceCount;
    public float Speed;
    public float Lifetime;
    public float HasteMultiplier;
    public float Scatter;
    public ulong CasterNetworkObjectId;
    public bool OnlyContinueAfterSpellEnds;
    public BiomeType SpawnBiome;

    public SyncSpellData(int spellItemId, int damage, int knockback, int bounceCount, int pierceCount, float speed, float lifetime, float hasteMultiplier, 
    float scatter, ulong casterNetworkObjectId, bool onlyContinueAfterSpellEnds, BiomeType spawnBiome)
    {
        SpellItemId = spellItemId;
        Damage = damage;
        Knockback = knockback;
        Speed = speed;
        Lifetime = lifetime;
        HasteMultiplier = hasteMultiplier;
        Scatter = scatter;
        CasterNetworkObjectId = casterNetworkObjectId;
        OnlyContinueAfterSpellEnds = onlyContinueAfterSpellEnds;
        SpawnBiome = spawnBiome;
        BounceCount = bounceCount;
        PierceCount = pierceCount;
    }

    public bool Equals(SyncSpellData other)
    {
        // Check if all primitive properties match
        if (SpellItemId != other.SpellItemId ||
            Damage != other.Damage ||
            Knockback != other.Knockback ||
            BounceCount != other.BounceCount ||
            PierceCount != other.PierceCount ||
            Speed != other.Speed ||
            Lifetime != other.Lifetime ||
            HasteMultiplier != other.HasteMultiplier ||
            Scatter != other.Scatter ||
            CasterNetworkObjectId != other.CasterNetworkObjectId ||
            SpawnBiome != other.SpawnBiome)
        {
            return false;
        }

        if (OnlyContinueAfterSpellEnds != other.OnlyContinueAfterSpellEnds)
            return false;

        return true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SpellItemId);
        serializer.SerializeValue(ref Damage);
        serializer.SerializeValue(ref Knockback);
        serializer.SerializeValue(ref BounceCount);
        serializer.SerializeValue(ref PierceCount);
        serializer.SerializeValue(ref Speed);
        serializer.SerializeValue(ref Lifetime);
        serializer.SerializeValue(ref HasteMultiplier);
        serializer.SerializeValue(ref Scatter);
        serializer.SerializeValue(ref CasterNetworkObjectId);
        serializer.SerializeValue(ref SpawnBiome);
        serializer.SerializeValue(ref OnlyContinueAfterSpellEnds);
    }
}