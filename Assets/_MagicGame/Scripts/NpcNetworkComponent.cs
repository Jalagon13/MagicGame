using System;
using Unity.Netcode;
using UnityEngine;

// Holds logic for dynamic client visibility
public class NpcNetworkComponent : NetworkBehaviour
{
	[SerializeField] private bool _continuallyCheckVisibility = true;

	private const int DESPAWN_TIMER_DURATION = 3;
	private ulong _spawningClientId;
	private Timer _despawnTimer;
	private bool _npcIsBeingRemoved;
	private Npc _npc;
	private byte _npcId;
	public BiomeType NpcEnvironment { get; private set; }
	private GameObject _npcGameObject;
	private Collider2D _npcCollider;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			_npcGameObject = transform.GetChild(0).gameObject;
			_npc = GetComponent<Npc>();
			_npc.OnNpcKilled += Npc_OnNpcKilled;
		
			_despawnTimer = new Timer(DESPAWN_TIMER_DURATION);
			_despawnTimer.OnTimerEnd += HandleDespawnTimerEnd;
			
			_npcCollider = GetComponent<Collider2D>();
		
			if (_continuallyCheckVisibility)
			{
				NetworkManager.NetworkTickSystem.Tick += NpcNetworkTick;
			}
		}
		base.OnNetworkSpawn();
	}
	
	

	private void Npc_OnNpcKilled(object sender, EventArgs e)
	{
		KillNpcServerRpc();
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void KillNpcServerRpc()
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] Killing NPC.");
		_npcIsBeingRemoved = true;
		NpcManager.Instance.DespawnNpcServerRpc(_npcId, GetComponent<NetworkObject>(), _spawningClientId, true);
	}

	public void SetSpawningClientId(ulong sourceClientId)
	{
		_spawningClientId = sourceClientId;
	}
	
	public void SetNpcId(byte npcId)
	{
		_npcId = npcId;
	}

	private bool CheckIfInSpawnZone(ulong clientId)
	{
		if (!IsSpawned) return false;

		Vector2 playerPos = NetworkManager.ConnectedClients[clientId].PlayerObject.transform.position;
		return IsPointInRectangle(transform.position, playerPos, NpcManager.SPAWN_ZONE_WIDTH, NpcManager.SPAWN_ZONE_HEIGHT);
	}

	private void NpcNetworkTick()
	{
		HandleNpcEnvironmentVisibility();
		HandleNpcSpawnZoneVisibility();
		HandlePathfindingVisibility();
		UpdateDespawnTimer();
	}

	private void HandlePathfindingVisibility()
	{
		if(!Pathfinding.Instance.EnvironmentPathfindingExists(NpcEnvironment))
		{
			// If No player is in the same environment as this npc, despawn it
			DespawnNpc();
		}
	}

	private void HandleNpcEnvironmentVisibility()
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			var isInSameEnvironment = CheckIfInSameEnvironment(clientId);
			var isVisibile = NetworkObjectVisibleTo(clientId);
			
			if(isInSameEnvironment && !isVisibile)
			{
				ShowNpc(clientId);
			}
			else if(!isInSameEnvironment && isVisibile)
			{
				HideNpc(clientId);
			}
		}
	}
	
	private void ShowNpc(ulong clientId)
	{
		Debug.Log($"Showing {gameObject.name} to player {clientId}");
		if(clientId == NetworkManager.ServerClientId)
		{
			_npcGameObject.SetActive(true);
			if(_npcCollider != null)
			{
				_npcCollider.enabled = true;
			}
		}
				
		NetworkObject.NetworkShow(clientId);
	}
	
	private void HideNpc(ulong clientId)
	{
		Debug.Log($"Hiding {gameObject.name} to player {clientId}");
		if(clientId == NetworkManager.ServerClientId)
		{
			_npcGameObject.SetActive(false);
			_npcCollider.enabled = false;
		}
				
		NetworkObject.NetworkHide(clientId);
	}
	
	private bool NetworkObjectVisibleTo(ulong clientId)
	{
		return clientId == NetworkManager.ServerClientId ? _npcGameObject.activeInHierarchy : NetworkObject.IsNetworkVisibleTo(clientId);
	}

	private void HandleNpcSpawnZoneVisibility()
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			// If clientId is not the same environment as this NPC, skip it
			if(!CheckIfInSameEnvironment(clientId)) continue;
			
			bool isInSpawnZone = CheckIfInSpawnZone(clientId);

			if (!isInSpawnZone && !_npcIsBeingRemoved)
			{
				if (!IsNpcVisibleToAnyOtherClient(clientId))
				{
					// If NPC is NOT visible to any other client once it reaches outside the spawn zone of this client, despawn it
					DespawnNpc();
				}
			}
		}
	}

	private bool CheckIfInSameEnvironment(ulong clientId)
	{
		var clientEnvironment = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().CurrentBiome.Value;
	
		return clientEnvironment == NpcEnvironment;
	}

	private void UpdateDespawnTimer()
	{
		if (_despawnTimer == null || _despawnTimer.RemainingSeconds <= 0 || _npcIsBeingRemoved) return;

		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			// If clientId is not the same environment as this NPC, skip it
			if(!CheckIfInSameEnvironment(clientId)) continue;
		
			var playerPos = NetworkManager.ConnectedClients[clientId].PlayerObject.transform.position;
			if (IsPointInRectangle(transform.position, playerPos, NpcManager.NO_SPAWN_ZONE_WIDTH, NpcManager.NO_SPAWN_ZONE_HEIGHT))
			{
				_despawnTimer.Reset();
				return;
			}
		}

		_despawnTimer.Tick(Time.deltaTime);
	}

	private bool IsPointInRectangle(Vector2 point, Vector2 rectCenter, float rectWidth, float rectHeight)
	{
		float halfWidth = rectWidth / 2;
		float halfHeight = rectHeight / 2;

		return point.x >= rectCenter.x - halfWidth &&
			   point.x <= rectCenter.x + halfWidth &&
			   point.y >= rectCenter.y - halfHeight &&
			   point.y <= rectCenter.y + halfHeight;
	}

	private void HandleDespawnTimerEnd(object sender, EventArgs e)
	{
		if (!IsServer) return;

		_despawnTimer.OnTimerEnd -= HandleDespawnTimerEnd;
		_despawnTimer = null;

		// Debug.Log($"[Client {NetworkManager.LocalClientId}] Despawn timer ended, despawning NPC");
		DespawnNpc();
	}

	private bool IsNpcVisibleToAnyOtherClient(ulong excludedClientId)
	{
		int clientsFound = 0;
	
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			if (clientId == excludedClientId) continue;
			
			bool isInSpawnZone = CheckIfInSpawnZone(clientId);
			
			if(isInSpawnZone)
			{
				clientsFound++;
			}
		}
		
		if(clientsFound > 0)
		{
			// There exists another client that wants to show this NPC, so do some local un-rendering on THIS client and not the client that wants to show the NPC
			return true;
		}
		
		return false;
	}

	private void DespawnNpc()
	{
		// Debug.Log($"[Client {NetworkManager.LocalClientId}] Despawning NPC.");
		_npcIsBeingRemoved = true;
		NpcManager.Instance.DespawnNpcServerRpc(_npcId, GetComponent<NetworkObject>(), _spawningClientId, false);
	}
	
	public void SetEnvironment(BiomeType environment)
	{
		NpcEnvironment = environment;
		
		// Find walldetectorcollider and populate it
		var wallDetectorCollider = GetComponentInChildren<WallDetectorCollider>();
		if(wallDetectorCollider != null)
		{
			wallDetectorCollider.SetEnvironment(environment);
		}
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			NetworkManager.NetworkTickSystem.Tick -= NpcNetworkTick;
			_npc.OnNpcKilled -= Npc_OnNpcKilled;
		}

		// Debug.Log($"OnNetworkDespawn callback on {gameObject.name} for client: {NetworkManager.LocalClientId}");
		base.OnNetworkDespawn();
	}
}