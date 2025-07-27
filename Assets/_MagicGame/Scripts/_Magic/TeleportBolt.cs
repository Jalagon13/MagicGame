using System.Collections;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class TeleportBolt : ProjectileSpell
{
    [field: Header("Teleport Bolt")]
    [field: SerializeField] public ParticleSystem TeleportParticles { get; private set; }
    [field: SerializeField] public ParticleSystem Trail { get; private set; }
    [field: SerializeField] public EventReference TeleportSound { get; private set; }

    protected override Vector2 CalculateVelocity(Vector2 currentVelocity)
    {
        return currentVelocity; // Constant velocity for teleport bolt, no decay
    }

    public override void OnClientSpellStop(ClientSpell clientSpell)
    {
        Player playerWhoShotIt = NetworkManager.Singleton.SpawnManager.SpawnedObjects[SpellData.Value.CasterNetworkObjectId].GetComponent<Player>();
        
        SoundManager.Instance.PlayOneShot(TeleportSound, transform.position);
        GameObject vfx = Instantiate(TeleportParticles.gameObject, playerWhoShotIt.transform.position, Quaternion.identity);
        vfx.GetComponent<ParticleSystem>().Play();

        if (Trail != null)
        {
            Trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (IsOwner)
        {
            Player.Instance.gameObject.transform.position = transform.position;
        }
    }
}