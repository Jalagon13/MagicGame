using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 public class Timer
{
    public event EventHandler OnTimerEnd;
    public bool IsPaused = false;

    private float _remainingSeconds;
    private readonly float _duration;

    public float RemainingSeconds
    {
        get { return _remainingSeconds; }
        set
        {
            value = Mathf.Max(value, 0f);
            _remainingSeconds = value;
        }
    }
	
    public float ElapsedSeconds
    {
        get { return _duration - _remainingSeconds; }
    }
    
    public float Duration
    {
        get { return _duration; }
    }
	
    public void Reset()
    {
        _remainingSeconds = _duration;
    }

    public Timer(float duration)
    {
        _duration = duration;
        _remainingSeconds = duration;
        IsPaused = false;
    }

    public void Tick(float deltaTime)
    {
        if (_remainingSeconds <= 0f || IsPaused) return;

        _remainingSeconds -= deltaTime;

        CheckForTimerEnd();
    }

    private void CheckForTimerEnd()
    {
        if (_remainingSeconds > 0f) return;

        _remainingSeconds = 0f;

        OnTimerEnd?.Invoke(this, EventArgs.Empty);
    }
}

