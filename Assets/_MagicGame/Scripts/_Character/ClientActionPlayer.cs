using System.Collections.Generic;
using UnityEngine;

public class ClientActionPlayer
{
    private ClientCharacter _clientCharacter;
    public ClientCharacter ClientCharacter => _clientCharacter;

    private List<(ActionSO action, Timer timer)> _actionList = new List<(ActionSO, Timer)>();

    public ClientActionPlayer(ClientCharacter clientCharacter)
    {
        _clientCharacter = clientCharacter;
    }

    public void PlayAction(ActionSO actionSO)
    {
        var timer = new Timer(actionSO.Config.DurationSeconds);
        _actionList.Add((actionSO, timer));
        actionSO.OnStartClient(_clientCharacter);
    }

    public void OnUpdateServerActions()
    {
        for (int i = _actionList.Count - 1; i >= 0; i--)
        {
            var (runningActionSO, timer) = _actionList[i];
            timer.Tick(Time.deltaTime);

            if (!UpdateAction(runningActionSO, timer))
            {
                runningActionSO.EndClient(_clientCharacter);
                _actionList.RemoveAt(i);
            }
        }
    }

    private bool UpdateAction(ActionSO action, Timer timer)
    {
        bool keepGoing = action.OnUpdateClient(_clientCharacter);
        bool expirable = action.Config.DurationSeconds > 0;
        bool timeExpired = expirable && timer.RemainingSeconds <= 0f;

        return keepGoing && !timeExpired;
    }
}
