using UnityEngine;

public interface IDamageable
{
    void ReceiveHP(ServerCharacter inflicter, int HP);
    ulong NetworkObjectId { get; }
}
