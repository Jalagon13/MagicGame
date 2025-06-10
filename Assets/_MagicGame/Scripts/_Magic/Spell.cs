using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Spell : NetworkBehaviour
{
	[field: SerializeField] public GameObject Visualization { get; private set; }
	[field: SerializeField] public float DespawnDelay { get; private set; }

	public NetworkVariable<Vector2> Velocity { get; set; } = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<SyncSpellData> SpellData { get; set; } = new NetworkVariable<SyncSpellData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<bool> ShowVisuals { get; set; } = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<bool> IsStarted { get; set; } = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public int CollisionMask { get; private set; }
	public int NpcLayer { get; private set; }
	public int WallMask { get; private set; }
	public List<NetworkHealthState> HitTargets { get; private set; } = new();
	public Timer SpellLifeTimer { get; private set; }
	
	protected GameObject _spellGameObject;
	protected Vector2 _finalDirection;
	private bool _despawning;

	public NetworkObject SpellCasterNetworkObject
	{
		get
		{
			if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(SpellData.Value.CasterNetworkObjectId, out NetworkObject inflicterNetworkObj))
			{
				return inflicterNetworkObj;
			}

			return null;
		}
	}

	protected virtual void Awake()
    {
		_spellGameObject = transform.GetChild(0).gameObject;
		Visualization.SetActive(false);
	}

	public override void OnNetworkSpawn()
	{
		if(IsClient)
		{
			ShowVisuals.OnValueChanged += HandleVisuals;
		}
		
		if (IsOwner)
		{
			CollisionMask = LayerMask.GetMask(new[] { "LocalWall", "Npc" });
			WallMask = LayerMask.NameToLayer("LocalWall");
			NpcLayer = LayerMask.NameToLayer("Npc");
			
			Visualization.SetActive(false);	

			SpellManager.Instance.OnExecuteSpells += ExecuteSpellStart;
			SpellManager.Instance.OnCancelSpells += CancelSpellCharge;
			HotbarManager.Instance.OnFocusSlotUpdated += TryToDespawnIfSlotChanged;

			OnSpellSpawned();
		}
	}

	public override void OnNetworkDespawn()
	{
		if (IsOwner)
		{
			SpellManager.Instance.OnExecuteSpells -= ExecuteSpellStart;
			SpellManager.Instance.OnCancelSpells -= CancelSpellCharge;
			HotbarManager.Instance.OnFocusSlotUpdated -= TryToDespawnIfSlotChanged;
		}

		if (IsClient)
		{
			Visualization.transform.parent = transform;
			ShowVisuals.OnValueChanged -= HandleVisuals;
		}
	}

	protected virtual void Update()
	{
		if(IsOwner)
		{
			if(SpellData.Value.IsContinuousCast && IsStarted.Value)
			{
			    if(!SpellManager.Instance.IsSpellKeyHeld(SpellData.Value.WandSlotIndex))
			    {
					OnOwnerSpellEnd();
				}
			}
			else if (SpellLifeTimer != null && SpellLifeTimer.RemainingSeconds > 0)
			{
				SpellLifeTimer.Tick(Time.deltaTime);
			}
		}
	}
	
	public void OnOwnerSpellCanceled()
	{
		IsStarted.Value = false;
		
		if (IsOwner && SpellData.Value.IsContinuousCast)
		{
			Debug.Log($"SPELL class: Stopping continuous casting");
			SpellManager.Instance.IsContinuouslyCasting = false;
		}

		DespawnSpellServerRpc();
		OnSpellCanceled();
	}
	
	public void OnOwnerSpellEnd()
	{
		IsStarted.Value = false;
		
		if(IsOwner && SpellData.Value.IsContinuousCast )
		{
		    SpellManager.Instance.IsContinuouslyCasting = false;
		}
		
		StartCoroutine(WaitToDespawnRoutine());
		OnSpellEnd();
	}

	public bool IsValidNpcHit(Collider2D collider, out DamageReceiver damageReceiver)
	{
		damageReceiver = null;

		if (collider.gameObject.layer != NpcLayer)
			return false;

		if (!collider.TryGetComponent(out NpcNetworkVisibility npcNet))
			return false;

		if (!npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
			return false;

		damageReceiver = npcNet.GetComponent<DamageReceiver>();
		return damageReceiver != null;
	}

    private void TryToDespawnIfSlotChanged(object sender, HotbarManager.OnFocusItemSetEventArgs e)
    {
        if(SpellData.Value.DespawnIfFocusSlotChanged)
        {
			InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
			
			if(selectedInventoryItem.Id != SpellData.Value.InventorySlotId)
			{
				OnOwnerSpellEnd();
			}
		}
    }

    private void ExecuteSpellStart(object sender, SpellManager.ExecuteSpellsEventArgs e)
	{
		if(IsStarted.Value || _despawning) return;

		SpellManager.Instance.OnCancelSpells -= CancelSpellCharge;

		transform.position = e.SpawnPoint;
		_finalDirection = e.Direction;

		IsStarted.Value = true;
		ShowVisuals.Value = true;

		SpellLifeTimer = new Timer(SpellData.Value.Lifetime);
		SpellLifeTimer.OnTimerEnd += OnSpellLifeTimerEnd;

		OnExecuteSpellStart();
	}

	private void CancelSpellCharge(object sender, EventArgs e)
    {
		OnOwnerSpellCanceled();
	}

	private IEnumerator WaitToDespawnRoutine()
	{
		_despawning = true;
		
		if (DespawnDelay > 0)
		{
			yield return new WaitForSeconds(DespawnDelay);
		}
	
		DespawnSpellServerRpc();
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DespawnSpellServerRpc()
	{
		NetworkObject.DontDestroyWithOwner = true;
		NetworkObject.Despawn();
	}

	private void OnSpellLifeTimerEnd(object sender, EventArgs e)
	{
		SpellLifeTimer.OnTimerEnd -= OnSpellLifeTimerEnd;
		OnOwnerSpellEnd();
	}

	private void HandleVisuals(bool previousValue, bool newValue)
    {
		Visualization.SetActive(newValue);
    }

	protected abstract void OnSpellSpawned();
	protected abstract void OnExecuteSpellStart();
	protected abstract void OnSpellEnd();
	protected abstract void OnSpellCanceled();
}