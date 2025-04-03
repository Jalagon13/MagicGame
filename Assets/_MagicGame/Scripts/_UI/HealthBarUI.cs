using System;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
	private MMProgressBar _progressBar;
	private NetworkHealthState _networkHealthState;
	
	private void Awake()
	{
		_progressBar = GetComponent<MMProgressBar>();
		_networkHealthState = transform.root.gameObject.GetComponent<NetworkHealthState>();
		if (_networkHealthState == null)
		{
			Debug.LogError("Root GameObject" + transform.root.gameObject.name + " does not have component that implements IHasHealth");
		}
	}
	
	private void Start()
	{
		_networkHealthState.OnHitPointsDamaged += OnHitPointsDamaged;
		_networkHealthState.OnHitPointsReplenished += OnHitPointsReplenished;
		_networkHealthState.OnHitPointsDepleted += OnHitPointsDepleted;

		Hide();
	}

    private void OnHitPointsDamaged(object sender, NetworkHealthState.HitPointsDamagedEventArgs e)
    {
		UpdateHealthBarVisibility();
	}

    private void OnHitPointsReplenished(object sender, EventArgs e)
    {
		UpdateHealthBarVisibility();
	}

    private void OnHitPointsDepleted(object sender, EventArgs e)
    {
		UpdateHealthBarVisibility();
	}
    
    private void UpdateHealthBarVisibility()
    {
		_progressBar.UpdateBar(_networkHealthState.HitPoints.Value, 0, _networkHealthState.MaxHealth);

		if (_networkHealthState.HitPoints.Value <= 0 || _networkHealthState.HitPoints.Value >= _networkHealthState.MaxHealth)
		{
			Hide();
		}
		else
		{
			Show();
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
		_networkHealthState.OnHitPointsDamaged -= OnHitPointsDamaged;
		_networkHealthState.OnHitPointsReplenished -= OnHitPointsReplenished;
		_networkHealthState.OnHitPointsDepleted -= OnHitPointsDepleted;
	}
}
