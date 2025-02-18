using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EntityData
{
	public Vector2 Position;
	
	public EntityData(Vector2 position)
	{
		Position = position;
	}
}

public class EntityManager : NetworkBehaviour
{
	public static EntityManager Instance { get; private set; }

	[SerializeField] private Entity _entityTest;

	private Dictionary<ulong, EntityData> _entityData = new();

	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		GameInput.Instance.OnResearchMenuButton += TestSpawn;
	}

	private void TestSpawn(object sender, EventArgs e)
	{
		SpawnEntityServerRpc(ActionManager.MouseWorldPosition);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnEntityServerRpc(Vector2 spawnPos)
	{
		ulong npcId = IdGenerator.GenerateRandomId();
		
		_entityData.Add(npcId, new(spawnPos));
		
		SpawnEntityClientRpc(spawnPos);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void SpawnEntityClientRpc(Vector2 spawnPos)
	{
		if(ChunkManager.Instance.ObjectPositionInLoadedChunks(spawnPos))
		{
			Instantiate(_entityTest, spawnPos, Quaternion.identity);
		}
	}

	public override void OnDestroy()
	{
		GameInput.Instance.OnResearchMenuButton -= TestSpawn;
	}
}
