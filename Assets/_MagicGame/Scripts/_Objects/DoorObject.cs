using System;
using FMODUnity;
using UnityEngine;


namespace ProjectTinker
{
	public class DoorObject : ResourceObject
	{
		[SerializeField] private WorldInput _worldInput;
		[SerializeField] private float _doorOpenDistance = 2.75f; 
		[SerializeField] private Collider2D _localWallCollider;

		[Header("Sounds")]
		[SerializeField] private EventReference _openSound;
		[SerializeField] private EventReference _closeSound;

		[Header("Door Sprites")]
		[SerializeField] private SpriteRenderer _doorSr;
		[SerializeField] private Sprite _meridianClosedSprite, _meridianOpenSprite, _latitudeClosedEastSprite, _latitudeClosedWestSprite, _latitudeOpenSprite; // Meridian is north to south, latitude is west to east
	
		private Sprite _openSprite, _closeSprite;
		private bool _isOpen;
	
		private void Start()
		{
			GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if(other.gameObject.layer == 8 && !_isOpen)
			{
				OpenDoor();
				ResourceManager.Instance.SetDoorOpenStateServerRpc(Vector2Int.FloorToInt(transform.position), Player.Instance.CurrentBiome.Value, true);
			}
		}

		private void OnTriggerExit2D(Collider2D other)
		{
			if (other.gameObject.layer == 8 && _isOpen)
			{
				CloseDoor();
				ResourceManager.Instance.SetDoorOpenStateServerRpc(Vector2Int.FloorToInt(transform.position), Player.Instance.CurrentBiome.Value, false);
			}
		}

		private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
		{
			var centerOfChestPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);
			var playerInRange = Vector2.Distance(Player.Instance.transform.position, centerOfChestPosition) <= _doorOpenDistance;

			if (_worldInput.IsMouseOverIndputDetector() && playerInRange)
			{
				_isOpen = !_isOpen;
		
				ResourceManager.Instance.SetDoorOpenStateServerRpc(Vector2Int.FloorToInt(transform.position), Player.Instance.CurrentBiome.Value, _isOpen);
				HandlePathfinding();
			}
		}

		public override void SetOrientation(CardinalDirection orientation)
	    {
	        base.SetOrientation(orientation);
        
			SetOrientationSprites();
		}

	    public void InitializeOpenState(bool isOpen)
		{
			SetIsOpen(isOpen, false);
			SetOrientationSprites();
		
		
		}
	
		private void SetOrientationSprites()
		{
			if (_orientation == CardinalDirection.North || _orientation == CardinalDirection.South)
			{
				_openSprite = _meridianOpenSprite;
				_closeSprite = _meridianClosedSprite;
			}
			else
			{
				_openSprite = _latitudeOpenSprite;
				_closeSprite = _orientation == CardinalDirection.East ? _latitudeClosedEastSprite : _latitudeClosedWestSprite;
			}

			_doorSr.sprite = _isOpen ? _openSprite : _closeSprite;
		}

		public void SetIsOpen(bool isOpen, bool playSound = true)
		{
			_isOpen = isOpen;
	
			if(_isOpen)
			{
				OpenDoor(playSound);
			}
			else
			{
				CloseDoor(playSound);
			}
		}
	
		private void OpenDoor(bool playSound = false)
		{
			_doorSr.sprite = _openSprite;
			_localWallCollider.gameObject.SetActive(false);

			HandlePathfinding();

			if (playSound)
			{
				SoundManager.Instance.PlayOneShot(_openSound, transform.position);
				Lightmap.Instance.UpdateLightMap();
			}
		}
	
		private void CloseDoor(bool playSound = false)
		{
			_doorSr.sprite = _closeSprite;
			_localWallCollider.gameObject.SetActive(true);

			HandlePathfinding();

			if (playSound)
			{
				SoundManager.Instance.PlayOneShot(_closeSound, transform.position);
				Lightmap.Instance.UpdateLightMap();
			}
		}
	
		private void HandlePathfinding()
		{
			if (_isOpen)
			{
				Pathfinding.Instance.RemovePathfindingfWallTileServerRpc(Vector2Int.FloorToInt(transform.position), Player.Instance.CurrentBiome.Value);
			}
			else
			{
				Pathfinding.Instance.AddPfWallTileServerRpc(Vector2Int.FloorToInt(transform.position), Player.Instance.CurrentBiome.Value);
			}
		}
	
		private void OnDestroy()
		{
			Pathfinding.Instance.RemovePathfindingfWallTileServerRpc(Vector2Int.FloorToInt(transform.position), Player.Instance.CurrentBiome.Value);
	
			GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
		}
	}

}