using System;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
	[SerializeField] private GameObject _iHasHealthGameObject;
	[SerializeField] private bool _isVisibleToThisClient = true;
	
	private MMProgressBar _progressBar;
	private NetworkHealthState _networkHealthState;
	
	private void Awake()
	{
		_progressBar = GetComponent<MMProgressBar>();
	}
	
	private void Start()
	{
		_networkHealthState = _iHasHealthGameObject.GetComponent<NetworkHealthState>();
		if(_networkHealthState == null)
		{
			Debug.LogError("Game Object  " + _iHasHealthGameObject + " does not have component that implements IHasHealth");
		}
	
		// _hasHealth.OnHealthUpdated += OnHealthUpdated;
		
		Hide();
	}

	// private void OnHealthUpdated(object sender, IHasHealth.OnHealthUpdatedEventArgs e)
	// {
	// 	if(!_isVisibleToThisClient && WorldManager.Instance.NetworkManager.LocalClientId == WorldManager.Instance.OwnerClientId)
	// 	{
	// 		return;
	// 	}
		
	// 	_progressBar.UpdateBar(e.NewValue, 0, e.MaxValue);
		
	// 	if(e.NewValue <= 0 || e.NewValue >= e.MaxValue)
	// 	{
	// 		Hide();
	// 	}
	// 	else
	// 	{
	// 		Show();
	// 	}
	// }
	
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
		// _hasHealth.OnHealthUpdated -= OnHealthUpdated;
	}
}
