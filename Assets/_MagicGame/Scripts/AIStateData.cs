using System;
using Unity.Netcode;
using UnityEngine;

public struct AIStateData : IEquatable<AIStateData>, INetworkSerializable
{
    public AIState CurrentState;
    public int SpellId;

    public AIStateData(AIState currentState)
    {
        CurrentState = currentState;
        SpellId = 0;
    }

    public AIStateData(AIState currentState, int spellId)
    {
        CurrentState = currentState;
        SpellId = spellId;
    }
    
    public bool Equals(AIStateData other)
    {
        return CurrentState == other.CurrentState && SpellId == other.SpellId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CurrentState);
        serializer.SerializeValue(ref SpellId);
    }
}