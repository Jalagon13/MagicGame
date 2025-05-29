using UnityEngine;

public interface IAIBrain
{
    void UpdateAI();
    void ReceiveHP(ServerCharacter inflicter, int amount);
}
