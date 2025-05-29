using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ServerActionPlayer
{
    private ServerCharacter _serverCharacter;
    
    private List<(ActionSO action, Timer timer)> _actionList = new List<(ActionSO, Timer)>();
    
    public ServerActionPlayer(ServerCharacter serverCharacter)
    {
        _serverCharacter = serverCharacter;
    }
    
    public void PlayAction(ActionSO actionSO)
    {
        var timer = new Timer(actionSO.Config.DurationSeconds);
        _actionList.Add((actionSO, timer));
        actionSO.OnStartServer(_serverCharacter);
    }
    
    public void OnUpdateServerActions()
    {
        for (int i = _actionList.Count - 1; i >= 0; i--)
        {
            var (runningActionSO, timer) = _actionList[i];
            timer.Tick(Time.deltaTime);

            if (!UpdateAction(runningActionSO, timer))
            {
                runningActionSO.End(_serverCharacter);
                _actionList.RemoveAt(i);
            }
        }
    }

    private bool UpdateAction(ActionSO action, Timer timer)
    {
        bool keepGoing = action.OnUpdateServer(_serverCharacter);
        bool expirable = action.Config.DurationSeconds > 0;
        bool timeExpired = expirable && timer.RemainingSeconds <= 0f;

        return keepGoing && !timeExpired;
    }
}
