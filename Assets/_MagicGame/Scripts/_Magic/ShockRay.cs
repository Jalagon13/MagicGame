using UnityEngine;

public class ShockRay : Spell
{
    protected override void OnOwnerSpellSpawned()
    {
        Debug.Log($"Spawned Shockray Spell. Client owner: {OwnerClientId}");
        
        
    }

    protected override void OnOwnerExecuteSpellStart()
    {
        Debug.Log($"Executing Shockray Spell. Owner: {NetworkObject.OwnerClientId}");
        
        
    }

    protected override void OnOwnerSpellEnd()
    {
        Debug.Log($"Ending Shockray Spell");

        base.OnOwnerSpellEnd();
    }

    public override void OnOwnerSpellCanceled()
    {
        Debug.Log($"Cancelling Shockray Spell");

        base.OnOwnerSpellCanceled();
    }

    private void FixedUpdate()
    {
        if(IsOwner && IsStarted.Value)
        {
            transform.position = ActionManager.MouseWorldPosition;
        }
    }
}
