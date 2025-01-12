using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerMainHand : MonoBehaviour
{
	[SerializeField] private bool _isMainHand;
	private SpriteRenderer _mainHandItemSR;
	private GameObject _mainHandArmGO;
	private bool _performingSwing;
	private Player _thisPlayer;
	private ItemSO _mainHandItemSO;

	private void Awake()
	{
		_mainHandArmGO = transform.GetChild(0).gameObject;
		_mainHandItemSR = transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
	}
	
	private void Start()
	{
		if(_thisPlayer == null)
		{
			_thisPlayer = transform.root.GetComponent<Player>();
			
			if(_isMainHand)
			{
				_thisPlayer.GetMainHandItemIndexNetworkVariable().OnValueChanged += MainHandItemIndexNetworkVariable_OnValueChanged;
			}
			else
			{
				_thisPlayer.GetOffHandItemIndexNetworkVariable().OnValueChanged += OffHandItemIndexNetworkVariable_OnValueChanged;
			}
			
		}
		
		Hide();
	}

	private void Update()
	{
		if(Player.LocalClientInstance.IsDead() || !Player.LocalClientInstance.IsOwner) return;
	
		if(!_performingSwing && (_isMainHand ? GameInput.Instance.GetPrimaryHeldDown() : GameInput.Instance.GetSecondaryHeldDown()) && _mainHandItemSO != null && _mainHandItemSO is MeleeItemSO && !Pointer.IsOverUI())
		{
			float angle = CalculateAngle();
			Debug.Log(_mainHandItemSO == null);
			// Changes pivot point position based on rotation.
			if ((angle < 45 && angle > 0) || (angle < 359.999 && angle > 315)) // East
			{
				SwingEast(0.35f);
			}
			else if (angle < 135 && angle > 45) // North
			{
				SwingNorth(0.35f);
			}
			else if (angle < 225 && angle > 135) // West
			{
				SwingWest(0.35f);
			}
			else if (angle < 315 && angle > 225) // South
			{
				SwingSouth(0.35f);
			}
		}
	}
	
	private float CalculateAngle()
	{
		Vector2 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position; // Calculate direction to target.
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // Calculate angle in degrees.
		
		// Clamps pivot to be positive.
		if (angle < 0)
		{
			angle = Mathf.Abs(angle);
			float leftover = 180 - angle;
			angle = 180 + leftover;
		}
		
		return angle;
	}

	private void MainHandItemIndexNetworkVariable_OnValueChanged(int previousValue, int newValue)
	{
		var item = GameManager.Instance.GetItemSOFromIndex(newValue);
		
		if(item is MeleeItemSO)
		{
			_mainHandItemSO = item;
		}
		else
		{
			_mainHandItemSO = null;
		}
		
		_mainHandItemSR.sprite = _mainHandItemSO != null ? _mainHandItemSO.UiDisplay : null;
	}
	
	private void OffHandItemIndexNetworkVariable_OnValueChanged(int previousValue, int newValue)
	{
		_mainHandItemSO = GameManager.Instance.GetItemSOFromIndex(newValue);
		
		_mainHandItemSR.sprite = _mainHandItemSO != null ? _mainHandItemSO.UiDisplay : null;
	}

	private void SwingNorth(float swingDuration)
	{
		if(_performingSwing) return;
	
		RotateZ(150, 30, swingDuration, true); // Swing clockwise
	}

	private void SwingSouth(float swingDuration)
	{
		if(_performingSwing) return;
	
		RotateZ(330, 210, swingDuration, false); // Swing counterclockwise
	}

	private void SwingEast(float swingDuration)
	{
		if(_performingSwing) return;
	
		RotateZ(60, 300, swingDuration, true); // Swing clockwise
	}

	private void SwingWest(float swingDuration)
	{
		if(_performingSwing) return;
	
		RotateZ(120, 240, swingDuration, false); // Swing counterclockwise
	}

	private void RotateZ(float startAngle, float endAngle, float duration, bool clockwise = true)
	{
		StartCoroutine(RotateCoroutine(startAngle, endAngle, duration, clockwise));
	}

	private IEnumerator RotateCoroutine(float startAngle, float endAngle, float duration, bool clockwise)
	{
		Show();
		
		_performingSwing = true;
	
		float elapsedTime = 0f;

		// Normalize angles to avoid issues with negative/overflow degrees
		startAngle = NormalizeAngle(startAngle);
		endAngle = NormalizeAngle(endAngle);

		// Adjust endAngle to ensure rotation goes in the desired direction
		if (clockwise && endAngle > startAngle)
		{
			startAngle += 360f;
		}
		else if (!clockwise && startAngle > endAngle)
		{
			endAngle += 360f;
		}

		Quaternion startRotation = Quaternion.Euler(0, 0, startAngle);
		Quaternion endRotation = Quaternion.Euler(0, 0, endAngle);

		while (elapsedTime < duration)
		{
			// Interpolate the rotation based on elapsed time
			transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsedTime / duration);
			elapsedTime += Time.deltaTime;
			yield return null; // Wait until the next frame
		}

		// Ensure the final rotation is set exactly
		transform.rotation = endRotation;
		
		Hide();
		
		yield return new WaitForSeconds(duration / 2f);
		
		_performingSwing = false;
	}

	private float NormalizeAngle(float angle)
	{
		// Normalize the angle to be between 0 and 360 degrees
		return (angle % 360 + 360) % 360;
	}

	private void Show()
	{
		_mainHandArmGO.SetActive(true);
	}

	private void Hide()
	{
		_mainHandArmGO.SetActive(false);
	}
	
	private void OnDestroy()
	{
		if(_isMainHand)
		{
			_thisPlayer.GetMainHandItemIndexNetworkVariable().OnValueChanged -= MainHandItemIndexNetworkVariable_OnValueChanged;
		}
		else
		{
			_thisPlayer.GetOffHandItemIndexNetworkVariable().OnValueChanged -= OffHandItemIndexNetworkVariable_OnValueChanged;
		}
	}
}