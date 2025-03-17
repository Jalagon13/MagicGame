using UnityEngine;
using System;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }
    public event EventHandler<GoldEventArgs> OnGoldChanged;
    public class GoldEventArgs : EventArgs
    {
        public int CurrentGold { get; }

        public GoldEventArgs(int currentGold)
        {
            CurrentGold = currentGold;
        }
    }
    
    [field: SerializeField] public int StartingGold { get; private set; } = 0;
    
    private static int _currentGold = 0;

    private void Awake()
    {
        Instance = this;

        _currentGold = StartingGold;
    }
    
    public void AddGold(int amount)
    {
        _currentGold += amount;
        OnGoldChanged?.Invoke(this, new GoldEventArgs(_currentGold));
    }
    
    public void RemoveGold(int amount)
    {
        _currentGold -= amount;
        _currentGold = Mathf.Max(0, _currentGold);
        OnGoldChanged?.Invoke(this, new GoldEventArgs(_currentGold));
    }
}
