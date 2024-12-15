using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class CastArmPivot : NetworkBehaviour
{
    public event EventHandler<OnCastingArmDirectionChangedEventArgs> OnCastingArmDirectionChanged;
    public class OnCastingArmDirectionChangedEventArgs : EventArgs
    {
        public CardinalDirection Direction;
    }

    [SerializeField] private Transform _armPivot;
	
    [FoldoutGroup("Pivots")]
    [SerializeField] private Transform _rightFacePivot;
    [FoldoutGroup("Pivots")]
    [SerializeField] private Transform _backFacePivot;
    [FoldoutGroup("Pivots")]
    [SerializeField] private Transform _leftFacePivot;
    [FoldoutGroup("Pivots")]
    [SerializeField] private Transform _frontFacePivot;
	
    private SortingGroup _sortingGroup;
    private NetworkVariable<float> _angleNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private Player _thisPlayer;
	
    public CardinalDirection CastArmDirection { get; private set;}
	
    private void Awake()
    {
        _sortingGroup = GetComponent<SortingGroup>();
        _thisPlayer = transform.root.GetComponent<Player>();
    }
	
    private void OnEnable()
    {
        WandUpdate();
    }
	
    private void FixedUpdate()
    {
        WandUpdate();
    }
	
    private void WandUpdate()
    {
        if(IsOwner)
        {
            // Mouse Angle on owner, handle rotation outside of this
            // Handles rotation of cast arm.
            Vector3 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position; // Calculate direction to target.
            _angleNetworkVariable.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // Calculate angle in degrees.
        }
		
        HandleRotation();
        HandleSortingOrder();
    }
	
    private void HandleRotation()
    {
        float angle = _angleNetworkVariable.Value;
		
        // Clamps pivot to be positive.
        if (angle < 0)
        {
            angle = Mathf.Abs(angle);
            float leftover = 180 - angle;
            angle = 180 + leftover;
        }
		
        // Changes pivot point position based on rotation.
        if ((angle < 45 && angle > 0) || (angle < 359.999 && angle > 315))
        {
            // East.
            transform.SetPositionAndRotation(_rightFacePivot.position, Quaternion.AngleAxis(angle, Vector3.forward));
            CastArmDirection = CardinalDirection.East;
        }
        else if (angle < 135 && angle > 45)
        {
            // North.
            transform.SetPositionAndRotation(_backFacePivot.position, Quaternion.AngleAxis(angle, Vector3.forward));
            CastArmDirection = CardinalDirection.North;
        }
        else if (angle < 225 && angle > 135)
        {
            // West.
            transform.SetPositionAndRotation(_leftFacePivot.position, Quaternion.AngleAxis(angle, Vector3.forward));
            CastArmDirection = CardinalDirection.West;
        }
        else if (angle < 315 && angle > 225)
        {
            // South.
            transform.SetPositionAndRotation(_frontFacePivot.position, Quaternion.AngleAxis(angle, Vector3.forward));
            CastArmDirection = CardinalDirection.South;
        }
		
        if(_thisPlayer.GetComponent<PlayerStateMachine>().MovingDirection != CastArmDirection)
        {
            OnCastingArmDirectionChanged?.Invoke(this, new OnCastingArmDirectionChangedEventArgs
            {
                Direction = CastArmDirection
            });
			
            // Flip arm sprite correctly depending on direction
            _armPivot.localScale = new(1, CastArmDirection == CardinalDirection.West ? -1 : 1, 0);
        }
    }

    private void HandleSortingOrder()
    {
        Vector3 localRotationAngles = transform.localEulerAngles;
        float angleZ = localRotationAngles.z;
		
        if (angleZ < 0) angleZ += 360;

        // Check which cardinal direction the angle falls into
        if ((angleZ >= 315 && angleZ <= 360) || (angleZ >= 0 && angleZ < 45))
        {
            // Debug.Log("Facing East");
            _sortingGroup.sortingOrder = 1;
            CastArmDirection = CardinalDirection.East;
        }
        else if (angleZ >= 45 && angleZ < 135)
        {
            // Debug.Log("Facing North");
            _sortingGroup.sortingOrder = -1;
            CastArmDirection = CardinalDirection.North;
        }
        else if (angleZ >= 135 && angleZ < 225)
        {
            // Debug.Log("Facing West");
            _sortingGroup.sortingOrder = 1;
            CastArmDirection = CardinalDirection.West;
        }
        else if (angleZ >= 225 && angleZ < 315)
        {
            // Debug.Log("Facing South");
            _sortingGroup.sortingOrder = 1;
            CastArmDirection = CardinalDirection.South;
        }
    }
}
