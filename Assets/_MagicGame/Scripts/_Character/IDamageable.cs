using UnityEngine;

namespace ProjectTinker
{
    public interface IDamageable
    {
        void ReceiveHP(ServerCharacter inflicter, int HP, bool playKnockback, float knockback = -1);
        ulong NetworkObjectId { get; }
    }
}
