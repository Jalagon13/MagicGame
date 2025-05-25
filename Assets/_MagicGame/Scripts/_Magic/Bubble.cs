using UnityEngine;

public class Bubble : Spell
{
    [field: SerializeField] public float OrbitSpeed { get; private set; }
    [field: SerializeField] public float DistanceFromPlayer { get; private set; }

    private Transform _playerToOrbit;

    protected override void OnSpellSpawned()
    {
        _playerToOrbit = Player.LocalClientInstance.transform;
    }

    protected override void OnExecuteSpellStart()
    {
        // Add logic here if needed
    }

    protected override void OnSpellEnd()
    {
        // Add logic here if needed
    }

    protected override void OnSpellCanceled()
    {
        // Add logic here if needed
    }
}
