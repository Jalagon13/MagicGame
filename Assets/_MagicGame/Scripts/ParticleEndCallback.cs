using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleEndCallback : MonoBehaviour
{
    // Unity automatically calls this if Stop Action = Callback
    private void OnParticleSystemStopped()
    {
        Debug.Log($"On Particlesystemstopped");
        SoundManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerDamaged, transform.position);
    }
}