using UnityEngine;
using Unity.Netcode;

namespace ProjectTinker
{
    public enum LifeState
    {
        Alive,
        IFrame,
        Dead
    }

    public class NetworkLifeState : NetworkBehaviour
    {
        [SerializeField]
        private NetworkVariable<LifeState> _lifeState = new NetworkVariable<LifeState>();

        public NetworkVariable<LifeState> LifeState => _lifeState;
    }
}

