using UnityEngine;

public abstract class ActionSO : ScriptableObject
{
    public ActionConfig Config;

    public abstract bool OnStartServer(ServerCharacter serverCharacter);
    
    public abstract bool OnUpdateServer(ServerCharacter serverCharacter);
    
    public virtual void EndServer(ServerCharacter serverCharacter)
    {
        CancelServer(serverCharacter);
    }
    
    public virtual void CancelServer(ServerCharacter serverCharacter) { }
    
    public virtual void OnStartClient(ClientCharacter clientCharacter) { }
    public virtual bool OnUpdateClient(ClientCharacter clientCharacter) 
    {
        return true;
    }
    
    public virtual void EndClient(ClientCharacter clientCharacter) 
    {
        CancelClient(clientCharacter);
    }
    
    public virtual void CancelClient(ClientCharacter clientCharacter) { }
}
