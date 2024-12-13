using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SwingArmPivot : MonoBehaviour
{
    [SerializeField] private Transform _armPivot;

    [Header("Transforms for pivot points")]
    [SerializeField] private Transform _eastPivot;
    [SerializeField] private Transform _northPivot;
    [SerializeField] private Transform _westPivot;
    [SerializeField] private Transform _southPivot;
	
    private SortingGroup _sortingGroup;
    private SwingController _swingController;
	
    private void Awake()
    {
        _sortingGroup = GetComponent<SortingGroup>();
        _swingController = transform.parent.GetComponent<SwingController>();
        _swingController.OnSwingStart += Swing_OnStart;
        _swingController.OnSwingEnd += Swing_OnEnd;
    }

    private void Swing_OnStart(object sender, SwingController.SwingEventArgs e)
    {
        SetPivot(e.SwingDirection);
		
        Show();
    }

    private void Swing_OnEnd(object sender, SwingController.SwingEventArgs e)
    {
        Hide();
    }

    private void SetPivot(CardinalDirection direction)
    {
        _sortingGroup.sortingOrder = 1;
        _armPivot.localScale = new(1, direction == CardinalDirection.West ? -1 : 1, 0);
        switch(direction)
        {
            case CardinalDirection.North:
                transform.SetPositionAndRotation(_northPivot.position, Quaternion.identity);
                // _direction = CardinalDirection.North;
				
                // If facing north, set this behind player sprite.
                _sortingGroup.sortingOrder = -1;
            break;
			
            case CardinalDirection.South:
                transform.SetPositionAndRotation(_southPivot.position, Quaternion.identity);
                // _direction = CardinalDirection.South;
            break;
			
            case CardinalDirection.East:
                transform.SetPositionAndRotation(_eastPivot.position, Quaternion.identity);
                // _direction = CardinalDirection.East;
            break;
			
            case CardinalDirection.West:
                transform.SetPositionAndRotation(_westPivot.position, Quaternion.identity);
                // _direction = CardinalDirection.West;
            break;
        }
    }
	
    private void Show()
    {
        gameObject.SetActive(true);
    }
	
    private void Hide()
    {
        gameObject.SetActive(false);
    }
	
    private void OnDestroy()
    {
        _swingController.OnSwingStart -= Swing_OnStart;
        _swingController.OnSwingEnd -= Swing_OnEnd;
    }
}
