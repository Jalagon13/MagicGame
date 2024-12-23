using Pathfinding;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestingNetcodeUI : MonoBehaviour
{
	[SerializeField] private Button _startHostButton;
	[SerializeField] private Button _startClientButton;
	
	private void Awake()
	{
		_startHostButton.onClick.AddListener(async () => 
		{
			Debug.Log("HOST");
			NetworkManager.Singleton.StartHost();
			
			if(SaveSystem.Instance.EnvironmentDataExists(EnvironmentID.Forest))
			{
				await SaveSystem.Instance.DeserializeAndDispatchData(EnvironmentID.Forest);
			}
			else
			{
				WorldManager.Instance.GenerateEnvironment(EnvironmentID.Forest);
			}
			
			
			Hide();
		});
		_startClientButton.onClick.AddListener(() => 
		{
			Debug.Log("CLIENT");
			NetworkManager.Singleton.StartClient();
			Hide();
		});
	}
	
	private void Hide()
	{
		gameObject.SetActive(false);
	}
}
