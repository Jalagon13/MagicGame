using System;
using Unity.Netcode;
using UnityEngine;

// Holds logic for dynamic client visibility
public class NpcNetworkVisibility : NetworkBehaviour
{
	[SerializeField] private WallColliderDetector _wallColliderDetector;
	// NTFS: This just makes it so it cannot despawn, it does nothing to alter AI behavior. Can potentially find an NPC in a wall if NPC is allowed to move around while no player (no pathfinding walls available) is around
	[field: SerializeField] public bool CanDespawn { get; private set; } = true; 

	private const int DESPAWN_TIMER_DURATION = 3;
	private ulong _spawningClientId;
	private Timer _despawnTimer;
	private bool _npcIsBeingRemoved;
	private Npc _npc;
	private int _npcId;
	public BiomeType NpcBiomeType { get; private set; }
	private GameObject _npcGameObject;
	private Collider2D _npcCollider;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			_npcGameObject = transform.GetChild(0).gameObject;
			_npc = GetComponent<Npc>();
			_npc.OnServerNpcKilled += Npc_OnNpcKilled;
		
			_despawnTimer = new Timer(DESPAWN_TIMER_DURATION);
			_despawnTimer.OnTimerEnd += HandleDespawnTimerEnd;
			
			_npcCollider = GetComponent<Collider2D>();
			
			HideNpc(NetworkManager.ServerClientId);

			NetworkObject.CheckObjectVisibility += CheckIfInSameEnvironment;
			NetworkManager.NetworkTickSystem.Tick += NpcNetworkTick;
		}
		base.OnNetworkSpawn();
	}
	
	public bool SameBiomeAs(BiomeType biome)
	{
	    return NpcBiomeType == biome;
	}

	private void Npc_OnNpcKilled(object sender, EventArgs e)
	{
		KillNpcServerRpc();
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void KillNpcServerRpc()
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] Killing NPC.");
		_npcIsBeingRemoved = true;
		NpcManager.Instance.DespawnNpcServerRpc(_npcId, GetComponent<NetworkObject>(), _spawningClientId, true);
	}
	
	public void InitialieNpcNetwork(ulong sourceClientId, int npcId, BiomeType biome)
	{
		NpcBiomeType = biome;
		_spawningClientId = sourceClientId;
		_npcId = npcId;
		
		if(_wallColliderDetector != null)
		{
			_wallColliderDetector.SetEnvironment(NpcBiomeType, Pathfinding.Instance.GetExistingPathfindingBiomes());
		}
	}

	private bool CheckIfInSpawnZone(ulong clientId)
	{
		if (!IsSpawned) return false;

		Vector2 playerPos = NetworkManager.ConnectedClients[clientId].PlayerObject.transform.position;
		return IsPointInRectangle(transform.position, playerPos, NpcManager.OUTER_SPAWN_ZONE_WIDTH, NpcManager.OUTER_SPAWN_ZONE_HEIGHT);
	}

	private void NpcNetworkTick()
	{
		if (!IsSpawned) return;
		
		HandleNpcBiomeVisibility();
		
		if(CanDespawn)
		{
			HandleNpcSpawnZoneVisibility();
			HandlePathfindingVisibility();
			UpdateDespawnTimer();
		}
	}

	private void HandlePathfindingVisibility()
	{
		if(!Pathfinding.Instance.EnvironmentPathfindingExists(NpcBiomeType))
		{
			// If No player is in the same environment as this npc, despawn it
			DespawnNpc();
		}
	}

	private void HandleNpcBiomeVisibility()
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
		if(clientId == NetworkManager.ServerClientId)
		{
			_npcGameObject.SetActive(true);
			_npcCollider.enabled = true;
			_npcCollider.isTrigger = true;
		}
		else
		{
			NetworkObject.NetworkShow(clientId);
		}
	}
	
	private void HideNpc(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			_npcGameObject.SetActive(false);
			_npcCollider.enabled = false;
			_npcCollider.isTrigger = false;
		}
		else
		{
			NetworkObject.NetworkHide(clientId);
		}
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
		return NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().CurrentPlayerBiome.Value == NpcBiomeType;
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
		
		if(IsSpawned)
		{
			_npcIsBeingRemoved = true;
			Debug.Log($"Spawned? {IsSpawned}");
			NpcManager.Instance.DespawnNpcServerRpc(_npcId, GetComponent<NetworkObject>(), _spawningClientId, false);
		}
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			NetworkObject.CheckObjectVisibility -= CheckIfInSameEnvironment;
			NetworkManager.NetworkTickSystem.Tick -= NpcNetworkTick;
			_npc.OnServerNpcKilled -= Npc_OnNpcKilled;
		}

		// Debug.Log($"OnNetworkDespawn callback on {gameObject.name} for client: {NetworkManager.LocalClientId}");
		base.OnNetworkDespawn();
	}
}