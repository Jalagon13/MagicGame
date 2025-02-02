using System;
using UnityEngine;

public class DoorObject : ResourceObject
{
	[SerializeField] private WorldInput _worldInput;
	[SerializeField] private float _doorOpenDistance = 2.75f; 
	[SerializeField] private Sprite _openSprite, _closeSprite;
	[SerializeField] private SpriteRenderer _doorSr;
	[SerializeField] private Collider2D _localWallCollider;
	
	private bool _isOpen;
	
	protected override void Start()
	{
		base.Start();
		
		GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
	}
	
	public void InitializeOpenState(bool isOpen)
	{
		SetIsOpen(isOpen, false);
	}

	private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
	{
		var centerOfChestPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);
		var playerInRange = Vector2.Distance(Player.LocalClientInstance.transform.position, centerOfChestPosition) <= _doorOpenDistance;
		
		if(_worldInput.IsMouseOverIndputDetector() && playerInRange)
		{
			ObjectManager.Instance.ToggleDoor(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.CurrentBiome.Value);
		}
	}
	
	public void SetIsOpen(bool isOpen, bool playSound = true)
	{
		_isOpen = isOpen;
	
		if(_isOpen)
		{
			OpenDoor();
		}
		else
		{
			CloseDoor();
		}
	}
	
	private void OpenDoor()
	{
		_doorSr.sprite = _openSprite;
		_localWallCollider.gameObject.SetActive(false);
		
		Pathfinding.Instance.RemovePfWallTile(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.CurrentBiome.Value);
		
		Debug.Log($"Open");
	}
	
	private void CloseDoor()
	{
		_doorSr.sprite = _closeSprite;
		_localWallCollider.gameObject.SetActive(true);
		
		Pathfinding.Instance.AddPfWallTile(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.CurrentBiome.Value);
		
		Debug.Log($"Close");
	}
	
	private void OnDestroy()
	{
		Pathfinding.Instance.RemovePfWallTile(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.CurrentBiome.Value);
	
		GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
	}
}
