using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class Relay : MonoBehaviour
{
	private bool _createdRelay;
	private bool _joinedRelay;

	private async void Start() // TODO: Make this game work offline so I can work on train
	{
		await UnityServices.InitializeAsync();

		AuthenticationService.Instance.SignedIn += () =>
		{
			Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
		};
		await AuthenticationService.Instance.SignInAnonymouslyAsync();
	}

	public async void CreateRelay()
	{
		if(_createdRelay) return;
	
		try
		{
			_createdRelay = true;
			
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);

			string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			GUIUtility.systemCopyBuffer = joinCode.ToString();
			Debug.Log(allocation.Region);
			Debug.Log(joinCode);

			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
				AllocationUtils.ToRelayServerData(allocation, "dtls")
			);

			Loader.IsHost = true;
			Loader.Load(Loader.Scene.GameScene);
			
		}
		catch (RelayServiceException e)
		{
			Debug.LogError(e);
		}
	}

	public async void JoinRelay(string joinCode)
	{
		if(_joinedRelay) return;
	
		try
		{
			_joinedRelay = true;
		
			Debug.Log($"Joining Relay with {joinCode}");
			JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
				AllocationUtils.ToRelayServerData(joinAllocation, "dtls")
			);

			Loader.IsHost = false;
			Loader.Load(Loader.Scene.GameScene);
		}
		catch (RelayServiceException e)
		{
			Debug.LogError(e);
		}
	}
}