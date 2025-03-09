using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Spell : NetworkBehaviour
{
	public bool Started { get; private set; }
	
	public NetworkVariable<SyncSpellData> SpellDataNV = new NetworkVariable<SyncSpellData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	protected GameObject _spellGameObject;
	protected CircleCollider2D _spellCollider;
	protected bool _isDead;
	protected Vector2 _finalDirection;
	protected Vector2 _velocity;
	
	private SyncSpellData _spellData;
	private Transform _spellModifierTf;
	private Timer _spellLifeTimer;
	private List<GameObject> _hitTargets = new List<GameObject>();
	private int _npcLayer, _collisionMask, _wallMask, _bounces;

	protected virtual void Awake()
    {
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<CircleCollider2D>();
		_spellModifierTf = transform.GetChild(0).GetChild(0);
	}
	
	public virtual void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
	{
		if(IsServer)
		{
			Started = true;
			transform.position = spawnPoint;
			_finalDirection = finalDirection;
			_isDead = false;

			_spellLifeTimer = new Timer(_spellData.Lifetime);
			_spellLifeTimer.OnTimerEnd += DestroySpell;
		}
	}
	
	public void CancelSpell()
	{
		if (IsServer)
		{
			NetworkObject.Despawn();
		}
		
		Destroy(gameObject);
	}
	
	public void SetSpellData(SyncSpellData spellData)
	{
		_spellData = spellData;
		GetComponent<SpellNetworkComponent>().InitializeSpellNetwork(spellData);
	}
	
	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			SpellDataNV.Value = _spellData;
			_collisionMask = LayerMask.GetMask(new[] { "PathfindingWall", "Npc" });
			_wallMask = LayerMask.NameToLayer("PathfindingWall");
			_npcLayer = LayerMask.NameToLayer("Npc");
		}

		foreach (int modifierIndex in SpellDataNV.Value.ModifierArray)
		{
			SpellModItemSO modifier = GameManager.Instance.GetItemSOFromItemId(modifierIndex) as SpellModItemSO;
			var go = Instantiate(modifier.SpellModifierPrefab, _spellModifierTf);
			
			if(IsServer)
			{
				go.GetComponent<ISpellModifier>().ApplyModifier(this);
			}
		}
	}
	
    private void DestroySpell(object sender, EventArgs e)
    {
		Started = false;

		if (IsServer)
		{
			NetworkObject.Despawn();
		}
		Debug.Log($"Spell Destroyed");
		Destroy(gameObject);
	}

	protected virtual void FixedUpdate()
	{
		if(!Started || !IsServer) return; //don't do anything before OnNetworkSpawn has run.

		_spellLifeTimer.Tick(Time.fixedDeltaTime);
		
		if(!_isDead)
		{
			DetectCollisions();
		}
	}

	private void DetectCollisions()
    {
		Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _spellCollider.radius, _collisionMask);
		for (int i = 0; i < collisions.Length; i++)
		{
			int layerTest = 1 << collisions[i].gameObject.layer;
			if((layerTest & _collisionMask) != 0)
			{
			    if(collisions[i].gameObject.layer == _wallMask)
			    {
			        if(collisions[i].TryGetComponent(out PathfindingWallTm pfWall))
			        {
						if (pfWall.BiomeSameAs(SpellDataNV.Value.SpawnBiome))
						{
							// Overlapping with a wall tile
							if (_bounces > SpellDataNV.Value.Bounces)
							{
								_isDead = true;
								_spellLifeTimer.Tick(Mathf.Infinity);
								return;
							}
							else
							{
								// Bounce if not at max bounces
								RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, _velocity.normalized, _spellCollider.radius, _collisionMask);
								foreach (var hit in hits)
								{
									if (hit.collider == collisions[i])
									{
										Vector2 hitNormal = hit.normal;
										float speed = _velocity.magnitude;
										_velocity = Vector2.Reflect(_velocity.normalized, hitNormal) * speed;
										_bounces++;
										break;
									}
								}
							}
						}
					}
			    }
			    
			    if(collisions[i].gameObject.layer == _npcLayer)
			    {
			    	if(collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.NpcBiomeType == SpellDataNV.Value.SpawnBiome)
			    	{
						// Overlapping with an NPC in the same biome
						if (!_hitTargets.Contains(collisions[i].gameObject))
			    		{
							_hitTargets.Add(collisions[i].gameObject);
							
							Npc npc = npcNet.gameObject.GetComponent<Npc>();
							npc.ApplyDamage(SpellDataNV.Value.Damage, NetworkManager.ConnectedClients[SpellDataNV.Value.SpawnPlayerId].PlayerObject.transform.position, SpellDataNV.Value.Knockback);
							
							if(_hitTargets.Count >= SpellDataNV.Value.MaxVictims)
							{
								_isDead = true;
								_spellLifeTimer.Tick(Mathf.Infinity);
								return;
							}
						}
			    	}
			    }
			}
		}
	}
}