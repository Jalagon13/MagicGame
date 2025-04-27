using System;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
	[field: SerializeField] public NetworkHealthState NetworkHealthState { get; private set; }
	
	private MMProgressBar _progressBar;
	
	private void Awake()
	{
		_progressBar = GetComponent<MMProgressBar>();
		if (NetworkHealthState == null)
		{
			Debug.LogError("Root GameObject" + transform.root.gameObject.name + " does not have component that implements IHasHealth");
		}
	}
	
	private void Start()
	{
		Debug.Log($"Subscribing to health events for {transform.root.gameObject.name}");
		NetworkHealthState.OnHitPointsDamaged += OnHitPointsDamaged;
		NetworkHealthState.OnHitPointsReplenished += OnHitPointsReplenished;
		NetworkHealthState.OnHitPointsDepleted += OnHitPointsDepleted;

		Hide();
	}

    private void OnHitPointsDamaged(object sender, NetworkHealthState.HitPointsDamagedEventArgs e)
    {
		Debug.Log($"Hit Points Damaged: {e.DamageTaken}");
		UpdateHealthBarVisibility();
	}

    private void OnHitPointsReplenished(object sender, EventArgs e)
    {
		Debug.Log($"Hit Points Replenished");
		UpdateHealthBarVisibility();
	}

    private void OnHitPointsDepleted(object sender, EventArgs e)
    {
		Debug.Log($"Hit Points Depleted");
		UpdateHealthBarVisibility();
	}
    
    private void UpdateHealthBarVisibility()
    {
		_progressBar.UpdateBar(NetworkHealthState.HitPoints.Value, 0, NetworkHealthState.MaxHealth.Value);

		Debug.Log($"Health: {NetworkHealthState.HitPoints.Value} / {NetworkHealthState.MaxHealth.Value}");

		if (NetworkHealthState.HitPoints.Value <= 0 || NetworkHealthState.HitPoints.Value >= NetworkHealthState.MaxHealth.Value)
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
		Debug.Log($"Showing health bar for {transform.root.gameObject.name}");
		gameObject.SetActive(true);
	}
	
	private void Hide()
	{
		Debug.Log($"Hiding health bar for {transform.root.gameObject.name}");
		gameObject.SetActive(false);
	}
	
	private void OnDestroy()
	{
		NetworkHealthState.OnHitPointsDamaged -= OnHitPointsDamaged;
		NetworkHealthState.OnHitPointsReplenished -= OnHitPointsReplenished;
		NetworkHealthState.OnHitPointsDepleted -= OnHitPointsDepleted;
	}
}
