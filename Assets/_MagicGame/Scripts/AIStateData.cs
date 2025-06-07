using System;
using Unity.Netcode;
using UnityEngine;

public struct AIStateData : IEquatable<AIStateData>, INetworkSerializable
{
    public AIState CurrentState;
    public int Amount; // Can be anything, spell id, damage, speed, whatever the fuck

    public AIStateData(AIState currentState)
    {
        CurrentState = currentState;
        Amount = 0;
    }

    public AIStateData(AIState currentState, int spellId)
    {
        CurrentState = currentState;
        Amount = spellId;
    }
    
    public bool Equals(AIStateData other)
    {
        return CurrentState == other.CurrentState && Amount == other.Amount;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CurrentState);
        serializer.SerializeValue(ref Amount);
    }
}