using UnityEngine;

public class Bubble : Spell
{
    [field: SerializeField] public float OrbitSpeed { get; private set; }
    [field: SerializeField] public float DistanceFromPlayer { get; private set; }

    private Transform _playerToOrbit;

    protected override void OnOwnerSpellSpawned()
    {
        _playerToOrbit = Player.LocalClientInstance.transform;
    }

    protected override void OnOwnerExecuteSpellStart()
    {

    }

    protected override void OnOwnerSpellEnd()
    {
        

        base.OnOwnerSpellEnd();
    }

    public override void OnOwnerSpellCanceled()
    {
        
        
        base.OnOwnerSpellCanceled();
    }
}
