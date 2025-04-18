using System;
using System.Collections.Generic;
using FMODUnity;
using MoreMountains.Tools;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Spell : NetworkBehaviour
{
	public static bool IsContinuouslyCasting;

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
	protected CircleCollider2D _spellCollider;
	protected Vector2 _finalDirection;

	private Transform _visualizationTf;
	private PositionLerper _positionLerper;
	const float k_LerpTime = 0.05f;

	protected virtual void Awake()
    {
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<CircleCollider2D>();
		_visualizationTf = transform.GetChild(0).GetChild(0);
		
		Hide();
	}

	public override void OnNetworkSpawn()
	{
		ShowVisuals.OnValueChanged += HandleVisuals;
		
		if (IsOwner)
		{
			ShowVisuals.Value = false;
			
			CollisionMask = LayerMask.GetMask(new[] { "PathfindingWall", "Npc" });
			WallMask = LayerMask.NameToLayer("PathfindingWall");
			NpcLayer = LayerMask.NameToLayer("Npc");

			SpellManager.Instance.OnExecuteSpells += ExecuteSpellStart;
			SpellManager.Instance.OnCancelSpells += CancelSpellCharge;
			HotbarManager.Instance.OnFocusSlotUpdated += TryToDespawnIfSlotChanged;
			
			OnOwnerSpellSpawned();
		}
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
		transform.position = e.SpawnPoint;
		_finalDirection = e.Direction;

		IsStarted.Value = true;
		ShowVisuals.Value = true;

		SpellLifeTimer = new Timer(SpellData.Value.Lifetime);
		SpellLifeTimer.OnTimerEnd += OnSpellLifeTimerEnd;

		OnOwnerExecuteSpellStart();
	}

	private void CancelSpellCharge(object sender, EventArgs e)
    {
		OnOwnerSpellCanceled();
	}

    protected virtual void Update()
	{
		if(IsStarted.Value && IsOwner)
		{
			SpellLifeTimer.Tick(Time.deltaTime);
		}
	
		if (IsClient && _visualizationTf.gameObject.activeSelf)
		{
			_visualizationTf.position = _positionLerper.LerpPosition(_visualizationTf.position, transform.position);
		}
		// don't do anything before OnNetworkSpawn has run.
	}

	protected virtual void OnOwnerSpellSpawned() { }
	protected virtual void OnOwnerExecuteSpellStart() { }
	protected virtual void OnOwnerSpellEnd()
	{
		IsStarted.Value = false;
		DespawnSpellServerRpc();
	}

	public virtual void OnOwnerSpellCanceled()
	{
		IsStarted.Value = false;
		DespawnSpellServerRpc();
	}

	private void OnSpellLifeTimerEnd(object sender, EventArgs e)
	{
		SpellLifeTimer.OnTimerEnd -= OnSpellLifeTimerEnd;
		OnOwnerSpellEnd();
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DespawnSpellServerRpc()
	{
		NetworkObject.DontDestroyWithOwner = true;
		NetworkObject.Despawn();
	}

    private void HandleVisuals(bool previousValue, bool newValue)
    {
        if(newValue)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

	private void Show()
	{
		if(Player.LocalClientInstance.OwnerClientId == SpellData.Value.OwnerPlayerId)
		{
			_visualizationTf.position = Player.LocalClientInstance.MainHand.SpellSpawnTransform.position;
		}
		else
		{
			_visualizationTf.position = NetworkManager.Singleton.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.GetComponent<Player>().MainHand.SpellSpawnTransform.position;
		}

		_positionLerper = new PositionLerper(transform.position, k_LerpTime);

		_visualizationTf.parent = null;
		_visualizationTf.gameObject.SetActive(true);
	}

	private void Hide()
	{
		_visualizationTf.gameObject.SetActive(false);
	}
	
	protected void DamageTargets()
	{
	    
	}

	// private void DetectCollisions()
    // {
	// 	if(_spellCollider == null) return;
    
	// 	Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _spellCollider.radius, CollisionMask);
	// 	for (int i = 0; i < collisions.Length; i++)
	// 	{
	// 		int layerTest = 1 << collisions[i].gameObject.layer;
	// 		if((layerTest & CollisionMask) != 0)
	// 		{
	// 		    if(collisions[i].gameObject.layer == WallMask)
	// 		    {
	// 				if (collisions[i].TryGetComponent(out PathfindingWallTm pfWall))
	// 		        {
	// 					if (pfWall.BiomeSameAs(SpellData.Value.SpawnBiome))
	// 					{
	// 						if(SpellData.Value.GhostDistance > 0)
	// 						{
	// 						    _passingThroughWall = true;
	// 						}
	// 						else
	// 						{
	// 							if (_bounces >= SpellData.Value.Bounces)
	// 							{
	// 								OnOwnerSpellEnd();

	// 								SoundManager.Instance.PlayOneShot(HitSomethingSound, transform.position);
									
	// 								return;
	// 							}
	// 							else
	// 							{
	// 								// Bounce if not at max bounces
	// 								RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Velocity.Value.normalized, _spellCollider.radius, CollisionMask);
	// 								foreach (var hit in hits)
	// 								{
	// 									if (hit.collider == collisions[i])
	// 									{
	// 										Vector2 hitNormal = hit.normal;
	// 										float speed = Velocity.Value.magnitude;
	// 										Velocity.Value = Vector2.Reflect(Velocity.Value.normalized, hitNormal) * speed;
	// 										_bounces++;
	// 										break;
	// 									}
	// 								}
	// 							}
	// 						}
	// 					}
	// 				}
	// 		    }
			    
	// 		    if(collisions[i].gameObject.layer == NpcLayer)
	// 		    {
	// 		    	if(collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
	// 		    	{
	// 					NetworkHealthState npcHealth = npcNet.gameObject.GetComponent<NetworkHealthState>();

	// 					// Overlapping with an NPC in the same biome
	// 					if (!HitTargets.Contains(npcHealth))
	// 		    		{
	// 						HitTargets.Add(npcHealth);
	// 						npcHealth.TakeDamageRpc(SpellData.Value.Damage, NetworkManager.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.transform.position, SpellData.Value.Knockback);
							
	// 						SoundManager.Instance.PlayOneShot(HitSomethingSound, transform.position);
							
	// 						if (HitTargets.Count >= SpellData.Value.Pierces)
	// 						{
	// 							OnOwnerSpellEnd();
	// 							return;
	// 						}
	// 					}
	// 		    	}
	// 		    }
	// 		}
	// 	}
	// }

	public override void OnNetworkDespawn()
	{
		if(IsOwner)
		{
			SpellManager.Instance.OnExecuteSpells -= ExecuteSpellStart;
			SpellManager.Instance.OnCancelSpells -= CancelSpellCharge;
			HotbarManager.Instance.OnFocusSlotUpdated -= TryToDespawnIfSlotChanged;
		}
	
		if (IsClient)
		{
			Debug.Log($"Reattaching visualization before despawning");
			_visualizationTf.parent = transform;
		}
	}
}