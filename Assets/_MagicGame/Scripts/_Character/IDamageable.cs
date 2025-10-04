using UnityEngine;

namespace ProjectWizard
{
    public interface IDamageable
    {
        void ReceiveHP(ServerCharacter inflicter, int HP, bool playKnockback, float knockback = -1);
        ulong NetworkObjectId { get; }
    }
}
