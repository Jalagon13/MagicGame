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
	private async void Start()
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
		try
		{
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);

			string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

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
		try
		{
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