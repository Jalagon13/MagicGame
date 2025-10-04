using System;
using Unity.Netcode;
using UnityEngine;

namespace ProjectWizard
{
    public class DamageReceiver : NetworkBehaviour, IDamageable
    {
        [SerializeField]
        private NetworkLifeState _lifeState;

        public event EventHandler<HpReceivedEventArgs> HpReceived;
        public class HpReceivedEventArgs : EventArgs
        {
            public ServerCharacter Inflicter;
            public int HpReceived;
            public bool PlayKnockback;
            public float KnockbackForce;
            public HpReceivedEventArgs(ServerCharacter inflicter, int hpReceived, bool playKnockback, float knockbackForce = -1)
            {
                Inflicter = inflicter;
                HpReceived = hpReceived;
                PlayKnockback = playKnockback;
                KnockbackForce = knockbackForce;
            }
        }

        public void ReceiveHP(ServerCharacter inflicter, int hp, bool playKnockback, float knockback = -1)
        {
            // Send request to server
            ReceiveHpRequestServerRpc(inflicter.NetworkObjectId, hp, playKnockback, knockback);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void ReceiveHpRequestServerRpc(ulong networkObjectId, int hp, bool playKnockback, float knockback)
        {
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject inflicterNet);
            ServerCharacter inflicterServerCharacter = inflicterNet != null ? inflicterNet.GetComponent<ServerCharacter>() : null;

            if (IsAlive())
            {
                HpReceived?.Invoke(this, new HpReceivedEventArgs(inflicterServerCharacter, hp, playKnockback, knockback));
            }
        }

        public bool IsAlive()
        {
            return _lifeState.LifeState.Value != LifeState.Dead;
        }
    }
}
