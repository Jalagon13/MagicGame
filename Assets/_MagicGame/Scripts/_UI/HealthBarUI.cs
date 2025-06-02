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
		NetworkHealthState.HitPointsDepleted += UpdateHealthBar;
		NetworkHealthState.HitPointsReplenished += UpdateHealthBar;

		Hide();
	}

    private void UpdateHealthBar(object sender, EventArgs e)
    {
		_progressBar.UpdateBar(NetworkHealthState.HitPoints.Value, 0, Player.LocalClientInstance.ServerCharacter.Data.BaseHP);

		if (NetworkHealthState.HitPoints.Value <= 0 || NetworkHealthState.HitPoints.Value >= Player.LocalClientInstance.ServerCharacter.Data.BaseHP)
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
		NetworkHealthState.HitPointsDepleted -= UpdateHealthBar;
		NetworkHealthState.HitPointsReplenished -= UpdateHealthBar;
	}
}
