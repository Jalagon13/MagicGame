using System;
using UnityEngine;

public interface IHasHealth
{
	public NetworkHealthState HealthNetworkVariable { get; }
	public BiomeType Biome { get; }
	public float IFrameLength { get; } // Read-only property
	
	public void ApplyDamage(int damage, Vector2 damagerPosition, int knockbackForce = 0);
}
