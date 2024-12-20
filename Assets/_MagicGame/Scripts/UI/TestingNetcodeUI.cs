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
        _startHostButton.onClick.AddListener(() => 
        {
            Debug.Log("HOST");
            GridGraph gridGraph = AstarPath.active.data.gridGraph;
            gridGraph.Scan();
            NetworkManager.Singleton.StartHost();
            WorldManager.Instance.GenerateEnvironment(EnvironmentID.Forest);
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
