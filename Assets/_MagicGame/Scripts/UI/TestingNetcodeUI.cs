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
			
			if(SaveSystem.Instance.EnvironmentDataExists(BiomeType.Forest))
			{
				await SaveSystem.Instance.DeserializeAndDispatchData(BiomeType.Forest);
			}
			else
			{
				WorldManager.Instance.GenerateEnvironment(BiomeType.Forest);
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
