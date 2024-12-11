using System;
using UnityEngine;

public interface IHasHealth
{
    public event EventHandler<OnHealthUpdatedEventArgs> OnHealthUpdated;
    public class OnHealthUpdatedEventArgs : EventArgs 
    {
        public int PreviousValue;
        public int NewValue;
        public int MaxValue;
    }
	
    public void ApplyDamage(int damage, Vector2 damagerPosition);
}
