using System;
using System.Collections.Generic;
using Unity.Netcode;

public struct SyncSpellData : IEquatable<SyncSpellData>, INetworkSerializable
{
    public int SpellItemId;
    public int ManaCost;
    public int Damage;
    public int Knockback;
    public int BounceCount;
    public int PierceCount;
    public float Speed;
    public float Lifetime;
    public float HasteMultiplier;
    public float Accuracy;
    public ulong CasterNetworkObjectId;
    public bool IsContinuousCast;
    public BiomeType SpawnBiome;
    public List<int> SpellMods; // Assuming SpellModItemSO is serializable or can be converted to int for network transmission

    public SyncSpellData(int spellItemId, int manaCost, int damage, int knockback, int bounceCount, int pierceCount, float speed, float lifetime, float hasteMultiplier, 
    float accuracy, ulong casterNetworkObjectId, bool isContinuousCast, BiomeType spawnBiome, List<SpellModItemSO> spellMods = null)
    {
        SpellItemId = spellItemId;
        ManaCost = manaCost;
        Damage = damage;
        Knockback = knockback;
        Speed = speed;
        Lifetime = lifetime;
        HasteMultiplier = hasteMultiplier;
        Accuracy = accuracy;
        CasterNetworkObjectId = casterNetworkObjectId;
        IsContinuousCast = isContinuousCast;
        SpawnBiome = spawnBiome;
        BounceCount = bounceCount;
        PierceCount = pierceCount;
        SpellMods = new List<int>();
        if (spellMods != null)
        {
            foreach (SpellModItemSO item in spellMods)
            {
                SpellMods.Add(GameManager.Instance.GetItemIdFromItemSO(item));
            }
        }
    }

    public bool Equals(SyncSpellData other)
    {
        // Check if all primitive properties match
        if (SpellItemId != other.SpellItemId ||
            ManaCost != other.ManaCost ||
            Damage != other.Damage ||
            Knockback != other.Knockback ||
            BounceCount != other.BounceCount ||
            PierceCount != other.PierceCount ||
            Speed != other.Speed ||
            Lifetime != other.Lifetime ||
            HasteMultiplier != other.HasteMultiplier ||
            Accuracy != other.Accuracy ||
            CasterNetworkObjectId != other.CasterNetworkObjectId ||
            IsContinuousCast != other.IsContinuousCast ||
            SpawnBiome != other.SpawnBiome)
        {
            return false;
        }

        if ((SpellMods == null && other.SpellMods != null) ||
            (SpellMods != null && other.SpellMods == null) ||
            (SpellMods != null && other.SpellMods != null && SpellMods.Count != other.SpellMods.Count))
        {
            return false;
        }

        if (SpellMods != null)
        {
            for (int i = 0; i < SpellMods.Count; i++)
            {
                if (SpellMods[i] != other.SpellMods[i])
                    return false;
            }
        }

        return true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SpellItemId);
        serializer.SerializeValue(ref ManaCost);
        serializer.SerializeValue(ref Damage);
        serializer.SerializeValue(ref Knockback);
        serializer.SerializeValue(ref BounceCount);
        serializer.SerializeValue(ref PierceCount);
        serializer.SerializeValue(ref Speed);
        serializer.SerializeValue(ref Lifetime);
        serializer.SerializeValue(ref HasteMultiplier);
        serializer.SerializeValue(ref Accuracy);
        serializer.SerializeValue(ref CasterNetworkObjectId);
        serializer.SerializeValue(ref IsContinuousCast);
        serializer.SerializeValue(ref SpawnBiome);

        int spellModsCount = SpellMods != null ? SpellMods.Count : 0;
        serializer.SerializeValue(ref spellModsCount);

        if (serializer.IsReader && SpellMods == null)
        {
            SpellMods = new List<int>(spellModsCount);
        }

        for (int i = 0; i < spellModsCount; i++)
        {
            int modValue = serializer.IsReader ? 0 : SpellMods[i];
            serializer.SerializeValue(ref modValue);

            if (serializer.IsReader)
            {
                SpellMods.Add(modValue);
            }
        }
    }
}