using System;
using Unity.Netcode;
using UnityEngine;

public class DamageReceiver : NetworkBehaviour, IDamageable
{
    [SerializeField] private NetworkLifeState _lifeState;

    public event EventHandler<DamageReceivedEventArgs> DamagedReceived;
    public class DamageReceivedEventArgs : EventArgs
    {
        public ServerCharacter Inflicter;
        public int HP;
        public DamageReceivedEventArgs(ServerCharacter inflicter, int hp)
        {
            Inflicter = inflicter;
            HP = hp;
        }
    }

    public void ReceiveHP(ServerCharacter inflicter, int hp)
    {
        if (IsDamageable())
        {
            DamagedReceived?.Invoke(this, new DamageReceivedEventArgs(inflicter, hp));
        }
    }

    public bool IsDamageable()
    {
        return _lifeState.LifeState.Value == LifeState.Alive;
    }
}
