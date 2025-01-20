using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

// Base class for every "physical" asset in the world
[SelectionBase]
public class WorldObject : MonoBehaviour
{	
	[SerializeField] private string _worldObjectName;
	[field: SerializeField] public EventReference ResourceHit { get; private set; }
	[field: SerializeField] public EventReference ResourceDestroyed { get; private set; }
	private bool _placedDownByPlayer;
	
	public void SetPlacedDownByPlayer(bool var)
	{
		_placedDownByPlayer = var;
	}
	
	public bool IsPlacedDownByPlayer()
	{
		return _placedDownByPlayer;
	}
	
	public string GetWorldObjectName()
	{
		return _worldObjectName;
	}
	
	public void DestroySelf()
	{
		Destroy(gameObject);
	}
}
