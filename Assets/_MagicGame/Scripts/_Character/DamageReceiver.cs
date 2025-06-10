using System;
using Unity.Netcode;
using UnityEngine;

public class DamageReceiver : NetworkBehaviour, IDamageable
{
    [SerializeField] private NetworkLifeState _lifeState;

    public event EventHandler<DamageReceivedEventArgs> HpReceived;
    public class DamageReceivedEventArgs : EventArgs
    {
        public ServerCharacter Inflicter;
        public int HP;
        public bool PlayKnockback;
        public float KnockbackForce;
        public DamageReceivedEventArgs(ServerCharacter inflicter, int hp, bool playKnockback, float knockbackForce = -1)
        {
            Inflicter = inflicter;
            HP = hp;
            PlayKnockback = playKnockback;
            KnockbackForce = knockbackForce;
        }
    }

    public void ReceiveHP(ServerCharacter inflicter, int hp, bool playKnockback, float knockback = -1)
    {
        if (IsAlive())
        {
            HpReceived?.Invoke(this, new DamageReceivedEventArgs(inflicter, hp, playKnockback, knockback));
        }
    }

    public bool IsAlive()
    {
        return _lifeState.LifeState.Value != LifeState.Dead;
    }
}
